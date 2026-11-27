namespace KnowledgeBank.Services.AI;

/// <summary>A chunk's position in the text it was cut from: <c>text[Start..End]</c></summary>
public readonly record struct ChunkSpan(int Start, int End)
{
    public int Length => End - Start;
}

/// <summary>
/// Cuts text into overlapping chunks and returns their positions, so every chunk can be traced back to its place in
/// the full text (for page numbers, and for widening a search hit to the text around it).
/// A chunk ends at the best boundary in its second half: a paragraph break, else a line break, else a sentence end,
/// else any whitespace, and only mid-word when there is none. The overlap with the next chunk starts at a line,
/// sentence or word boundary as well.
/// </summary>
public static class DocumentChunker
{
    // ~512 tokens of typical English or Dutch prose
    public const int DefaultChunkChars = 2000;
    public const int DefaultOverlapChars = 300;

    public static List<ChunkSpan> Split(string text, int chunkChars = DefaultChunkChars, int overlapChars = DefaultOverlapChars)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkChars, 2);
        if (overlapChars < 0 || overlapChars >= chunkChars / 2)
            throw new ArgumentOutOfRangeException(nameof(overlapChars), "The overlap must be at least 0 and less than half the chunk size");

        List<ChunkSpan> spans = [];
        int start = SkipWhitespace(text, 0);

        while (start < text.Length)
        {
            int limit = Math.Min(start + chunkChars, text.Length);
            int end = limit == text.Length ? limit : FindBreak(text, start + chunkChars / 2, limit);

            spans.Add(new ChunkSpan(start, TrimEnd(text, start, end)));
            if (end >= text.Length) break;

            // Overlap below half the chunk size and a break in the chunk's second half keep this past start
            int overlapFrom = Math.Max(end - overlapChars, start + 1);
            start = SkipWhitespace(text, FindOverlapStart(text, overlapFrom, end));
        }

        return spans;
    }

    /// <summary>Position just after the best boundary in <c>text[min..limit]</c>, or <paramref name="limit"/> when there is none</summary>
    private static int FindBreak(string text, int min, int limit)
    {
        int line = -1, sentence = -1, space = -1;

        for (int i = limit - 1; i >= min; i--)
        {
            char c = text[i];

            if (c == '\n')
            {
                int previous = i - 1;
                if (previous >= min && text[previous] == '\r') previous--;
                if (previous >= min && text[previous] == '\n') return i + 1; // Paragraph break: the best option

                if (line < 0) line = i + 1;
            }
            else if (sentence < 0 && (c is '.' or '?' or '!') && i + 1 < limit && char.IsWhiteSpace(text[i + 1]))
            {
                sentence = i + 1;
            }
            else if (space < 0 && char.IsWhiteSpace(c))
            {
                space = i + 1;
            }
        }

        if (line >= 0) return line;
        if (sentence >= 0) return sentence;
        if (space >= 0) return space;
        return limit;
    }

    /// <summary>
    /// The earliest line, else sentence, else word start in <c>text[from..end]</c>, so an overlap doesn't open mid-word.
    /// Only positions where text starts count: the gap inside a paragraph break would give no overlap at all.
    /// </summary>
    private static int FindOverlapStart(string text, int from, int end)
    {
        int sentence = -1, word = -1;

        for (int i = from; i < end; i++)
        {
            char previous = text[i - 1];
            if (char.IsWhiteSpace(text[i]) || !char.IsWhiteSpace(previous)) continue;
            if (previous == '\n') return i;

            if (sentence < 0 && i >= 2 && (text[i - 2] is '.' or '?' or '!')) sentence = i;
            if (word < 0) word = i;
        }

        return sentence >= 0 ? sentence : word >= 0 ? word : from;
    }

    private static int SkipWhitespace(string text, int index)
    {
        while (index < text.Length && char.IsWhiteSpace(text[index])) index++;
        return index;
    }

    private static int TrimEnd(string text, int start, int end)
    {
        while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
        return end;
    }
}