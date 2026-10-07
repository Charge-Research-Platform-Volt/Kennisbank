using System.Net;
using System.Net.Http.Json;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

/// <summary>Chats are private: only their owner can read or change them.</summary>
[Collection(KennisbankCollection.Name)]
public class ChatOwnershipTests(KennisbankFixture fixture)
{
    [Fact]
    public async Task OtherUser_CannotReadOrChangeSomeoneElsesChat()
    {
        var (_, ownerId) = await fixture.LoginAsNewUserAsync();
        var (other, _) = await fixture.LoginAsNewUserAsync();
        Guid chatId = await CreateChatAsync(ownerId);

        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/chats/{chatId}/messages")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.GetAsync($"/chats/{chatId}/attachments")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.PatchAsJsonAsync($"/chats/{chatId}/title", "Hijacked")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await other.DeleteAsync($"/chats/{chatId}")).StatusCode);
    }

    [Fact]
    public async Task Owner_CanReadRenameAndDeleteTheirChat()
    {
        var (owner, ownerId) = await fixture.LoginAsNewUserAsync();
        Guid chatId = await CreateChatAsync(ownerId);

        Assert.Equal(HttpStatusCode.OK, (await owner.GetAsync($"/chats/{chatId}/messages")).StatusCode);
        Assert.True((await owner.PatchAsJsonAsync($"/chats/{chatId}/title", "Renamed")).IsSuccessStatusCode);
        Assert.True((await owner.DeleteAsync($"/chats/{chatId}")).IsSuccessStatusCode);
    }

    [Fact]
    public async Task ChatList_OnlyShowsYourOwnChats()
    {
        var (owner, ownerId) = await fixture.LoginAsNewUserAsync();
        var (other, _) = await fixture.LoginAsNewUserAsync();
        Guid chatId = await CreateChatAsync(ownerId);

        Assert.Contains(chatId.ToString(), await owner.GetStringAsync("/chats"));
        Assert.DoesNotContain(chatId.ToString(), await other.GetStringAsync("/chats"));
    }

    // Chats are normally created over SignalR when the first message is sent; create one directly instead
    private async Task<Guid> CreateChatAsync(Guid userId)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ChatService>()
            .CreateChatAsync(new ChatsCreateDto { UserId = userId, Title = "Private chat" });
    }
}
