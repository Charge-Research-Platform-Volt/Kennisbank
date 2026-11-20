using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Models.Chat;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// Project chats belong to the root project, wherever in the tree they were started, and their tools see the whole
/// tree. They never fall back to the whole library.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ChatProjectScopeTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task AChatStartedInAFolder_IsStoredOnTheRoot()
    {
        var (creator, userId) = await fixture.LoginAsNewUserAsync();
        Guid rootId = await creator.CreateProjectAsync();
        Guid subfolderId = await creator.CreateFolderAsync(await creator.CreateFolderAsync(rootId));

        Guid chatId = await CreateChatAsync(userId, subfolderId);

        using IServiceScope scope = fixture.Services.CreateScope();
        Chats chat = await scope.ServiceProvider.GetRequiredService<DatabaseContext>().Chats.SingleAsync(c => c.Id == chatId);
        Assert.Equal(rootId, chat.ProjectId);
    }

    [Fact]
    public async Task TheHistory_IsTheSameFromTheRootAndFromAFolder_AndSeparateFromTheLibrary()
    {
        var (creator, userId) = await fixture.LoginAsNewUserAsync();
        Guid rootId = await creator.CreateProjectAsync();
        Guid folderId = await creator.CreateFolderAsync(rootId);

        Guid fromRoot = await CreateChatAsync(userId, rootId);
        Guid fromFolder = await CreateChatAsync(userId, folderId);
        Guid libraryChat = await CreateChatAsync(userId, null);

        Guid[] seenFromRoot = await ChatIdsAsync(creator, $"/chats?projectId={rootId}");
        Assert.Equivalent(new[] { fromRoot, fromFolder }, seenFromRoot);
        Assert.Equivalent(seenFromRoot, await ChatIdsAsync(creator, $"/chats?projectId={folderId}"));
        Assert.Equal([libraryChat], await ChatIdsAsync(creator, "/chats"));
    }

    [Fact]
    public async Task AnUnknownProject_IsNotFound_InsteadOfListingLibraryChats()
    {
        var (creator, userId) = await fixture.LoginAsNewUserAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await creator.GetAsync($"/chats?projectId={Guid.NewGuid()}")).StatusCode);
        await Assert.ThrowsAsync<ArgumentException>(() => CreateChatAsync(userId, Guid.NewGuid()));
    }

    [Fact]
    public async Task TheSearchScope_IsEveryItemInTheTree_AndNothingOutsideIt()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        HttpClient admin = await fixture.LoginAsAdminAsync();
        Guid typeId = await admin.CreateResourceTypeAsync();

        Guid rootId = await creator.CreateProjectAsync();
        Guid folderId = await creator.CreateFolderAsync(rootId);
        Guid subfolderId = await creator.CreateFolderAsync(folderId);
        Guid otherProjectId = await creator.CreateProjectAsync();

        Guid inRoot = await AddNewItemAsync(creator, admin, typeId, rootId);
        Guid inSubfolder = await AddNewItemAsync(creator, admin, typeId, subfolderId);
        await AddNewItemAsync(creator, admin, typeId, otherProjectId);
        // The same item in two folders counts once
        await Api.EnsureAsync(await creator.PostAsync($"/projects/{folderId}/items/{inRoot}", null), HttpStatusCode.NoContent);

        using IServiceScope scope = fixture.Services.CreateScope();
        Guid[] scoped = await scope.ServiceProvider.GetRequiredService<ProjectService>().GetTreeItemIdsAsync(rootId);

        Assert.Equivalent(new[] { inRoot, inSubfolder }, scoped);
        Assert.Equal(scoped.Length, scoped.Distinct().Count());
    }

    [Fact]
    public async Task SearchingAProjectWithoutItems_NeverFallsBackToTheWholeLibrary()
    {
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        Guid rootId = await creator.CreateProjectAsync();
        await creator.CreateFolderAsync(rootId);

        using IServiceScope scope = fixture.Services.CreateScope();
        string result = await scope.ServiceProvider.GetRequiredService<ChatToolExecutor>().ExecuteAsync(
            new MistralToolCall("call-1", "search_library", """{"query":"energy"}"""),
            "What do we know about energy?",
            new ChatToolContext(Guid.NewGuid(), Guid.NewGuid().ToString(), IsOwner: false, rootId, "test-connection"),
            CancellationToken.None);

        Assert.Equal("This project has no library items yet.", result);
    }

    private async Task<Guid> AddNewItemAsync(HttpClient member, HttpClient admin, Guid typeId, Guid projectId)
    {
        Guid itemId = await admin.CreateResourceAsync(typeId);
        await Api.EnsureAsync(await member.PostAsync($"/projects/{projectId}/items/{itemId}", null), HttpStatusCode.NoContent);
        return itemId;
    }

    // Chats are normally created over SignalR when the first message is sent; create one directly instead
    private async Task<Guid> CreateChatAsync(Guid userId, Guid? projectId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ChatService>()
            .CreateChatAsync(new ChatsCreateDto { UserId = userId, Title = "Project chat", ProjectId = projectId });
    }

    private static async Task<Guid[]> ChatIdsAsync(HttpClient client, string url)
        => (await client.GetFromJsonAsync<Dictionary<string, JsonElement[]>>(url))!
            .Values.SelectMany(day => day).Select(c => c.GetProperty("id").GetGuid()).ToArray();
}
