using System.Net;
using KnowledgeBank.IntegrationTests.Infrastructure;

namespace KnowledgeBank.IntegrationTests;

/// <summary>Moving to the trash is open to everyone; restoring and permanently deleting are admin-only.</summary>
[Collection(KennisbankCollection.Name)]
public class ResourcePermissionTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task RegularUser_CanTrashAResource()
    {
        var (user, _) = await fixture.LoginAsNewUserAsync();
        Guid resourceId = await user.CreateResourceAsync(await user.CreateResourceTypeAsync());

        Assert.Equal(HttpStatusCode.NoContent, (await user.PatchAsync($"/resources/{resourceId}/trash", null)).StatusCode);
    }

    [Fact]
    public async Task RegularUser_CannotRestoreOrPermanentlyDeleteAResource()
    {
        var (user, _) = await fixture.LoginAsNewUserAsync();
        Guid resourceId = await user.CreateResourceAsync(await user.CreateResourceTypeAsync());
        await user.PatchAsync($"/resources/{resourceId}/trash", null);

        Assert.Equal(HttpStatusCode.Forbidden, (await user.PatchAsync($"/resources/{resourceId}/untrash", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.DeleteAsync($"/resources/{resourceId}")).StatusCode);
    }

    [Fact]
    public async Task Admin_CanRestoreAndPermanentlyDeleteAResource()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid resourceId = await admin.CreateResourceAsync(await admin.CreateResourceTypeAsync());
        await admin.PatchAsync($"/resources/{resourceId}/trash", null);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsync($"/resources/{resourceId}/untrash", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/resources/{resourceId}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync($"/resources/{resourceId}")).StatusCode);
    }

    [Fact]
    public async Task Persons_FollowTheSameRules()
    {
        var (user, _) = await fixture.LoginAsNewUserAsync();
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid personId = await user.CreatePersonAsync();

        Assert.Equal(HttpStatusCode.NoContent, (await user.PatchAsync($"/persons/{personId}/trash", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.PatchAsync($"/persons/{personId}/untrash", null)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await user.DeleteAsync($"/persons/{personId}")).StatusCode);

        Assert.Equal(HttpStatusCode.NoContent, (await admin.PatchAsync($"/persons/{personId}/untrash", null)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await admin.DeleteAsync($"/persons/{personId}")).StatusCode);
    }
}
