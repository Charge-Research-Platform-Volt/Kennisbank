using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// Items on a project page are copied field by field from the library item. A missing field falls back to its default,
/// and for the embedding status that is Pending: the inspector then always showed "Still processing embeddings".
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ProjectItemStatusTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task AnEmbeddedItem_IsNotShownAsStillProcessing_OnTheProjectPage()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid itemId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync());
        await fixture.WaitForEmbeddingAsync(itemId);

        await Api.EnsureAsync(await creator.PostAsync($"/projects/{projectId}/items/{itemId}", null), HttpStatusCode.NoContent);

        // Items come through the library view, so wait until it shows up
        JsonElement item = default;
        await Api.WaitUntilAsync(async () =>
        {
            JsonElement info = await creator.GetFromJsonAsync<JsonElement>($"/projects/{projectId}");
            item = info.GetProperty("items").EnumerateArray().FirstOrDefault(i => i.GetProperty("id").GetGuid() == itemId);
            return item.ValueKind == JsonValueKind.Object;
        }, $"item {itemId} in project {projectId}");

        Assert.Equal("Completed", item.GetProperty("embeddingStatus").GetString());
    }
}
