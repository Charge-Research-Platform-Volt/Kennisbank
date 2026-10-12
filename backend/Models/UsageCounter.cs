using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Models;

/// <summary>
/// A daily total for one usage metric (e.g. "search.content"). Deliberately has no user column: only how often
/// something happened, how often it failed and how long it took in total.
/// </summary>
[Table("usage_counters")]
[PrimaryKey(nameof(Date), nameof(Metric))]
public class UsageCounter
{
    [Column("date")]
    public DateOnly Date { get; set; }

    [Column("metric")]
    public required string Metric { get; set; }

    [Column("count")]
    public long Count { get; set; }

    [Column("failure_count")]
    public long FailureCount { get; set; }

    [Column("total_duration_ms")]
    public long TotalDurationMs { get; set; }
}
