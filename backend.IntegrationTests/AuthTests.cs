using System.Net;
using System.Net.Http.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;

namespace KnowledgeBank.IntegrationTests;

[Collection(KennisbankCollection.Name)]
public class AuthTests(KennisbankFixture fixture)
{
    [Theory]
    [InlineData("GET", "/chats")]
    [InlineData("GET", "/library/trash")]
    [InlineData("GET", "/users/me")]
    [InlineData("GET", "/Auth/ping")]
    public async Task NotLoggedIn_Returns401(string method, string path)
    {
        HttpResponseMessage response = await fixture.CreateAnonymousClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task NotLoggedIn_CannotBrowseLibrary()
    {
        HttpResponseMessage response = await fixture.CreateAnonymousClient().PostAsJsonAsync("/library", new { page = 1, pageSize = 10 });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task WrongPassword_IsRejected()
    {
        HttpResponseMessage response = await fixture.CreateAnonymousClient()
            .PostAsJsonAsync("/Auth/login?useCookies=true", new { email = KennisbankFixture.OwnerEmail, password = "wrong-Passw0rd!" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/admin/reembed")]
    [InlineData("POST", "/admin/reindex-search")]
    [InlineData("POST", "/admin/cleanup-orphans")]
    [InlineData("GET", "/users/combined")]
    [InlineData("GET", "/users/roles")]
    [InlineData("GET", "/tags/suggestions")]
    [InlineData("GET", "/persons/suggestions")]
    public async Task RegularUser_IsForbiddenFromAdminEndpoints(string method, string path)
    {
        var (client, _) = await fixture.LoginAsNewUserAsync();

        HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), path));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegularUser_CannotPostChangelog()
    {
        var (client, _) = await fixture.LoginAsNewUserAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/Changelog", new { title = "Hacked", body = "Not allowed" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RegularUser_CannotChangeAnotherUsersRole()
    {
        var (client, _) = await fixture.LoginAsNewUserAsync();
        var (_, otherId) = await fixture.LoginAsNewUserAsync();

        HttpResponseMessage response = await client.PatchAsJsonAsync($"/users/{otherId}/role", new { roleName = "admin" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task OwnerAccount_IsProtectedEvenFromAdmins()
    {
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid ownerId = await fixture.GetOwnerIdAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PatchAsJsonAsync($"/users/{ownerId}/role", new { roleName = "user" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PatchAsJsonAsync($"/users/{ownerId}/email", new { email = "other@kennisbank.test" })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PatchAsJsonAsync($"/users/{ownerId}/name", new { newFirstName = "New", newLastName = "Name" })).StatusCode);
    }
}
