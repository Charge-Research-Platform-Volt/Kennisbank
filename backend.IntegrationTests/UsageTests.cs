using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using KnowledgeBank.Data;
using KnowledgeBank.IntegrationTests.Infrastructure;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Background;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace KnowledgeBank.IntegrationTests;

/// <summary>Usage totals for the owner dashboard: recorded in memory, flushed to the database as daily totals.</summary>
[Collection(KennisbankCollection.Name)]
public class UsageTests(KennisbankFixture fixture)
{
    private UsageFlushService Flush => fixture.Services.GetRequiredService<UsageFlushService>();

    [Fact]
    public async Task ContentSearch_IsCountedOncePerSearch_NotPerExtraPage()
    {
        HttpClient owner = await fixture.LoginAsAdminAsync();
        var (user, _) = await fixture.LoginAsNewUserAsync();

        await Flush.FlushAsync();
        long before = await TodayCountAsync(owner, "search.content");

        await Api.EnsureAsync(await user.PostAsJsonAsync("/library/search-content", new { search = Api.Unique("usage"), pageSize = 20 }), HttpStatusCode.OK);
        await Api.EnsureAsync(await user.PostAsJsonAsync("/library/search-content", new { search = "usage", pageSize = 20, excludeIds = new[] { Guid.NewGuid() } }), HttpStatusCode.OK);
        await Flush.FlushAsync();

        Assert.Equal(before + 1, await TodayCountAsync(owner, "search.content"));
    }

    [Fact]
    public async Task Flush_AddsToExistingDailyTotals()
    {
        UsageMetrics metrics = fixture.Services.GetRequiredService<UsageMetrics>();
        string metric = $"test.{Guid.NewGuid():N}";

        metrics.Record(metric, duration: TimeSpan.FromMilliseconds(100));
        await Flush.FlushAsync();
        metrics.Record(metric, success: false, duration: TimeSpan.FromMilliseconds(300));
        await Flush.FlushAsync();

        using IServiceScope scope = fixture.Services.CreateScope();
        UsageCounter row = await scope.ServiceProvider.GetRequiredService<DatabaseContext>().UsageCounters.SingleAsync(c => c.Metric == metric);

        Assert.Equal(2, row.Count);
        Assert.Equal(1, row.FailureCount);
        Assert.Equal(400, row.TotalDurationMs);
    }

    [Fact]
    public async Task Usage_IncludesActivityFromStoredData()
    {
        HttpClient owner = await fixture.LoginAsAdminAsync();
        long before = await TodayCountAsync(owner, "activity.resources");

        await owner.CreateResourceAsync(await owner.CreateResourceTypeAsync());

        Assert.Equal(before + 1, await TodayCountAsync(owner, "activity.resources"));
    }

    private static async Task<long> TodayCountAsync(HttpClient owner, string key)
    {
        JsonElement usage = await owner.GetFromJsonAsync<JsonElement>("/dashboard/usage?days=1");

        foreach (JsonElement series in usage.GetProperty("series").EnumerateArray())
        {
            if (series.GetProperty("key").GetString() == key)
                return series.GetProperty("counts")[0].GetInt64();
        }

        return 0;
    }
}
