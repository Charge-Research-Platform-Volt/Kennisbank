using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace KnowledgeBank.Models;

/// <summary>
/// The full extracted text of a resource. Kept apart from the resource row so loading resources doesn't pull
/// megabytes of text along. Chunk positions point into <see cref="Text"/>, and <see cref="PageStarts"/> maps a
/// position to its page. 
/// </summary>
[Table("resource-texts")]
public class ResourceText
{
    [Key]
    [Column("resource-id")]
    public required Guid ResourceId { get; set; }

    [Column("text")]
    public required string Text { get; set; }

    /// <summary>Where each page starts in Text: page n starts at PageStarts[n - 1]. Null when the source has no pages (websites, docx, plain text)</summary>
    [Column("page-starts")]
    public int[]? PageStarts { get; set; }

    [Column("extracted-at")]
    public required DateTime ExtractedAt { get; set; }

    [JsonIgnore]
    public Resource? Resource { get; set; }

    /// <summary>The 1-based page a position in the text falls on, or null when there are no pages</summary>
    public int? PageAt(int position) => PageAt(PageStarts, position);

    /// <summary>
    /// The last page starting at or before the position. Pages without text start where the next one does,
    /// so a position there resolves to the page that actually holds it.
    /// </summary>
    public static int? PageAt(int[]? pageStarts, int position)
    {
        if (pageStarts is not { Length: > 0}) return null;

        int low = 0, high = pageStarts.Length - 1, index = 0;
        while (low <= high)
        {
            int middle = (low + high) / 2;
            if (pageStarts[middle] <= position) { index = middle; low = middle + 1; }
            else high = middle - 1;
        }

        return index + 1;
    }
}