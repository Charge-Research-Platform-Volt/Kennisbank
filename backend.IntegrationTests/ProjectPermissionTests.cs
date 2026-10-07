using System.Net;
using System.Net.Http.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// Changing a project needs membership (or admin). Adding folders and items is deliberately open to
/// everyone, so anyone can contribute; removing an item is allowed for whoever added it.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ProjectPermissionTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task NonMember_CannotChangeTheProject()
    {
        var (member, _) = await fixture.LoginAsNewUserAsync();
        var (outsider, outsiderId) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await member.CreateProjectAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PatchAsJsonAsync($"/projects/{projectId}", new { title = Api.Unique("Hijacked") })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PatchAsJsonAsync($"/projects/{projectId}/tags", Array.Empty<Guid>())).StatusCode);
        // Making themselves the only member is exactly what the check has to stop
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.PatchAsJsonAsync($"/projects/{projectId}/members", new[] { outsiderId })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.DeleteAsync($"/projects/{projectId}")).StatusCode);
    }

    [Fact]
    public async Task Creator_IsAMemberAndCanChangeTheProject()
    {
        var (member, _) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await member.CreateProjectAsync();

        Assert.True((await member.PatchAsJsonAsync($"/projects/{projectId}", new { title = Api.Unique("Renamed") })).IsSuccessStatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await member.DeleteAsync($"/projects/{projectId}")).StatusCode);
    }

    [Fact]
    public async Task AddedMember_CanChangeTheProject()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        var (colleague, colleagueId) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync(colleagueId);

        Assert.True((await colleague.PatchAsJsonAsync($"/projects/{projectId}", new { description = "Updated by a member" })).IsSuccessStatusCode);
    }

    [Fact]
    public async Task Admin_CanChangeAnyProject()
    {
        var (member, _) = await fixture.LoginAsNewUserAsync();
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid projectId = await member.CreateProjectAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/projects/{projectId}")).StatusCode);
    }

    [Fact]
    public async Task NonMember_CanAddFoldersAndItems_AndRemoveOnlyTheirOwnItems()
    {
        var (member, _) = await fixture.LoginAsNewUserAsync();
        var (outsider, _) = await fixture.LoginAsNewUserAsync();
        Guid typeId = await member.CreateResourceTypeAsync();
        Guid projectId = await member.CreateProjectAsync();
        Guid membersItem = await member.CreateResourceAsync(typeId);
        Guid outsidersItem = await outsider.CreateResourceAsync(typeId);
        Assert.Equal(HttpStatusCode.NoContent, (await member.PostAsync($"/projects/{projectId}/items/{membersItem}", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Created, (await outsider.PostAsJsonAsync($"/projects/{projectId}/folders", new { name = "Outsider folder" })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await outsider.PostAsync($"/projects/{projectId}/items/{outsidersItem}", null)).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await outsider.DeleteAsync($"/projects/{projectId}/items/{membersItem}")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await outsider.DeleteAsync($"/projects/{projectId}/items/{outsidersItem}")).StatusCode);
    }

    [Fact]
    public async Task AddingTheSameItemTwice_IsAConflict()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid projectId = await admin.CreateProjectAsync();
        Guid itemId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync());

        await admin.PostAsync($"/projects/{projectId}/items/{itemId}", null);

        Assert.Equal(HttpStatusCode.Conflict, (await admin.PostAsync($"/projects/{projectId}/items/{itemId}", null)).StatusCode);
    }
}
