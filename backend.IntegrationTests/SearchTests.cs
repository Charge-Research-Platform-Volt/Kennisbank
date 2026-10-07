using System.Net;
using System.Net.Http.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;

namespace KnowledgeBank.IntegrationTests;

/// <summary>Library search with a search term goes through Meilisearch (hybrid keyword + vector).</summary>
[Collection(KennisbankCollection.Name)]
public class SearchTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task Search_FindsTheResource_AndRespectsTagFilters()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid typeId = await admin.CreateResourceTypeAsync();
        Guid tag = await admin.CreateTagAsync();
        Guid otherTag = await admin.CreateTagAsync();
        string keyword = "zonnepark" + Guid.NewGuid().ToString("N")[..8];
        Guid resourceId = await admin.CreateResourceAsync(typeId, $"Study on {keyword}", tag.ToString());
        await fixture.WaitForEmbeddingAsync(resourceId);

        await Api.WaitUntilAsync(async () => (await SearchAsync(admin, keyword, tag)).Items.Any(i => i.Id == resourceId), "the resource to become searchable");

        Assert.DoesNotContain((await SearchAsync(admin, keyword, otherTag)).Items, i => i.Id == resourceId);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("x\"] OR type = \"person")]
    public async Task InvalidFilterValues_DoNotBreakSearch(string invalid)
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();

        HttpResponseMessage response = await admin.PostAsJsonAsync("/library", new
        {
            page = 1,
            pageSize = 10,
            search = "energy",
            filterOptions = new { tagFilter = new[] { invalid }, typeFilter = new[] { invalid }, journalFilter = new[] { invalid } }
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private static Task<LibraryResult> SearchAsync(HttpClient client, string term, Guid tag)
        => client.BrowseLibraryAsync(new
        {
            page = 1,
            pageSize = 20,
            search = term,
            filterOptions = new { typeFilter = new[] { "resource" }, tagFilter = new[] { tag.ToString() } }
        });
}
