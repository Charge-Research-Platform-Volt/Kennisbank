using KnowledgeBank.Services.AI;
using System.Text.Json;
using KnowledgeBank.Models;

namespace KnowledgeBank.Tests.AI;

public class ChatEvalTests
{
    private const string Uuid1 = "3f2b1c4e-9a7d-4b2e-8f1a-0c9d8e7f6a5b";
    private const string Uuid2 = "a1b2c3d4-e5f6-4a7b-8c9d-0e1f2a3b4c5d";
    private const string Uuid3 = "0f1e2d3c-4b5a-4968-8776-655443322110";

    [Fact]
    public void CheckCitations_AllCitedIdsInToolResults_NoneUnsupported()
    {
        var (cited, unsupported) = ChatEvalService.CheckCitations(
            $"Solar is cheaper.[SRC:{Uuid1}] Wind too.[SRC:{Uuid2}]",
            [$"Report (resource)\nCite as: [SRC:{Uuid1}]", $"- Author: Jane Doe [SRC:{Uuid2}]"]);

        Assert.Equal(2, cited);
        Assert.Empty(unsupported);
    }

    [Fact]
    public void CheckCitations_IdMissingFromToolResults_IsUnsupported()
    {
        var (cited, unsupported) = ChatEvalService.CheckCitations(
            $"Solar is cheaper.[SRC:{Uuid1}] Made up.[SRC:{Uuid3}]",
            [$"Cite as: [SRC:{Uuid1}]"]);

        Assert.Equal(2, cited);
        Assert.Equal([Uuid3], unsupported);
    }

    [Fact]
    public void CheckCitations_GroupedAndRepeatedMarkers_CountEachIdOnce()
    {
        var (cited, unsupported) = ChatEvalService.CheckCitations(
            $"A.[SRC:{Uuid1}, {Uuid2}] B.[SRC:{Uuid1}]",
            [$"Cite as: [SRC:{Uuid1}]", $"Cite as: [SRC:{Uuid2}]"]);

        Assert.Equal(2, cited);
        Assert.Empty(unsupported);
    }

    [Fact]
    public void CheckCitations_IgnoresCase()
    {
        var (_, unsupported) = ChatEvalService.CheckCitations($"A.[SRC:{Uuid1}]", [$"Cite as: [SRC:{Uuid1.ToUpperInvariant()}]"]);

        Assert.Empty(unsupported);
    }

    [Fact]
    public void CheckCitations_NoCitations_CountsZero()
    {
        var (cited, unsupported) = ChatEvalService.CheckCitations("The library has nothing on this.", []);

        Assert.Equal(0, cited);
        Assert.Empty(unsupported);
    }

    [Fact]
    public void ParseCases_CaseWithoutTurns_Throws()
        => Assert.Throws<InvalidDataException>(() => ChatEvalService.ParseCases("""[{ "id": "empty", "turns": [] }]"""));

    [Fact]
    public void ParseCases_DuplicateIds_Throws()
        => Assert.Throws<InvalidDataException>(() => ChatEvalService.ParseCases("""[{ "id": "a", "turns": ["x"] }, { "id": "a", "turns": ["y"] }]"""));

    [Fact]
    public void LoadCases_ShippedFile_IsValid()
        => Assert.NotEmpty(ChatEvalService.LoadCases());

        private static ChatEvalRun RunWith(ChatEvalRunStatus status, params ChatEvalTurnResult[] turns) => new()
    {
        Model = "mistral-medium-test",
        Status = status,
        Results = JsonSerializer.Serialize(new List<ChatEvalCaseResult> { new() { Id = "case", Turns = [.. turns] } })
    };

    [Fact]
    public void Summarize_AveragesOnlyAnsweredTurns()
    {
        var summary = ChatEvalService.Summarize(RunWith(ChatEvalRunStatus.Completed,
            new ChatEvalTurnResult { Question = "a", TotalMs = 10_000, Rounds = 2, PromptTokens = 100, Citations = 3, UnsupportedCitations = [Uuid1] },
            new ChatEvalTurnResult { Question = "b", TotalMs = 20_000, Rounds = 4, PromptTokens = 300, Citations = 1 },
            new ChatEvalTurnResult { Question = "c", TotalMs = 90_000, Error = "Mistral down" }), evalRunning: false);

        Assert.Equal(3, summary.Turns);
        Assert.Equal(1, summary.FailedTurns);
        Assert.Equal(15_000, summary.AvgTotalMs);
        Assert.Equal(3, summary.AvgRounds);
        Assert.Equal(400, summary.PromptTokens);
        Assert.Equal(4, summary.Citations);
        Assert.Equal(1, summary.UnsupportedCitations);
    }

    [Theory]
    [InlineData(true, "Running")]
    [InlineData(false, "Interrupted")]
    public void Summarize_RunningRun_IsInterruptedWhenNoEvalIsActive(bool evalRunning, string expected)
        => Assert.Equal(expected, ChatEvalService.Summarize(RunWith(ChatEvalRunStatus.Running), evalRunning).Status);

    [Fact]
    public void Summarize_NoAnsweredTurns_LeavesAveragesEmpty()
    {
        var summary = ChatEvalService.Summarize(RunWith(ChatEvalRunStatus.Failed,
            new ChatEvalTurnResult { Question = "a", Error = "boom" }), evalRunning: false);

        Assert.Null(summary.AvgTotalMs);
        Assert.Null(summary.AvgFirstTokenMs);
    }

}
