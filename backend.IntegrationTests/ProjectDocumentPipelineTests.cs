using System.Net;
using System.Net.Http.Json;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Models.ProjectDocuments;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Search;
using KnowledgeBank.Services.Vector;
using KnowledgeBank.Utils;
using Meilisearch;
using Meilisearch.QueryParameters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// The background pipeline after an upload: preview conversion, text extraction and indexing of the latest
/// version. Word files throughout, since they're extracted locally; PDFs would need Mistral OCR, which has no fake.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ProjectDocumentPipelineTests(KennisbankFixture fixture)
{
    private string BucketName => fixture.Services.GetRequiredService<EnvironmentConfig>().GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    [Fact]
    public async Task Docx_GetsAPreview_ExtractedText_AndSearchableChunks()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        string marker = Api.Unique("Budget overrun in phase two");
        byte[] docx = Docx("Minutes of the steering group", marker);

        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, fileName: "minutes.docx", content: docx);
        ProjectDocumentVersion version = await WaitForPipelineAsync(await LatestVersionIdAsync(documentId));

        Assert.Equal(EmbeddingStatus.Completed, version.PreviewStatus);
        Assert.Equal(EmbeddingStatus.Completed, version.EmbeddingStatus);
        Assert.Equal(docx.Length, version.SizeBytes);
        Assert.Contains(marker, version.ExtractedText);

        HttpResponseMessage preview = await creator.GetAsync($"/projects/{projectId}/documents/{documentId}/versions/{version.Id}/preview");
        await Api.EnsureAsync(preview, HttpStatusCode.OK);
        Assert.Equal("application/pdf", preview.Content.Headers.ContentType?.MediaType);
        Assert.Equal(FakeAiServer.ConvertedPdf, await preview.Content.ReadAsByteArrayAsync());

        List<ProjectDocumentChunk> chunks = await ChunksAsync(documentId);
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.Equal(version.Id, c.VersionId));
        Assert.Contains(chunks, c => c.ChunkText.Contains(marker));

        await Api.WaitUntilAsync(async () => (await MeiliChunksAsync(documentId)).Count == chunks.Count, "chunks in Meilisearch");
        Assert.All(await MeiliChunksAsync(documentId), c => Assert.Equal(projectId.ToString(), c.ProjectId));

        // Search only looks inside the documents it's given
        ProjectDocumentChunkSearchIndexService search = fixture.Services.CreateScope().ServiceProvider.GetRequiredService<ProjectDocumentChunkSearchIndexService>();
        float[] embedding = FakeAiServer.VectorFor(marker);
        Assert.Contains(await search.SearchAsync(marker, embedding, [documentId]), hit => hit.DocumentId == documentId.ToString());
        Assert.Empty(await search.SearchAsync(marker, embedding, [Guid.NewGuid()]));
        Assert.Empty(await search.SearchAsync(marker, embedding, []));
    }

    [Fact]
    public async Task NewVersion_ReplacesThePreviousVersionsChunks()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        string oldMarker = Api.Unique("Draft one"), newMarker = Api.Unique("Draft two");
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, fileName: "plan.docx", content: Docx(oldMarker));
        await WaitForPipelineAsync(await LatestVersionIdAsync(documentId));

        Guid secondVersion = await AddVersionAsync(creator, projectId, documentId, Docx(newMarker));
        await WaitForPipelineAsync(secondVersion);

        List<ProjectDocumentChunk> chunks = await ChunksAsync(documentId);
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.Equal(secondVersion, c.VersionId));
        Assert.DoesNotContain(chunks, c => c.ChunkText.Contains(oldMarker));

        await Api.WaitUntilAsync(async () =>
        {
            var meili = await MeiliChunksAsync(documentId);
            return meili.Count == chunks.Count && meili.All(c => c.VersionId == secondVersion.ToString());
        }, "Meilisearch to hold only the second version's chunks");
    }

    [Fact]
    public async Task NewVersionWithoutText_RemovesThePreviousVersionsChunks()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, fileName: "plan.docx", content: Docx(Api.Unique("Has text")));
        await WaitForPipelineAsync(await LatestVersionIdAsync(documentId));
        await Api.WaitUntilAsync(async () => (await MeiliChunksAsync(documentId)).Count > 0, "chunks in Meilisearch");

        Guid emptyVersion = await AddVersionAsync(creator, projectId, documentId, Docx());
        ProjectDocumentVersion version = await WaitForPipelineAsync(emptyVersion);

        Assert.Equal(EmbeddingStatus.Completed, version.EmbeddingStatus);
        Assert.Null(version.ExtractedText);
        Assert.Empty(await ChunksAsync(documentId));
        await Api.WaitUntilAsync(async () => (await MeiliChunksAsync(documentId)).Count == 0, "Meilisearch chunks to be removed");
    }

    [Fact]
    public async Task OlderVersionFinishingLast_DoesNotOverwriteTheNewerChunks()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        string oldMarker = Api.Unique("Old text"), newMarker = Api.Unique("New text");
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, fileName: "plan.docx", content: Docx(oldMarker));
        Guid firstVersion = await LatestVersionIdAsync(documentId);
        await WaitForPipelineAsync(firstVersion);
        Guid secondVersion = await AddVersionAsync(creator, projectId, documentId, Docx(newMarker));
        await WaitForPipelineAsync(secondVersion);

        using IServiceScope scope = fixture.Services.CreateScope();

        // The swap itself refuses a version that is no longer the latest: the check that closes the race
        bool replaced = await scope.ServiceProvider.GetRequiredService<IVectorStore>()
            .ReplaceProjectDocumentPointsAsync(documentId, firstVersion, [(oldMarker, 0)]);
        Assert.False(replaced);

        // And the whole pipeline for the old version, as when it waited in the queue behind the new one
        await scope.ServiceProvider.GetRequiredService<IngestionService>().RunProjectDocumentPipelineAsync(firstVersion);

        Assert.Equal(EmbeddingStatus.Completed, (await VersionAsync(firstVersion))!.EmbeddingStatus);
        List<ProjectDocumentChunk> chunks = await ChunksAsync(documentId);
        Assert.NotEmpty(chunks);
        Assert.All(chunks, c => Assert.Equal(secondVersion, c.VersionId));
        Assert.Contains(chunks, c => c.ChunkText.Contains(newMarker));
    }

    [Fact]
    public async Task ReplacingChunksOfADeletedDocument_ChangesNothing()
    {
        using IServiceScope scope = fixture.Services.CreateScope();

        bool replaced = await scope.ServiceProvider.GetRequiredService<IVectorStore>()
            .ReplaceProjectDocumentPointsAsync(Guid.NewGuid(), Guid.NewGuid(), [("orphan", 0)]);

        Assert.False(replaced);
    }

    [Fact]
    public async Task FailedConversion_LeavesTheDocumentSearchable()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId;
        ProjectDocumentVersion version;

        fixture.Ai.FailConversions = true;
        try
        {
            documentId = await creator.CreateProjectDocumentAsync(projectId, fileName: "plan.docx", content: Docx(Api.Unique("Searchable anyway")));
            version = await WaitForPipelineAsync(await LatestVersionIdAsync(documentId));
        }
        finally
        {
            fixture.Ai.FailConversions = false;
        }

        Assert.Equal(EmbeddingStatus.Failed, version.PreviewStatus);
        Assert.Equal(EmbeddingStatus.Completed, version.EmbeddingStatus);
        Assert.NotEmpty(await ChunksAsync(documentId));
        Assert.False(fixture.Storage.Exists(BucketName, ProjectDocumentService.PreviewKey(version.Id)));

        HttpResponseMessage preview = await creator.GetAsync($"/projects/{projectId}/documents/{documentId}/versions/{version.Id}/preview");
        await Api.EnsureAsync(preview, HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Pdf_IsItsOwnPreview()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        byte[] pdf = "%PDF-1.4 its own preview"u8.ToArray();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, content: pdf);

        // Embedding fails here (no OCR fake), which must not affect the preview
        ProjectDocumentVersion version = await WaitForPipelineAsync(await LatestVersionIdAsync(documentId));

        Assert.Equal(EmbeddingStatus.Completed, version.PreviewStatus);
        Assert.Equal(pdf.Length, version.SizeBytes);
        Assert.False(fixture.Storage.Exists(BucketName, ProjectDocumentService.PreviewKey(version.Id)));
        Assert.Equal(pdf, await creator.GetByteArrayAsync($"/projects/{projectId}/documents/{documentId}/versions/{version.Id}/preview"));
    }

    [Fact]
    public async Task DeletingADocument_RemovesItsChunksAndFiles()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, fileName: "plan.docx", content: Docx(Api.Unique("To be deleted")));
        Guid versionId = await LatestVersionIdAsync(documentId);
        await WaitForPipelineAsync(versionId);
        await Api.WaitUntilAsync(async () => (await MeiliChunksAsync(documentId)).Count > 0, "chunks in Meilisearch");
        Assert.True(fixture.Storage.Exists(BucketName, ProjectDocumentService.PreviewKey(versionId)));

        await Api.EnsureAsync(await creator.DeleteAsync($"/projects/{projectId}/documents/{documentId}"), HttpStatusCode.NoContent);

        Assert.Empty(await ChunksAsync(documentId));
        Assert.False(fixture.Storage.Exists(BucketName, ProjectDocumentService.StorageKey(versionId)));
        Assert.False(fixture.Storage.Exists(BucketName, ProjectDocumentService.PreviewKey(versionId)));
        await Api.WaitUntilAsync(async () => (await MeiliChunksAsync(documentId)).Count == 0, "Meilisearch chunks to be removed");
    }

    [Fact]
    public async Task DeletingTheProject_RemovesItsDocumentsAndChunks()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, fileName: "plan.docx", content: Docx(Api.Unique("Project goes away")));
        await WaitForPipelineAsync(await LatestVersionIdAsync(documentId));
        await Api.WaitUntilAsync(async () => (await MeiliChunksAsync(documentId)).Count > 0, "chunks in Meilisearch");

        await Api.EnsureAsync(await creator.DeleteAsync($"/projects/{projectId}"), HttpStatusCode.NoContent);

        using (IServiceScope scope = fixture.Services.CreateScope())
            Assert.False(await scope.ServiceProvider.GetRequiredService<DatabaseContext>().ProjectDocuments.AnyAsync(d => d.Id == documentId));
        Assert.Empty(await ChunksAsync(documentId));
        await Api.WaitUntilAsync(async () => (await MeiliChunksAsync(documentId)).Count == 0, "Meilisearch chunks to be removed");
    }

    #region Helpers

    /// <summary>A minimal valid Word document with one paragraph per string.</summary>
    private static byte[] Docx(params string[] paragraphs)
    {
        using MemoryStream stream = new();
        using (WordprocessingDocument document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            MainDocumentPart main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(paragraphs.Select(p => new Paragraph(new Run(new Text(p))))));
        }
        return stream.ToArray();
    }

    private static async Task<Guid> AddVersionAsync(HttpClient client, Guid projectId, Guid documentId, byte[] docx)
    {
        string upload = await client.UploadProjectDocumentFileAsync("plan.docx", docx);
        return await Api.ReadGuidAsync(await client.PostAsJsonAsync($"/projects/{projectId}/documents/{documentId}/versions",
            new { objectName = upload, fileName = "plan.docx" }));
    }

    private async Task<Guid> LatestVersionIdAsync(Guid documentId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DatabaseContext>().ProjectDocumentVersions
            .Where(v => v.DocumentId == documentId)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => v.Id)
            .FirstAsync();
    }

    private async Task<ProjectDocumentVersion?> VersionAsync(Guid versionId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DatabaseContext>().ProjectDocumentVersions
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == versionId);
    }

    /// <summary>Waits until both the preview and the embedding of a version have finished, either way.</summary>
    private async Task<ProjectDocumentVersion> WaitForPipelineAsync(Guid versionId)
    {
        static bool Done(EmbeddingStatus status) => status is EmbeddingStatus.Completed or EmbeddingStatus.Failed;

        ProjectDocumentVersion? version = null;
        await Api.WaitUntilAsync(async () =>
        {
            version = await VersionAsync(versionId);
            return version != null && Done(version.PreviewStatus) && Done(version.EmbeddingStatus);
        }, $"pipeline of project document version {versionId}");
        return version!;
    }

    private async Task<List<ProjectDocumentChunk>> ChunksAsync(Guid documentId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<DatabaseContext>().ProjectDocumentChunks
            .AsNoTracking()
            .Where(c => c.DocumentId == documentId)
            .OrderBy(c => c.ChunkPart)
            .ToListAsync();
    }

    private async Task<List<ProjectDocumentChunkDocument>> MeiliChunksAsync(Guid documentId)
    {
        MeilisearchClient meili = fixture.Services.GetRequiredService<MeilisearchClient>();
        var result = await meili.Index("project-doc-chunks").GetDocumentsAsync<ProjectDocumentChunkDocument>(new DocumentsQuery
        {
            Filter = $"documentId = \"{documentId}\"",
            Fields = ["id", "documentId", "projectId", "versionId", "chunkPart", "chunkText"]
        });
        return [.. result.Results];
    }

    #endregion
}
