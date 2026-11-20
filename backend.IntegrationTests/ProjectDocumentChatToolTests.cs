using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Models.Chat;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Tests.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

/// <summary>
/// The chat's project document tools follow the same access rules as the app: a user only ever gets what they could
/// open themselves, and only from the chat's own project. Runs the tool handlers directly, as there's no SignalR client.
/// </summary>
[Collection(KennisbankCollection.Name)]
public class ProjectDocumentChatToolTests(KennisbankFixture fixture)
{
    private const string NoDocuments = "This project has no documents you can access.";
    private const string NotFound = "Error: document not found in this project";

    /// <summary>A project with one document for all members and one only its creator can open.</summary>
    private sealed record Setup(Guid ProjectId, Guid CreatorId, Guid MemberId, Guid OutsiderId,
        Guid ForMembers, string MembersMarker, Guid Restricted, string RestrictedMarker);

    private async Task<Setup> CreateSetupAsync()
    {
        var (creator, creatorId) = await fixture.LoginAsNewUserAsync();
        var (_, memberId) = await fixture.LoginAsNewUserAsync();
        var (_, outsiderId) = await fixture.LoginAsNewUserAsync();
        Guid projectId = await creator.CreateProjectAsync(memberId);

        string membersMarker = Marker("steering");
        string restrictedMarker = Marker("salaries");
        Guid forMembers = await creator.CreateProjectDocumentAsync(projectId, visibility: "ProjectMembers",
            fileName: "minutes.docx", content: TestDocuments.Docx("Minutes of the steering group", membersMarker));
        Guid restricted = await creator.CreateProjectDocumentAsync(projectId, visibility: "Restricted",
            fileName: "budget.docx", content: TestDocuments.Docx("Budget", restrictedMarker));

        await WaitUntilEmbeddedAsync(forMembers);
        await WaitUntilEmbeddedAsync(restricted);

        return new Setup(projectId, creatorId, memberId, outsiderId, forMembers, membersMarker, restricted, restrictedMarker);
    }

    [Fact]
    public async Task List_ShowsOnlyWhatTheUserCanOpen()
    {
        Setup s = await CreateSetupAsync();

        string forMember = await RunAsync("list_project_documents", "{}", Context(s.MemberId, s.ProjectId));
        Assert.Contains($"[DOC:{s.ForMembers}]", forMember);
        Assert.DoesNotContain(s.Restricted.ToString(), forMember);

        string forCreator = await RunAsync("list_project_documents", "{}", Context(s.CreatorId, s.ProjectId));
        Assert.Contains($"[DOC:{s.ForMembers}]", forCreator);
        Assert.Contains($"[DOC:{s.Restricted}]", forCreator);

        Assert.Equal(NoDocuments, await RunAsync("list_project_documents", "{}", Context(s.OutsiderId, s.ProjectId)));
    }

    [Fact]
    public async Task TheAppOwner_SeesEveryDocument_LikeInTheApp()
    {
        Setup s = await CreateSetupAsync();

        string result = await RunAsync("list_project_documents", "{}", Context(Guid.NewGuid(), s.ProjectId, isAppOwner: true));

        Assert.Contains($"[DOC:{s.ForMembers}]", result);
        Assert.Contains($"[DOC:{s.Restricted}]", result);
    }

    [Fact]
    public async Task Search_FindsWhatTheUserCanOpen_AndNeverWhatTheyCant()
    {
        Setup s = await CreateSetupAsync();
        ChatToolContext member = Context(s.MemberId, s.ProjectId);

        // Meilisearch indexes asynchronously, so wait until the member's document shows up
        string found = "";
        await Api.WaitUntilAsync(async () =>
        {
            found = await RunAsync("search_project_documents", Args(("query", s.MembersMarker)), member);
            return found.Contains(s.MembersMarker);
        }, "the members document to become searchable");
        Assert.Contains($"[DOC:{s.ForMembers}]", found);

        // The creator can find the restricted one, so it is searchable: the member just doesn't get it
        await Api.WaitUntilAsync(async () =>
            (await RunAsync("search_project_documents", Args(("query", s.RestrictedMarker)), Context(s.CreatorId, s.ProjectId))).Contains(s.RestrictedMarker),
            "the restricted document to become searchable");

        string hidden = await RunAsync("search_project_documents", Args(("query", s.RestrictedMarker)), member);
        Assert.DoesNotContain(s.RestrictedMarker, hidden);
        Assert.DoesNotContain(s.Restricted.ToString(), hidden);

        // Narrowing to a document the user can't open matches nothing instead of searching it
        Assert.Equal("No project documents you can access match that.",
            await RunAsync("search_project_documents", Args(("query", s.RestrictedMarker), ("documentId", s.Restricted.ToString())), member));
    }

    [Fact]
    public async Task Search_WithNothingToOpen_DoesNotSearch()
    {
        Setup s = await CreateSetupAsync();

        Assert.Equal("No project documents you can access match that.",
            await RunAsync("search_project_documents", Args(("query", s.MembersMarker)), Context(s.OutsiderId, s.ProjectId)));
    }

    [Fact]
    public async Task Read_ReturnsTheText_OnlyForADocumentTheUserCanOpen()
    {
        Setup s = await CreateSetupAsync();
        ChatToolContext member = Context(s.MemberId, s.ProjectId);

        string read = await RunAsync("read_project_document", Args(("documentId", s.ForMembers.ToString())), member);
        Assert.Contains(s.MembersMarker, read);
        Assert.Contains($"Cite as: [DOC:{s.ForMembers}]", read);

        // No access and doesn't exist look the same, so the model can't learn a document exists
        Assert.Equal(NotFound, await RunAsync("read_project_document", Args(("documentId", s.Restricted.ToString())), member));
        Assert.Equal(NotFound, await RunAsync("read_project_document", Args(("documentId", Guid.NewGuid().ToString())), member));
    }

    [Fact]
    public async Task ADocument_IsOnlyUsableFromItsOwnProjectsChat()
    {
        Setup s = await CreateSetupAsync();
        var (creator, _) = await fixture.LoginAsNewUserAsync();
        // The creator can open it, but this chat belongs to another project they're also in
        Guid otherProjectId = await creator.CreateProjectAsync(s.CreatorId);
        ChatToolContext otherProject = Context(s.CreatorId, otherProjectId);
        ChatToolContext libraryChat = Context(s.CreatorId, projectId: null);

        Assert.Equal(NotFound, await RunAsync("read_project_document", Args(("documentId", s.ForMembers.ToString())), otherProject));
        Assert.Equal(NoDocuments, await RunAsync("list_project_documents", "{}", otherProject));

        Assert.Equal(NotFound, await RunAsync("read_project_document", Args(("documentId", s.ForMembers.ToString())), libraryChat));
        Assert.Equal(NoDocuments, await RunAsync("list_project_documents", "{}", libraryChat));
    }

    private static ChatToolContext Context(Guid userId, Guid? projectId, bool isAppOwner = false)
        => new(Guid.NewGuid(), userId.ToString(), isAppOwner, projectId, "test-connection");

    private static string Args(params (string Name, string Value)[] args)
        => System.Text.Json.JsonSerializer.Serialize(args.ToDictionary(a => a.Name, a => a.Value));

    private static string Marker(string word) => word + Guid.NewGuid().ToString("N")[..8];

    private async Task<string> RunAsync(string tool, string arguments, ChatToolContext context)
    {
        using IServiceScope scope = fixture.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<ChatToolExecutor>()
            .ExecuteAsync(new MistralToolCall("call-1", tool, arguments), "test question", context, CancellationToken.None);
    }

    private async Task WaitUntilEmbeddedAsync(Guid documentId)
        => await Api.WaitUntilAsync(async () =>
        {
            using IServiceScope scope = fixture.Services.CreateScope();
            EmbeddingStatus status = await scope.ServiceProvider.GetRequiredService<DatabaseContext>().ProjectDocumentVersions
                .Where(v => v.DocumentId == documentId)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => v.EmbeddingStatus)
                .FirstAsync();
            Assert.NotEqual(EmbeddingStatus.Failed, status);
            return status == EmbeddingStatus.Completed;
        }, $"project document {documentId} to be embedded");
}
