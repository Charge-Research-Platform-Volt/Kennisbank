using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

[Collection(KennisbankCollection.Name)]
public class MergeTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task MergingTags_MovesRelationsWithoutDuplicates_AndRemovesTheOldTag()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid typeId = await admin.CreateResourceTypeAsync();
        Guid keep = await admin.CreateTagAsync();
        Guid remove = await admin.CreateTagAsync();
        Guid hasBoth = await admin.CreateResourceAsync(typeId, tags: [keep.ToString(), remove.ToString()]);
        Guid hasOnlyRemoved = await admin.CreateResourceAsync(typeId, tags: remove.ToString());

        await Api.EnsureAsync(await admin.PatchAsync($"/tags/merge/{keep}/{remove}", null), System.Net.HttpStatusCode.NoContent);

        using IServiceScope scope = fixture.Services.CreateScope();
        DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        Assert.False(await db.Tags.AnyAsync(t => t.Id == remove));
        Assert.Equal([keep], await db.ResourceTagRelations.Where(r => r.ResourceId == hasBoth).Select(r => r.TagId).ToListAsync());
        Assert.Equal([keep], await db.ResourceTagRelations.Where(r => r.ResourceId == hasOnlyRemoved).Select(r => r.TagId).ToListAsync());
    }
}
