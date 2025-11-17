using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

/// <summary>
/// Indicates the precision of a publication date
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PublicationDatePrecision
{
    /// <summary>
    /// Only the year is known (e.g., 2020)
    /// Date is stored as YYYY-01-01
    /// </summary>
    Year = 0,

    /// <summary>
    /// Year and month are known (e.g., May 2020)
    /// Date is stored as YYYY-MM-01
    /// </summary>
    Month = 1,

    /// <summary>
    /// Full date is known (e.g., May 15, 2020)
    /// Date is stored as YYYY-MM-DD
    /// </summary>
    Day = 2
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)
