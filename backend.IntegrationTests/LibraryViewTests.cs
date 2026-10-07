using System.Net.Http.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// The library and trash lists read from materialized views that database triggers refresh.
/// Every test filters on its own fresh tag, since all tests share one database.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class LibraryViewTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task NewResource_AppearsInTheLibraryImmediately()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid tag = await admin.CreateTagAsync();
        string title = Api.Unique("Heat pump study");

        Guid resourceId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync(), title, tag.ToString());

        LibraryItem item = Assert.Single((await BrowseResourcesAsync(admin, tag)).Items);
        Assert.Equal(resourceId, item.Id);
        Assert.Equal(title, item.Name);
    }

    [Fact]
    public async Task TrashAndRestore_MoveTheItemBetweenLibraryAndTrash()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid tag = await admin.CreateTagAsync();
        Guid resourceId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync(), tags: tag.ToString());

        await admin.PatchAsync($"/resources/{resourceId}/trash", null);
        Assert.Empty((await BrowseResourcesAsync(admin, tag)).Items);
        Assert.Contains(await admin.GetFromJsonAsync<TrashItem[]>("/library/trash") ?? [], t => t.Id == resourceId);

        await admin.PatchAsync($"/resources/{resourceId}/untrash", null);
        Assert.Single((await BrowseResourcesAsync(admin, tag)).Items);
        Assert.DoesNotContain(await admin.GetFromJsonAsync<TrashItem[]>("/library/trash") ?? [], t => t.Id == resourceId);
    }

    [Fact]
    public async Task TagFilter_AnyVersusAll()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid typeId = await admin.CreateResourceTypeAsync();
        Guid solar = await admin.CreateTagAsync();
        Guid wind = await admin.CreateTagAsync();
        Guid both = await admin.CreateResourceAsync(typeId, tags: [solar.ToString(), wind.ToString()]);
        Guid onlySolar = await admin.CreateResourceAsync(typeId, tags: solar.ToString());

        LibraryResult any = await BrowseResourcesAsync(admin, [solar, wind], mode: "any");
        LibraryResult all = await BrowseResourcesAsync(admin, [solar, wind], mode: "all");

        Assert.Equal(new[] { both, onlySolar }.Order(), any.Items.Select(i => i.Id).Order());
        Assert.Equal([both], all.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task SortingAndPaging_WorkTogether()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid typeId = await admin.CreateResourceTypeAsync();
        Guid tag = await admin.CreateTagAsync();
        string suffix = Api.Unique("");
        foreach (string name in new[] { "Charlie", "Alpha", "Bravo" })
            await admin.CreateResourceAsync(typeId, name + suffix, tag.ToString());

        LibraryResult page1 = await BrowseResourcesAsync(admin, [tag], page: 1, pageSize: 2, sortBy: "name");
        LibraryResult page2 = await BrowseResourcesAsync(admin, [tag], page: 2, pageSize: 2, sortBy: "name");

        Assert.Equal(3, page1.TotalCount);
        Assert.Equal(["Alpha" + suffix, "Bravo" + suffix], page1.Items.Select(i => i.Name));
        Assert.Equal(["Charlie" + suffix], page2.Items.Select(i => i.Name));
    }

    private static Task<LibraryResult> BrowseResourcesAsync(HttpClient client, Guid tag) => BrowseResourcesAsync(client, [tag]);

    private static Task<LibraryResult> BrowseResourcesAsync(HttpClient client, Guid[] tags, string mode = "any", int page = 1, int pageSize = 50, string? sortBy = null)
        => client.BrowseLibraryAsync(new
        {
            page,
            pageSize,
            sortBy,
            sortDirection = "asc",
            filterOptions = new { typeFilter = new[] { "resource" }, tagFilter = tags.Select(t => t.ToString()), tagFilterMode = mode }
        });
}
