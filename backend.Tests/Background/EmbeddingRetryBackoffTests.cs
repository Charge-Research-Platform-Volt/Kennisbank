using KnowledgeBank.Services.Background;

namespace KnowledgeBank.Tests.Background;

public class EmbeddingRetryBackoffTests
{
    private static readonly DateTime Now = new(2026, 10, 7, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void NeverCountedFailure_IsDueImmediately()
    {
        Assert.True(EmbeddingRetryService.IsDue(0, Now, Now));
        Assert.True(EmbeddingRetryService.IsDue(3, null, Now));
    }

    [Theory]
    [InlineData(1, 15)]
    [InlineData(2, 60)]
    [InlineData(3, 6 * 60)]
    [InlineData(4, 24 * 60)]
    public void Failure_IsDueExactlyWhenItsBackoffHasPassed(int failures, int backoffMinutes)
    {
        TimeSpan backoff = TimeSpan.FromMinutes(backoffMinutes);

        Assert.False(EmbeddingRetryService.IsDue(failures, Now - backoff + TimeSpan.FromSeconds(1), Now));
        Assert.True(EmbeddingRetryService.IsDue(failures, Now - backoff, Now));
    }

    [Fact]
    public void GivesUpAfterFiveFailures()
        => Assert.Equal(5, EmbeddingRetryService.MaxFailures);
}
