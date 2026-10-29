using System.Net;
using System.Net.Http.Json;
using System.Data.Common;
using System.Text.Json;
using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// Project documents through the real API: the private upload path, who sees and edits what,
/// versions, preview and download, deletion, document types and storage cleanup.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ProjectDocumentTests(KennisbankFixture fixture)
{
    private string BucketName => fixture.Services.GetRequiredService<EnvironmentConfig>().GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    // Upload path

    [Fact]
    public async Task Upload_LandsUnderThePrefix_AndIsNotServedByTheFilesEndpoint()
    {
        var (member, _) = await fixture.LoginAsNewUserAsync();

        string objectName = await member.UploadProjectDocumentFileAsync();

        Assert.True(fixture.Storage.Exists(BucketName, $"project-docs/{objectName}"));
        Assert.False(fixture.Storage.Exists(BucketName, objectName));
        Assert.False((await member.GetAsync($"/files/{objectName}")).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Upload_OnlyAcceptsPdfAndWord()
    {
        var (member, _) = await fixture.LoginAsNewUserAsync();

        HttpResponseMessage response = await member.PostAsJsonAsync("/files/upload/init", new { fileName = "slides.pptx", fileSize = 10, purpose = "ProjectDocument" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_RejectsAnythingButAFreshProjectDocumentUpload()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        string libraryUpload = await creator.UploadFileAsync("document.pdf", "%PDF-1.4 library"u8.ToArray());
        Guid existingDocument = await creator.CreateProjectDocumentAsync(projectId);
        Guid existingVersion = await LatestVersionIdAsync(creator, projectId, existingDocument);

        // A library upload, a version that is already taken, an object that doesn't exist and garbage
        foreach (string objectName in new[] { libraryUpload, existingVersion.ToString(), Guid.NewGuid().ToString(), "not-a-guid" })
            Assert.Equal(HttpStatusCode.BadRequest, (await PostCreateAsync(creator, projectId, objectName)).StatusCode);

        Assert.True(fixture.Storage.Exists(BucketName, libraryUpload));
    }

    [Fact]
    public async Task Create_RejectsUnsupportedFileNames()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        string objectName = await creator.UploadProjectDocumentFileAsync();

        Assert.Equal(HttpStatusCode.BadRequest, (await PostCreateAsync(creator, projectId, objectName, "slides.pptx")).StatusCode);
    }

    [Fact]
    public async Task Create_NeedsProjectMembership_AndARootProject()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (outsider, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        HttpResponseMessage folder = await creator.PostAsJsonAsync($"/projects/{projectId}/folders", new { name = "Folder" });
        Guid folderId = (await folder.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("folderId").GetGuid();

        Assert.Equal(HttpStatusCode.Forbidden, (await PostCreateAsync(outsider, projectId, await outsider.UploadProjectDocumentFileAsync())).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostCreateAsync(creator, folderId, await creator.UploadProjectDocumentFileAsync())).StatusCode);
    }

    // Access

    [Fact]
    public async Task List_ShowsEachUserOnlyTheDocumentsTheyCanView()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (member, memberId) = await fixture.LoginAsNewUserAsync();
        var (outsider, outsiderId) = await fixture.LoginAsNewUserAsync();
        var (admin, _) = await fixture.LoginAsNewUserAsync("admin");
        HttpClient owner = await fixture.LoginAsAdminAsync();
        Guid projectId = await creator.CreateProjectAsync(memberId);

        Guid forMembers = await creator.CreateProjectDocumentAsync(projectId, "ProjectMembers");
        Guid restricted = await creator.CreateProjectDocumentAsync(projectId, "Restricted", grants: [Api.Grant(outsiderId, "Viewer")]);
        Guid forEveryone = await creator.CreateProjectDocumentAsync(projectId, "Everyone", "Viewer");

        Assert.Equivalent(new[] { forMembers, restricted, forEveryone }, await ListIdsAsync(creator, projectId));
        Assert.Equivalent(new[] { forMembers, forEveryone }, await ListIdsAsync(member, projectId));
        Assert.Equivalent(new[] { restricted, forEveryone }, await ListIdsAsync(outsider, projectId));
        // Admins get no special rights, only the owner account does
        Assert.Equivalent(new[] { forEveryone }, await ListIdsAsync(admin, projectId));
        Assert.Equivalent(new[] { forMembers, restricted, forEveryone }, await ListIdsAsync(owner, projectId));
    }

    [Fact]
    public async Task WithoutAccess_EverythingIsNotFound()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (admin, _) = await fixture.LoginAsNewUserAsync("admin");
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, "Restricted");
        Guid versionId = await LatestVersionIdAsync(creator, projectId, documentId);
        string url = $"/projects/{projectId}/documents/{documentId}";

        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{url}/versions/{versionId}/preview")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"{url}/versions/{versionId}/download")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await RenameAsync(admin, projectId, documentId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.DeleteAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Viewer_CanOpenButNotChange()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (viewer, viewerId) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, "Restricted", grants: [Api.Grant(viewerId, "Viewer")]);
        Guid versionId = await LatestVersionIdAsync(creator, projectId, documentId);
        string url = $"/projects/{projectId}/documents/{documentId}";

        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{url}/versions/{versionId}/preview")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await viewer.GetAsync($"{url}/versions/{versionId}/download")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await RenameAsync(viewer, projectId, documentId)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PutAsJsonAsync($"{url}/access", new { visibility = "Everyone", generalAccessType = "Editor", grants = Array.Empty<object>() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.PostAsJsonAsync($"{url}/versions", new { objectName = await viewer.UploadProjectDocumentFileAsync(), fileName = "document.pdf" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await viewer.DeleteAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Grant_RaisesMembersFromViewerToEditor()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (member, memberId) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync(memberId);
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, "ProjectMembers", "Viewer");

        Assert.Equal(HttpStatusCode.Forbidden, (await RenameAsync(member, projectId, documentId)).StatusCode);

        HttpResponseMessage setAccess = await creator.PutAsJsonAsync($"/projects/{projectId}/documents/{documentId}/access",
            new { visibility = "ProjectMembers", generalAccessType = "Viewer", grants = new[] { Api.Grant(memberId, "Editor") } });
        await Api.EnsureAsync(setAccess, HttpStatusCode.NoContent);

        Assert.Equal(HttpStatusCode.NoContent, (await RenameAsync(member, projectId, documentId)).StatusCode);
    }

    [Fact]
    public async Task Owner_CanRecoverADocumentNobodyElseCanReach()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (colleague, colleagueId) = await fixture.LoginAsNewUserAsync();
        HttpClient owner = await fixture.LoginAsAdminAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, "Restricted");

        HttpResponseMessage setAccess = await owner.PutAsJsonAsync($"/projects/{projectId}/documents/{documentId}/access",
            new { visibility = "Restricted", generalAccessType = "Viewer", grants = new[] { Api.Grant(colleagueId, "Editor") } });
        await Api.EnsureAsync(setAccess, HttpStatusCode.NoContent);

        Assert.Equal(HttpStatusCode.NoContent, (await RenameAsync(colleague, projectId, documentId)).StatusCode);
    }

    [Fact]
    public async Task SetAccess_RejectsDuplicateAndUnknownPeople()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (_, colleagueId) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId);
        string url = $"/projects/{projectId}/documents/{documentId}/access";

        object[] duplicate = [Api.Grant(colleagueId, "Viewer"), Api.Grant(colleagueId, "Editor")];
        object[] unknown = [Api.Grant(Guid.NewGuid(), "Viewer")];

        Assert.Equal(HttpStatusCode.BadRequest, (await creator.PutAsJsonAsync(url, new { visibility = "Restricted", generalAccessType = "Viewer", grants = duplicate })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await creator.PutAsJsonAsync(url, new { visibility = "Restricted", generalAccessType = "Viewer", grants = unknown })).StatusCode);
    }

    [Fact]
    public async Task DeletingAUser_RemovesTheirGrants_ButKeepsTheirDocuments()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (intern, internId) = await fixture.LoginAsNewUserAsync();
        HttpClient owner = await fixture.LoginAsAdminAsync();
        Guid projectId = await creator.CreateProjectAsync(internId);
        Guid sharedDocument = await creator.CreateProjectDocumentAsync(projectId, "Restricted", grants: [Api.Grant(internId, "Editor")]);
        Guid internsDocument = await intern.CreateProjectDocumentAsync(projectId);

        await Api.EnsureAsync(await owner.DeleteAsync($"/users/{internId}"), HttpStatusCode.OK);

        JsonElement detail = await GetDetailAsync(owner, projectId, sharedDocument);
        Assert.Empty(detail.GetProperty("grants").EnumerateArray());
        Assert.Contains(internsDocument, await ListIdsAsync(creator, projectId));
    }

    // Versions, preview and download

    [Fact]
    public async Task NewVersion_BecomesLatest_AndOlderVersionsStayDownloadable()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, content: "%PDF-1.4 first"u8.ToArray());
        Guid firstVersion = await LatestVersionIdAsync(creator, projectId, documentId);

        string upload = await creator.UploadProjectDocumentFileAsync(content: "%PDF-1.4 second"u8.ToArray());
        Guid secondVersion = await Api.ReadGuidAsync(await creator.PostAsJsonAsync($"/projects/{projectId}/documents/{documentId}/versions",
            new { objectName = upload, fileName = "document.pdf", note = "Second draft" }));

        JsonElement detail = await GetDetailAsync(creator, projectId, documentId);
        Assert.Equal(2, detail.GetProperty("latestVersion").GetProperty("versionNumber").GetInt32());
        Assert.Equal(secondVersion, detail.GetProperty("latestVersion").GetProperty("id").GetGuid());
        Assert.Equal(2, detail.GetProperty("versions").GetArrayLength());

        string url = $"/projects/{projectId}/documents/{documentId}/versions";
        Assert.Equal("%PDF-1.4 first", await creator.GetStringAsync($"{url}/{firstVersion}/download"));
        Assert.Equal("%PDF-1.4 second", await creator.GetStringAsync($"{url}/{secondVersion}/download"));
    }

    [Fact]
    public async Task NewVersion_LosingARace_IsAConflict_AndTheUploadCanBeRetried()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId);
        string versionsUrl = $"/projects/{projectId}/documents/{documentId}/versions";
        string losingUpload = await creator.UploadProjectDocumentFileAsync();
        string winningUpload = await creator.UploadProjectDocumentFileAsync();

        // Right after this service reads the highest version number, a colleague saves v2 through the API,
        // so its own v2 hits the unique index: the same interleaving as two people uploading at once
        await using DatabaseContext db = CreateContextWithInterceptor(new AfterMaxVersionQuery(async () =>
            await Api.ReadGuidAsync(await creator.PostAsJsonAsync(versionsUrl, new { objectName = winningUpload, fileName = "document.pdf" }))));
        ProjectDocumentService service = new(db, fixture.Storage, fixture.Services.GetRequiredService<EnvironmentConfig>());
        var document = (await service.GetAsync(projectId, documentId))!;

        Guid? lost = await service.AddVersionAsync(document, new() { ObjectName = losingUpload, FileName = "document.pdf" }, document.CreatedBy);

        Assert.Null(lost);
        Assert.True(fixture.Storage.Exists(BucketName, $"project-docs/{losingUpload}"));

        // Retrying the same upload through the API, as the frontend does after a 409, now becomes v3
        await Api.ReadGuidAsync(await creator.PostAsJsonAsync(versionsUrl, new { objectName = losingUpload, fileName = "document.pdf" }));
        JsonElement detail = await GetDetailAsync(creator, projectId, documentId);
        Assert.Equal(3, detail.GetProperty("latestVersion").GetProperty("versionNumber").GetInt32());
    }

    [Fact]
    public async Task PreviewAndDownload_HandleNonAsciiTitles()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, title: "Evaluatie café \"fase 2\"");
        Guid versionId = await LatestVersionIdAsync(creator, projectId, documentId);
        string url = $"/projects/{projectId}/documents/{documentId}/versions/{versionId}";

        HttpResponseMessage preview = await creator.GetAsync($"{url}/preview");
        await Api.EnsureAsync(preview, HttpStatusCode.OK);
        Assert.Equal("application/pdf", preview.Content.Headers.ContentType?.MediaType);
        Assert.Equal("inline", preview.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("Evaluatie café \"fase 2\" v1.pdf", preview.Content.Headers.ContentDisposition?.FileNameStar);

        HttpResponseMessage download = await creator.GetAsync($"{url}/download");
        await Api.EnsureAsync(download, HttpStatusCode.OK);
        Assert.Equal("attachment", download.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("Evaluatie café \"fase 2\" v1.pdf", download.Content.Headers.ContentDisposition?.FileNameStar);
    }

    [Fact]
    public async Task WordPreview_IsNotReadyUntilConverted_ButCanBeDownloaded()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId, fileName: "minutes.docx", content: "word"u8.ToArray());
        Guid versionId = await LatestVersionIdAsync(creator, projectId, documentId);
        string url = $"/projects/{projectId}/documents/{documentId}/versions/{versionId}";

        Assert.Equal(HttpStatusCode.Conflict, (await creator.GetAsync($"{url}/preview")).StatusCode);

        HttpResponseMessage download = await creator.GetAsync($"{url}/download");
        await Api.EnsureAsync(download, HttpStatusCode.OK);
        Assert.Equal("application/vnd.openxmlformats-officedocument.wordprocessingml.document", download.Content.Headers.ContentType?.MediaType);
    }

    // Deletion

    [Fact]
    public async Task Delete_RemovesTheDocumentAndItsFiles()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId);
        Guid versionId = await LatestVersionIdAsync(creator, projectId, documentId);
        string url = $"/projects/{projectId}/documents/{documentId}";

        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync(url)).StatusCode);

        Assert.Equal(HttpStatusCode.NotFound, (await creator.GetAsync(url)).StatusCode);
        Assert.False(fixture.Storage.Exists(BucketName, $"project-docs/{versionId}"));
    }

    [Fact]
    public async Task DeletingTheProject_DeletesItsDocuments()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId);

        await Api.EnsureAsync(await creator.DeleteAsync($"/projects/{projectId}"), HttpStatusCode.NoContent);

        using IServiceScope scope = fixture.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        Assert.False(await db.ProjectDocuments.AnyAsync(d => d.Id == documentId));
        Assert.False(await db.ProjectDocumentVersions.AnyAsync(v => v.DocumentId == documentId));
    }

    // Document types

    [Fact]
    public async Task Types_AreCaseInsensitive_AndOnlyUnusedOwnTypesCanBeDeleted()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (other, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        string name = Api.Unique("Progress report");

        Guid usedType = await Api.ReadGuidAsync(await creator.PutAsJsonAsync("/project-document-types", new { name }));
        Guid unusedType = await Api.ReadGuidAsync(await creator.PutAsJsonAsync("/project-document-types", new { name = Api.Unique("Quote") }));
        await creator.CreateProjectDocumentAsync(projectId, typeId: usedType);

        Assert.Equal(HttpStatusCode.Conflict, (await other.PutAsJsonAsync("/project-document-types", new { name = name.ToUpperInvariant() })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/project-document-types/{unusedType}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await creator.DeleteAsync($"/project-document-types/{ProjectDocumentTypes.Minutes}")).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await creator.DeleteAsync($"/project-document-types/{usedType}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/project-document-types/{unusedType}")).StatusCode);
    }

    // Storage cleanup

    [Fact]
    public async Task StorageCleanup_RemovesOrphanedProjectDocumentFiles_AndKeepsVersions()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId);
        string versionKey = $"project-docs/{await LatestVersionIdAsync(creator, projectId, documentId)}";
        string abandonedUpload = $"project-docs/{await creator.UploadProjectDocumentFileAsync()}";
        string stalePreview = $"project-docs/{Guid.NewGuid()}-preview";
        await fixture.Storage.UploadObjectAsync(BucketName, stalePreview, new MemoryStream([1]));

        // Past the 48 hour grace period, otherwise the cleanup treats everything as an upload in progress
        foreach (string key in new[] { versionKey, abandonedUpload, stalePreview })
            fixture.Storage.SetLastModified(BucketName, key, DateTime.UtcNow.AddDays(-3));

        await RunStorageCleanupAsync();

        Assert.True(fixture.Storage.Exists(BucketName, versionKey));
        Assert.False(fixture.Storage.Exists(BucketName, abandonedUpload));
        Assert.False(fixture.Storage.Exists(BucketName, stalePreview));
    }

    // Helpers

    /// <summary>Runs one cleanup pass and waits for it to finish; the hosted service's own loop never signals completion.</summary>
    private async Task RunStorageCleanupAsync()
    {
        StorageCleanupService cleanup = new(fixture.Services, fixture.Services.GetRequiredService<EnvironmentConfig>());
        IDbContextFactory<DatabaseContext> dbFactory = fixture.Services.GetRequiredService<IDbContextFactory<DatabaseContext>>();
        await cleanup.CleanupOrphanedObjects(fixture.Storage, dbFactory, CancellationToken.None);
    }

    /// <summary>A context on its own connection, like the app's, with an interceptor the app's own contexts don't have.</summary>
    private DatabaseContext CreateContextWithInterceptor(IInterceptor interceptor)
    {
        string connectionString = fixture.Services.GetRequiredService<EnvironmentConfig>().GetVariableValue(EnvironmentVariable.DATABASE_CONNECTION_STRING);
        NpgsqlDataSourceBuilder dataSource = new(connectionString);
        dataSource.UseVector();

        DbContextOptions<DatabaseContext> options = new DbContextOptionsBuilder<DatabaseContext>()
            .UseNpgsql(dataSource.Build(), o => o.UseVector())
            .AddInterceptors(interceptor)
            .Options;
        return new DatabaseContext(options);
    }

    /// <summary>Runs <paramref name="action"/> once, right after the query for the highest version number.</summary>
    private sealed class AfterMaxVersionQuery(Func<Task> action) : DbCommandInterceptor
    {
        private bool ran;

        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (!ran && command.CommandText.Contains("max(", StringComparison.OrdinalIgnoreCase) && command.CommandText.Contains("version-number"))
            {
                ran = true;
                await action();
            }
            return result;
        }
    }

    private static Task<HttpResponseMessage> PostCreateAsync(HttpClient client, Guid projectId, string objectName, string fileName = "document.pdf")
        => client.PostAsJsonAsync($"/projects/{projectId}/documents", new
        {
            objectName,
            fileName,
            title = Api.Unique("Document"),
            typeId = ProjectDocumentTypes.Minutes,
            visibility = "ProjectMembers",
            generalAccessType = "Editor"
        });

    private static Task<HttpResponseMessage> RenameAsync(HttpClient client, Guid projectId, Guid documentId)
        => client.PatchAsJsonAsync($"/projects/{projectId}/documents/{documentId}", new { title = Api.Unique("Renamed") });

    private static async Task<Guid[]> ListIdsAsync(HttpClient client, Guid projectId)
    {
        HttpResponseMessage response = await client.GetAsync($"/projects/{projectId}/documents");
        await Api.EnsureAsync(response, HttpStatusCode.OK);
        JsonElement list = await response.Content.ReadFromJsonAsync<JsonElement>();
        return [.. list.EnumerateArray().Select(d => d.GetProperty("id").GetGuid())];
    }

    private static async Task<JsonElement> GetDetailAsync(HttpClient client, Guid projectId, Guid documentId)
    {
        HttpResponseMessage response = await client.GetAsync($"/projects/{projectId}/documents/{documentId}");
        await Api.EnsureAsync(response, HttpStatusCode.OK);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<Guid> LatestVersionIdAsync(HttpClient client, Guid projectId, Guid documentId)
        => (await GetDetailAsync(client, projectId, documentId)).GetProperty("latestVersion").GetProperty("id").GetGuid();
}
