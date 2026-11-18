using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// The folder list behind the Move dialog and the inspector's add-to-project dialog. The frontend draws the tree by
/// indenting on depth alone, so every folder must come directly before its own subfolders.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ProjectFolderTreeTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task AllFolders_ComeInTreeOrder_AlphabeticalPerLevel()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();

        // Created out of order on purpose
        Guid beta = await creator.CreateFolderAsync(projectId, "Beta");
        Guid alpha = await creator.CreateFolderAsync(projectId, "Alpha");
        await creator.CreateFolderAsync(beta, "Beta two");
        await creator.CreateFolderAsync(alpha, "Alpha one");
        await creator.CreateFolderAsync(beta, "Beta one");

        JsonElement folders = await creator.GetFromJsonAsync<JsonElement>($"/projects/{projectId}/folders");

        Assert.Equal(
            ["1 Alpha", "2 Alpha one", "1 Beta", "2 Beta one", "2 Beta two"],
            folders.EnumerateArray().Select(f => $"{f.GetProperty("depth").GetInt32()} {f.GetProperty("title").GetString()}"));
    }

    [Fact]
    public async Task AProjectWithoutFolders_ListsNone()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync();

        JsonElement folders = await creator.GetFromJsonAsync<JsonElement>($"/projects/{projectId}/folders");

        Assert.Empty(folders.EnumerateArray());
    }
}
