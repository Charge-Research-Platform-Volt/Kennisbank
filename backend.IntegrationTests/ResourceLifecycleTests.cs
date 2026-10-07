using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

[Collection(KennisbankCollection.Name)]
public class ResourceLifecycleTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task TagsGivenByName_AreReusedInsteadOfDuplicated()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid typeId = await admin.CreateResourceTypeAsync();
        string tagName = Api.Unique("Grid congestion");

        Guid first = await admin.CreateResourceAsync(typeId, tags: [tagName, tagName]);
        Guid second = await admin.CreateResourceAsync(typeId, tags: tagName);

        using IServiceScope scope = fixture.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        Guid tagId = Assert.Single(await db.Tags.Where(t => t.Name == tagName).Select(t => t.Id).ToListAsync());
        Assert.Equal(1, await db.ResourceTagRelations.CountAsync(r => r.ResourceId == first));
        Assert.True(await db.ResourceTagRelations.AnyAsync(r => r.ResourceId == second && r.TagId == tagId));
    }

    [Fact]
    public async Task PermanentDelete_RemovesChunksAndRelations()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid tag = await admin.CreateTagAsync();
        Guid resourceId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync(), tags: tag.ToString());
        await fixture.WaitForEmbeddingAsync(resourceId);

        await Api.EnsureAsync(await admin.DeleteAsync($"/resources/{resourceId}"), System.Net.HttpStatusCode.NoContent);

        using IServiceScope scope = fixture.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        Assert.False(await db.Resources.AnyAsync(r => r.Id == resourceId));
        Assert.False(await db.ResourceChunks.AnyAsync(c => c.ResourceId == resourceId));
        Assert.False(await db.ResourceTagRelations.AnyAsync(r => r.ResourceId == resourceId));
        Assert.True(await db.Tags.AnyAsync(t => t.Id == tag), "the tag itself stays");
    }
}
