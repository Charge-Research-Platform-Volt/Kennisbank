using System.Net;
using System.Net.Http.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// Tags, regions, journals and resource types share one rule: their creator may rename or delete them
/// while nobody uses them yet; anything else needs an admin. Tags stand in for all four here.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class TaxonomyPermissionTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task Creator_CanRenameAndDeleteAnUnusedTag()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid tagId = await creator.CreateTagAsync();

        Assert.True((await creator.PatchAsJsonAsync($"/tags/{tagId}/name", new { name = Api.Unique("Renamed") })).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await creator.DeleteAsync($"/tags/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task OtherUser_CannotRenameOrDeleteSomeoneElsesTag()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (other, _) = await fixture.LoginAsNewUserAsync();
        Guid tagId = await creator.CreateTagAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await other.PatchAsJsonAsync($"/tags/{tagId}/name", new { name = Api.Unique("Renamed") })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/tags/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task Creator_CannotDeleteATagOnceItIsUsed_ButAdminCan()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid tagId = await creator.CreateTagAsync();
        await creator.CreateResourceAsync(await creator.CreateResourceTypeAsync(), tags: tagId.ToString());

        Assert.Equal(HttpStatusCode.Forbidden, (await creator.DeleteAsync($"/tags/{tagId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/tags/{tagId}")).StatusCode);
    }

    [Fact]
    public async Task Merging_IsAdminOnly()
    {
        var (user, _) = await fixture.LoginAsNewUserAsync();
        Guid keep = await user.CreateTagAsync();
        Guid remove = await user.CreateTagAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await user.PatchAsync($"/tags/merge/{keep}/{remove}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PatchAsync($"/regions/merge/{keep}/{remove}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PatchAsync($"/persons/merge/{keep}/{remove}", null)).StatusCode);
    }

    [Fact]
    public async Task DuplicateTagName_IsRejectedWithTheExistingId()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        string name = Api.Unique("Solar");
        Guid existing = await admin.CreateTagAsync(name);

        HttpResponseMessage response = await admin.PutAsJsonAsync("/tags", new { name });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(existing, await response.Content.ReadFromJsonAsync<Guid>());
    }
}
