using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// The Edit Project dialog: title and description, tags and members each have their own endpoint, so it makes
/// three calls. Sending everything to the main PATCH silently dropped the tags and members.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ProjectEditTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task EditDialogCalls_UpdateDetailsTagsAndMembers()
    {
        var (creator, creatorId) = await fixture.LoginAsNewUserAsync();
        var (_, colleagueId) = await fixture.LoginAsNewUserAsync();
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid tagId = await admin.CreateTagAsync();
        Guid projectId = await creator.CreateProjectAsync();
        string title = Api.Unique("Renamed project");

        await Api.EnsureAsync(await creator.PatchAsJsonAsync($"/projects/{projectId}", new { title, description = "About energy" }), HttpStatusCode.NoContent);
        await Api.EnsureAsync(await creator.PatchAsJsonAsync($"/projects/{projectId}/tags", new[] { tagId }), HttpStatusCode.NoContent);
        await Api.EnsureAsync(await creator.PatchAsJsonAsync($"/projects/{projectId}/members", new[] { creatorId, colleagueId }), HttpStatusCode.NoContent);

        JsonElement info = await creator.GetFromJsonAsync<JsonElement>($"/projects/{projectId}");
        Assert.Equal(title, info.GetProperty("rootProject").GetProperty("title").GetString());
        Assert.Equal("About energy", info.GetProperty("rootProject").GetProperty("description").GetString());
        Assert.Equal([tagId], info.GetProperty("tags").EnumerateArray().Select(t => t.GetProperty("id").GetGuid()));
        Assert.Equivalent(new[] { creatorId, colleagueId }, info.GetProperty("members").EnumerateArray().Select(m => m.GetProperty("id").GetGuid()).ToArray());
    }

    [Fact]
    public async Task EmptyDescription_ClearsIt_WhileNullLeavesItAlone()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        string title = Api.Unique("Project");

        await Api.EnsureAsync(await creator.PatchAsJsonAsync($"/projects/{projectId}", new { title, description = "Old text" }), HttpStatusCode.NoContent);
        await Api.EnsureAsync(await creator.PatchAsJsonAsync($"/projects/{projectId}", new { title, description = (string?)null }), HttpStatusCode.NoContent);
        Assert.Equal("Old text", await DescriptionAsync(creator, projectId));

        await Api.EnsureAsync(await creator.PatchAsJsonAsync($"/projects/{projectId}", new { title, description = "   " }), HttpStatusCode.NoContent);
        Assert.Null(await DescriptionAsync(creator, projectId));
    }

    private static async Task<string?> DescriptionAsync(HttpClient client, Guid projectId)
        => (await client.GetFromJsonAsync<JsonElement>($"/projects/{projectId}")).GetProperty("rootProject").GetProperty("description").GetString();
}
