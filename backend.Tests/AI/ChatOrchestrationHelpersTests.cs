using KnowledgeBank.Models;
using KnowledgeBank.Services.AI;

namespace KnowledgeBank.Tests.AI;

public class ChatOrchestrationHelpersTests
{
    private const string Uuid1 = "3f2b1c4e-9a7d-4b2e-8f1a-0c9d8e7f6a5b";
    private const string Uuid2 = "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";

    [Theory]
    [InlineData("Solar is getting cheaper.[SRC:" + Uuid1 + "]", "Solar is getting cheaper.")]
    [InlineData("Solar is getting cheaper.[ATTACH:" + Uuid1 + "]", "Solar is getting cheaper.")]
    [InlineData("Solar is getting cheaper.[SRC:" + Uuid1 + ", " + Uuid2 + "]", "Solar is getting cheaper.")]
    [InlineData("[SRC:" + Uuid1 + "]Wind too.[SRC:" + Uuid2 + "]", "Wind too.")]
    public void StripCitationMarkers_RemovesSourceAndAttachmentMarkers(string input, string expected)
        => Assert.Equal(expected, ChatOrchestrationService.StripCitationMarkers(input));

    [Theory]
    [InlineData("See [1] and [AI]context[/AI].")]
    [InlineData("An array like [a, b] stays.")]
    public void StripCitationMarkers_LeavesOtherBracketsAlone(string input)
        => Assert.Equal(input, ChatOrchestrationService.StripCitationMarkers(input));

    [Fact]
    public void StripAiTags_RemovesOpeningAndClosingTags()
        => Assert.Equal("General context here.", ChatOrchestrationService.StripAiTags("[AI]General context here.[/AI]"));

    [Fact]
    public void BuildMessageWithAttachments_NoAttachments_ReturnsMessageUnchanged()
    {
        var (content, note) = ChatOrchestrationService.BuildMessageWithAttachments("Hello", []);

        Assert.Equal("Hello", content);
        Assert.Null(note);
    }

    [Fact]
    public void BuildMessageWithAttachments_SmallFile_IsInlinedIntoMessage()
    {
        MessageAttachments small = Attachment("notes.txt", isChunked: false, extractedText: "Plan: finish phase 1");

        var (content, note) = ChatOrchestrationService.BuildMessageWithAttachments("Summarise this", [small]);

        Assert.StartsWith("Summarise this", content);
        Assert.Contains($"[Attached file: notes.txt (id: {small.Id})]", content);
        Assert.Contains("Plan: finish phase 1", content);
        Assert.Null(note);
    }

    [Fact]
    public void BuildMessageWithAttachments_LargeFile_GoesIntoNoteInsteadOfMessage()
    {
        MessageAttachments large = Attachment("report.pdf", isChunked: true);

        var (content, note) = ChatOrchestrationService.BuildMessageWithAttachments("Summarise this", [large]);

        Assert.Equal("Summarise this", content);
        Assert.NotNull(note);
        Assert.Contains("report.pdf", note);
        Assert.Contains("search_attachment_content", note);
    }

    [Fact]
    public void BuildMessageWithAttachments_Mixed_InlinesSmallAndNotesLarge()
    {
        MessageAttachments small = Attachment("notes.txt", isChunked: false, extractedText: "inline me");
        MessageAttachments large = Attachment("report.pdf", isChunked: true);

        var (content, note) = ChatOrchestrationService.BuildMessageWithAttachments("Hi", [small, large]);

        Assert.Contains("inline me", content);
        Assert.DoesNotContain("report.pdf", content);
        Assert.Contains("report.pdf", note);
        Assert.DoesNotContain("notes.txt", note);
    }

    [Fact]
    public void BuildAttachmentsListText_LabelsSizeAndIncludesCiteLine()
    {
        MessageAttachments small = Attachment("notes.txt", isChunked: false, description: "Meeting notes");
        MessageAttachments large = Attachment("report.pdf", isChunked: true);

        string text = ChatOrchestrationService.BuildAttachmentsListText([small, large]);

        Assert.Contains($"- notes.txt (id: {small.Id}) [small document] — Meeting notes — Cite as: [ATTACH:{small.Id}]", text);
        Assert.Contains($"- report.pdf (id: {large.Id}) [large document] — Cite as: [ATTACH:{large.Id}]", text);
    }

    private static MessageAttachments Attachment(string fileName, bool isChunked, string? extractedText = null, string? description = null) => new()
    {
        Id = Guid.NewGuid(),
        ChatId = Guid.NewGuid(),
        FileName = fileName,
        Extension = Path.GetExtension(fileName),
        IsChunked = isChunked,
        ExtractedText = extractedText,
        Description = description,
        CreatedOn = DateTime.UtcNow
    };
}
