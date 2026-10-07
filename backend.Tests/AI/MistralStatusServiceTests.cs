using KnowledgeBank.Hubs;
using KnowledgeBank.Services.AI;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;

namespace KnowledgeBank.Tests.AI;

public class MistralStatusServiceTests
{
    private readonly IAppHubClient allClients = Substitute.For<IAppHubClient>();
    private readonly MistralStatusService service;

    public MistralStatusServiceTests()
    {
        var hub = Substitute.For<IHubContext<AppHub, IAppHubClient>>();
        hub.Clients.All.Returns(allClients);
        service = new MistralStatusService(hub);
    }

    [Fact]
    public void StartsOperational()
        => Assert.Equal(MistralAvailability.Operational, service.Current.Status);

    [Fact]
    public async Task SingleFailure_DoesNotMarkDown()
    {
        await service.ReportFailure("HTTP 503");

        Assert.Equal(MistralAvailability.Operational, service.Current.Status);
        await allClients.DidNotReceive().MistralStatusChanged(Arg.Any<MistralStatusPayload>());
    }

    [Fact]
    public async Task TwoConsecutiveFailures_MarkDownAndBroadcastOnce()
    {
        await service.ReportFailure("first");
        await service.ReportFailure("second");
        await service.ReportFailure("third");

        MistralStatusSnapshot current = service.Current;
        Assert.Equal(MistralAvailability.Down, current.Status);
        Assert.NotNull(current.DownSinceUtc);
        Assert.Equal("third", current.LastError);
        await allClients.Received(1).MistralStatusChanged(Arg.Is<MistralStatusPayload>(p => p.Status == "down"));
    }

    [Fact]
    public async Task SuccessAfterDown_MarksOperationalAndBroadcasts()
    {
        await service.ReportFailure("first");
        await service.ReportFailure("second");
        await service.ReportSuccess();

        MistralStatusSnapshot current = service.Current;
        Assert.Equal(MistralAvailability.Operational, current.Status);
        Assert.Null(current.DownSinceUtc);
        Assert.Null(current.LastError);
        await allClients.Received(1).MistralStatusChanged(Arg.Is<MistralStatusPayload>(p => p.Status == "operational"));
    }

    [Fact]
    public async Task SuccessInBetween_ResetsFailureCount()
    {
        await service.ReportFailure("first");
        await service.ReportSuccess();
        await service.ReportFailure("second");

        Assert.Equal(MistralAvailability.Operational, service.Current.Status);
    }

    [Fact]
    public async Task BroadcastFailure_DoesNotBreakStatusTracking()
    {
        allClients.MistralStatusChanged(Arg.Any<MistralStatusPayload>()).Returns(Task.FromException(new InvalidOperationException("hub down")));

        await service.ReportFailure("first");
        await service.ReportFailure("second");

        Assert.Equal(MistralAvailability.Down, service.Current.Status);
    }

    [Theory]
    [InlineData(500, true)]
    [InlineData(502, true)]
    [InlineData(503, true)]
    [InlineData(504, true)]
    [InlineData(529, true)]
    [InlineData(400, false)]
    [InlineData(401, false)]
    [InlineData(404, false)]
    [InlineData(429, false)]
    public void IsOutageStatusCode_OnlyServerSideProblems(int statusCode, bool expected)
        => Assert.Equal(expected, MistralStatusService.IsOutageStatusCode(statusCode));
}
