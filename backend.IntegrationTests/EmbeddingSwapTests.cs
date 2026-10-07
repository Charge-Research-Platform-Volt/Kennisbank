using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using Meilisearch;
using Meilisearch.QueryParameters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// Re-embedding swaps chunks only once new embeddings exist, so a failed re-embed never leaves a
/// resource without chunks (the delete-embed-store order this replaced could).
/// </summary>
[Collection(KennisbankCollection.Name)]
public class EmbeddingSwapTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task NewResource_IsEmbeddedIntoPostgresAndMeilisearch()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid resourceId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync());

        Resource resource = await fixture.WaitForEmbeddingAsync(resourceId);

        Assert.Equal(EmbeddingStatus.Completed, resource.EmbeddingStatus);
        Assert.NotEmpty(await ChunkTextsAsync(resourceId));
        await Api.WaitUntilAsync(async () => await MeiliChunkCountAsync(resourceId) > 0, "chunks in Meilisearch");
    }

    [Fact]
    public async Task FailedReembed_KeepsTheExistingChunks()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        string title = Api.Unique("Swap test");
        Guid resourceId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync(), title);
        await fixture.WaitForEmbeddingAsync(resourceId);
        List<string> before = await ChunkTextsAsync(resourceId);
        await Api.WaitUntilAsync(async () => await MeiliChunkCountAsync(resourceId) > 0, "chunks in Meilisearch");

        fixture.Ai.FailEmbeddingsContaining(title);
        try
        {
            using IServiceScope scope = fixture.Services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<IngestionService>().RunResourcePipelineAsync(resourceId);
        }
        finally
        {
            fixture.Ai.StopFailing(title);
        }

        Resource after = await fixture.WaitForEmbeddingAsync(resourceId);
        Assert.Equal(EmbeddingStatus.Failed, after.EmbeddingStatus);
        Assert.Equal(1, after.EmbeddingFailures);
        Assert.Equal(before, await ChunkTextsAsync(resourceId));
        Assert.True(await MeiliChunkCountAsync(resourceId) > 0, "Meilisearch chunks were removed");
    }

    private async Task<List<string>> ChunkTextsAsync(Guid resourceId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        return await db.ResourceChunks.Where(c => c.ResourceId == resourceId).OrderBy(c => c.ChunkPart).Select(c => c.ChunkText).ToListAsync();
    }

    private async Task<int> MeiliChunkCountAsync(Guid resourceId)
    {
        MeilisearchClient meili = fixture.Services.GetRequiredService<MeilisearchClient>();
        var result = await meili.Index("chunks").GetDocumentsAsync<Dictionary<string, object>>(new DocumentsQuery
        {
            Filter = $"parentId = \"{resourceId}\"",
            Fields = ["id"]
        });
        return result.Results.Count();
    }
}
