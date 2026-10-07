using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using KnowledgeBank.Hubs;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Tests.Helpers;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace KnowledgeBank.Tests.AI;

public class MistralHttpClientTests
{
    private readonly MistralStatusService statusService;
    private readonly MistralHttpClient client;

    public MistralHttpClientTests()
    {
        var hub = Substitute.For<IHubContext<AppHub, IAppHubClient>>();
        hub.Clients.All.Returns(Substitute.For<IAppHubClient>());
        statusService = new MistralStatusService(hub);
        client = new MistralHttpClient(TestEnvironment.CreateConfig(), statusService);
    }

    // GetRetryDelay

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1, 2)]
    [InlineData(2, 4)]
    public void GetRetryDelay_WithoutRetryAfter_UsesExponentialBackoff(int attempt, int expectedSeconds)
        => Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), client.GetRetryDelay(null, attempt));

    [Fact]
    public void GetRetryDelay_UsesRetryAfterDelta()
        => Assert.Equal(TimeSpan.FromSeconds(5), client.GetRetryDelay(WithRetryAfter(new RetryConditionHeaderValue(TimeSpan.FromSeconds(5))), 0));

    [Fact]
    public void GetRetryDelay_CapsRetryAfterAt30Seconds()
        => Assert.Equal(TimeSpan.FromSeconds(30), client.GetRetryDelay(WithRetryAfter(new RetryConditionHeaderValue(TimeSpan.FromMinutes(5))), 0));

    [Fact]
    public void GetRetryDelay_UsesRetryAfterDate()
    {
        TimeSpan delay = client.GetRetryDelay(WithRetryAfter(new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddSeconds(10))), 0);

        // The header has one-second resolution, so allow for rounding
        Assert.InRange(delay, TimeSpan.FromSeconds(8), TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void GetRetryDelay_RetryAfterDateInPast_FallsBackToBackoff()
        => Assert.Equal(TimeSpan.FromSeconds(2), client.GetRetryDelay(WithRetryAfter(new RetryConditionHeaderValue(DateTimeOffset.UtcNow.AddMinutes(-1))), 1));

    // SendWithRetryAsync (Retry-After: 1 keeps the real waits short)

    [Fact]
    public async Task SendWithRetry_SuccessOnFirstTry_SendsOnce()
    {
        FakeHandler handler = new(Respond(HttpStatusCode.OK));

        using HttpResponseMessage response = await Send(handler);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task SendWithRetry_RateLimitedThenOk_Retries()
    {
        FakeHandler handler = new(Respond(HttpStatusCode.TooManyRequests, retryAfterSeconds: 1), Respond(HttpStatusCode.OK));

        using HttpResponseMessage response = await Send(handler);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, handler.Calls);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendWithRetry_NonRetryableStatus_IsReturnedWithoutRetry(HttpStatusCode status)
    {
        FakeHandler handler = new(Respond(status), Respond(HttpStatusCode.OK));

        using HttpResponseMessage response = await Send(handler);

        Assert.Equal(status, response.StatusCode);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task SendWithRetry_KeepsFailingWith503_GivesUpAfterThreeAttempts()
    {
        FakeHandler handler = new(
            Respond(HttpStatusCode.ServiceUnavailable, retryAfterSeconds: 1),
            Respond(HttpStatusCode.ServiceUnavailable, retryAfterSeconds: 1),
            Respond(HttpStatusCode.ServiceUnavailable, retryAfterSeconds: 1),
            Respond(HttpStatusCode.OK));

        using HttpResponseMessage response = await Send(handler);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task SendWithRetry_OutageStatuses_MarkMistralDown()
    {
        using (await Send(new FakeHandler(Respond(HttpStatusCode.InternalServerError)))) { }
        using (await Send(new FakeHandler(Respond(HttpStatusCode.InternalServerError)))) { }

        Assert.Equal(MistralAvailability.Down, statusService.Current.Status);
    }

    [Fact]
    public async Task SendWithRetry_ClientErrors_DoNotMarkMistralDown()
    {
        using (await Send(new FakeHandler(Respond(HttpStatusCode.BadRequest)))) { }
        using (await Send(new FakeHandler(Respond(HttpStatusCode.BadRequest)))) { }

        Assert.Equal(MistralAvailability.Operational, statusService.Current.Status);
    }

    [Fact]
    public async Task SendWithRetry_TransportErrors_RetryThenThrow()
    {
        FakeHandler handler = new(Throw(), Throw(), Throw());

        await Assert.ThrowsAsync<HttpRequestException>(() => Send(handler));
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task SendWithRetry_CallerCancellation_IsNotRetried()
    {
        FakeHandler handler = new(Respond(HttpStatusCode.OK));
        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Send(handler, cts.Token));
        Assert.Equal(0, handler.Calls);
    }

    // Response parsing

    [Theory]
    [InlineData("\"plain answer\"", "plain answer")]
    [InlineData("""[{"type":"thinking","thinking":[]},{"type":"text","text":"answer"}]""", "answer")]
    [InlineData("""[{"type":"thinking","thinking":[]}]""", "")]
    [InlineData("[]", "")]
    [InlineData("null", "")]
    [InlineData("42", "")]
    public void ExtractTextContent_HandlesStringAndChunkArrays(string json, string expected)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.Equal(expected, MistralHttpClient.ExtractTextContent(doc.RootElement));
    }

    [Theory]
    [InlineData("Prompt exceeds the model's context length", true)]
    [InlineData("MAXIMUM CONTEXT reached", true)]
    [InlineData("too many tokens in request", true)]
    [InlineData("Invalid model name", false)]
    [InlineData("", false)]
    public void IsContextLengthError_RecognisesKnownMessages(string body, bool expected)
        => Assert.Equal(expected, MistralHttpClient.IsContextLengthError(body));

    // Helpers

    private Task<HttpResponseMessage> Send(FakeHandler handler, CancellationToken ct = default)
    {
        HttpClient httpClient = new(handler) { BaseAddress = new Uri("http://mistral.test/") };
        return client.SendWithRetryAsync(httpClient, () => new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions"), HttpCompletionOption.ResponseContentRead, ct);
    }

    private static HttpResponseMessage WithRetryAfter(RetryConditionHeaderValue value)
    {
        HttpResponseMessage response = new(HttpStatusCode.TooManyRequests);
        response.Headers.RetryAfter = value;
        return response;
    }

    private static Func<HttpResponseMessage> Respond(HttpStatusCode status, int? retryAfterSeconds = null) => () =>
    {
        HttpResponseMessage response = new(status);
        if (retryAfterSeconds is int seconds)
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(seconds));
        return response;
    };

    private static Func<HttpResponseMessage> Throw() => () => throw new HttpRequestException("connection refused");

    private sealed class FakeHandler(params Func<HttpResponseMessage>[] responses) : HttpMessageHandler
    {
        public int Calls { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Func<HttpResponseMessage> next = responses[Math.Min(Calls, responses.Length - 1)];
            Calls++;
            return Task.FromResult(next());
        }
    }
}
