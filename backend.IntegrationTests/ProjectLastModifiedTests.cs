using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// The project page's "Last modified" column: who added or uploaded each row and when, with what the avatar needs.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ProjectLastModifiedTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task FoldersAndItems_SayWhoAddedThem_AndWhen()
    {
        var (creator, creatorId) = await fixture.LoginAsNewUserAsync();
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid projectId = await creator.CreateProjectAsync();
        DateTime before = DateTime.UtcNow.AddSeconds(-5);

        Guid folderId = await creator.CreateFolderAsync(projectId);
        Guid itemId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync());
        await Api.EnsureAsync(await creator.PostAsync($"/projects/{projectId}/items/{itemId}", null), HttpStatusCode.NoContent);

        JsonElement info = await GetInfoWithItemAsync(creator, projectId, itemId);

        JsonElement folder = info.GetProperty("folders").EnumerateArray().Single(f => f.GetProperty("id").GetGuid() == folderId);
        AssertIsUser(folder.GetProperty("addedBy"), creatorId);
        Assert.True(folder.GetProperty("createdOn").GetDateTime() >= before);

        JsonElement item = info.GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == itemId);
        AssertIsUser(item.GetProperty("addedBy"), creatorId);
        Assert.True(item.GetProperty("addedOn").GetDateTime() >= before);
    }

    [Fact]
    public async Task OldItemLinks_WithoutADateOrAKnownUser_StillShow()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid itemId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync());

        // A link from before "added-on" existed, by a user id that no longer resolves
        using (IServiceScope scope = fixture.Services.CreateScope())
        {
            DatabaseContext db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
            db.ProjectItemRelations.Add(new ProjectItemRelation { ProjectId = projectId, ItemId = itemId, AddedBy = Guid.NewGuid().ToString() });
            await db.SaveChangesAsync();
        }

        JsonElement item = (await GetInfoWithItemAsync(creator, projectId, itemId))
            .GetProperty("items").EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == itemId);

        Assert.Equal(JsonValueKind.Null, item.GetProperty("addedOn").ValueKind);
        Assert.Equal(JsonValueKind.Null, item.GetProperty("addedBy").GetProperty("id").ValueKind);
        Assert.Equal("Unknown", item.GetProperty("addedBy").GetProperty("name").GetString());
    }

    [Fact]
    public async Task DocumentVersions_SayWhoUploadedThem()
    {
        var (creator, creatorId) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();
        Guid documentId = await creator.CreateProjectDocumentAsync(projectId);

        JsonElement listed = (await creator.GetFromJsonAsync<JsonElement>($"/projects/{projectId}/documents"))
            .EnumerateArray().Single(d => d.GetProperty("id").GetGuid() == documentId);
        AssertIsUser(listed.GetProperty("latestVersion").GetProperty("uploadedBy"), creatorId);

        JsonElement detail = await creator.GetFromJsonAsync<JsonElement>($"/project-documents/{documentId}");
        AssertIsUser(detail.GetProperty("versions")[0].GetProperty("uploadedBy"), creatorId);
    }

    private static void AssertIsUser(JsonElement user, Guid userId)
    {
        Assert.Equal(userId, user.GetProperty("id").GetGuid());
        // LoginAsNewUserAsync names every user "Test User"; no avatar uploaded, so the frontend shows initials
        Assert.Equal("Test User", user.GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Null, user.GetProperty("customAvatarVersion").ValueKind);
    }

    /// <summary>Project info, waiting until the item shows up: items come through the library view.</summary>
    private static async Task<JsonElement> GetInfoWithItemAsync(HttpClient client, Guid projectId, Guid itemId)
    {
        JsonElement info = default;
        await Api.WaitUntilAsync(async () =>
        {
            info = await client.GetFromJsonAsync<JsonElement>($"/projects/{projectId}");
            return info.GetProperty("items").EnumerateArray().Any(i => i.GetProperty("id").GetGuid() == itemId);
        }, $"item {itemId} in project {projectId}");
        return info;
    }
}
