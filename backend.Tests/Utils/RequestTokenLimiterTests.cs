using KnowledgeBank.Utils;
using Microsoft.Extensions.Time.Testing;

namespace KnowledgeBank.Tests.Utils;

public class RequestTokenLimiterTests
{
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan PastWindow = Window + TimeSpan.FromMilliseconds(51);

    private readonly FakeTimeProvider time = new();
    private readonly RequestTokenLimiter limiter;

    public RequestTokenLimiterTests() => limiter = new RequestTokenLimiter(requestBudget: 2, tokenBudget: 1000, Window, time);

    [Fact]
    public async Task RequestBudget_BlocksTheNextRequestUntilTheWindowPasses()
    {
        Assert.True(limiter.WaitForSlotAsync().IsCompletedSuccessfully);
        Assert.True(limiter.WaitForSlotAsync().IsCompletedSuccessfully);

        Task third = limiter.WaitForSlotAsync();
        Assert.False(third.IsCompleted);

        time.Advance(PastWindow);
        await third.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task TokenBudget_BlocksEvenWhenRequestsAreAvailable()
    {
        await limiter.WaitForSlotAsync();
        limiter.RecordTokens(1000);

        Task next = limiter.WaitForSlotAsync();
        Assert.False(next.IsCompleted);

        time.Advance(PastWindow);
        await next.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void TokensUnderBudget_DoNotBlock()
    {
        limiter.RecordTokens(999);
        limiter.RecordTokens(0);
        limiter.RecordTokens(-10);

        Assert.True(limiter.WaitForSlotAsync().IsCompletedSuccessfully);
    }

    [Fact]
    public async Task Waiting_CanBeCancelled()
    {
        await limiter.WaitForSlotAsync();
        await limiter.WaitForSlotAsync();
        using CancellationTokenSource cts = new();

        Task wait = limiter.WaitForSlotAsync(cts.Token);
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => wait);
    }
}
