using KnowledgeBank.Models;

namespace KnowledgeBank.Services.AI;

/// <summary>
/// Turns matched chunks into the passages the chat reads. A chunk with a position becomes a slice of the stored full
/// text: the chunk plus up to <see cref="ContextChars"/> on each side, cut at a paragraph break where possible. The model
/// sees the hit in its surroundings and without the context lines that were only added for embedding. Slices of one
/// document that overlap, or have only whitespace between them, are merged. A chunk without a position (metadata,
/// persons and organisations, or indexed before positions existed) is shown as it is. Passages keep the rank of their
/// best chunk. 
/// </summary>
public static class SourcePassages
{
    public const int ContextChars = 1000;

    public static List<string> Build(List<MatchedChunk> matches, ResourceText? text, int contextChars = ContextChars)
    {
        List<(int Start, int End, int Rank)> slices = [];
        List<(string Passage, int Rank)> passages = [];

        for (int rank = 0; rank < matches.Count; rank++)
        {
            MatchedChunk match = matches[rank];

            // A position outside the text means the index and the stored text are out of step: show the chunk as it is
            if (text != null && match.StartChar is int start && match.EndChar is int end && start >= 0 && start < end && end <= text.Text.Length)
                slices.Add((SnapStart(text.Text, start, contextChars), SnapEnd(text.Text, end, contextChars), rank));
            else
                passages.Add((match.Text, rank));
        }

        if (slices.Count > 0)
        {
            List<(int Position, string Heading)> headings = IngestionService.FindHeadings(text!.Text);
            foreach ((int start, int end, int rank) in Merge(text.Text, slices))
                passages.Add((Format(text, headings, start, end), rank));
        }

        return [.. passages.OrderBy(p => p.Rank).Select(p => p.Passage)];
    }

    private static List<(int Start, int End, int Rank)> Merge(string text, List<(int Start, int End, int Rank)> slices)
    {
        List<(int Start, int End, int Rank)> merged = [];

        foreach ((int start, int end, int rank) in slices.OrderBy(s => s.Start))
        {
            if (merged.Count > 0 && Touches(text, merged[^1].End, start))
            {
                (int lastStart, int lastEnd, int lastRank) = merged[^1];
                merged[^1] = (lastStart, Math.Max(lastEnd, end), Math.Min(lastRank, rank));
            }
            else merged.Add((start, end, rank));
        }

        return merged;
    }

    private static bool Touches(string text, int previousEnd, int start)
        => start <= previousEnd || text.AsSpan(previousEnd, start - previousEnd).IsWhiteSpace();

    /// <summary>The earliest paragraph start in the context before the chunk, else the earliest line, sentence or word start</summary>
    private static int SnapStart(string text, int start, int contextChars)
    {
        int from = Math.Max(0, start - contextChars);
        if (from == 0) return 0;

        int count = start - from;
        if (count <= 0) return start;

        int at = text.IndexOf("\n\n", from, count, StringComparison.Ordinal);
        if (at < 0) at = text.IndexOf('\n', from, count);
        if (at < 0) at = FirstSentenceEnd(text, from, start);
        if (at < 0) at = text.IndexOf(' ', from, count);
        if (at < 0) return start;

        // Past the break itself
        while (at < start && char.IsWhiteSpace(text[at])) at++;
        return at;
    }

    /// <summary>The last paragraph end in the context after the chunk, else the last line, sentence or word end</summary>
    private static int SnapEnd(string text, int end, int contextChars)
    {
        int to = Math.Min(text.Length, end + contextChars);
        if (to == text.Length) return text.Length;

        int count = to - end;
        if (count <= 0) return end;

        int at = text.LastIndexOf("\n\n", to - 1, count, StringComparison.Ordinal);
        if (at < 0) at = text.LastIndexOf('\n', to - 1, count);
        if (at < 0) at = LastSentenceEnd(text, end, to);
        if (at < 0) at = text.LastIndexOf(' ', to - 1, count);
        return at < 0 ? end : at;
    }

    // Both return the whitespace right after a sentence's closing punctuation
    private static int FirstSentenceEnd(string text, int from, int to)
    {
        for (int i = Math.Max(from, 1); i < to; i++)
            if (char.IsWhiteSpace(text[i]) && text[i - 1] is '.' or '?' or '!')
                return i;

        return -1;
    }

    private static int LastSentenceEnd(string text, int from, int to)
    {
        for (int i = to - 1; i >= Math.Max(from, 1); i--)
            if (char.IsWhiteSpace(text[i]) && text[i - 1] is '.' or '?' or '!')
                return i;

        return -1;
    }

    /// <summary>The slice with a label line saying where it is: its pages and the section opens in</summary>
    private static string Format(ResourceText text, List<(int Position, string Heading)> headings, int start, int end)
    {
        while (start < end && char.IsWhiteSpace(text.Text[start])) start++;
        while (end > start && char.IsWhiteSpace(text.Text[end - 1])) end--;

        List<string> location = [];

        if (text.PageAt(start) is int firstPage)
        {
            int lastPage = text.PageAt(end - 1) ?? firstPage;
            location.Add(lastPage > firstPage ? $"pp. {firstPage}-{lastPage}" : $"p. {firstPage}");
        }

        // Left out when the passage opens with the heading itself
        (int Position, string Heading) section = headings.LastOrDefault(h => h.Position <= start);
        if (section.Heading != null && section.Position < start)
            location.Add($"section \"{section.Heading}\"");

        string passage = text.Text[start..end];
        return location.Count > 0 ? $"({string.Join(", ", location)})\n{passage}" : passage;
    }
}