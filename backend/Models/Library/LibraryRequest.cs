namespace KnowledgeBank.Models;

public class LibraryRequest
{
    public required int Page { get; set; }
    public required int PageSize { get; set; }
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDirection { get; set; }
    public LibraryFilterOptions? FilterOptions { get; set; }
}

public class LibraryContentSearchRequest
{
    public required string Search { get; set; }
    public int PageSize { get; set; } = 20;
    public Guid[]? ExcludeIds { get; set; }
}

public class LibraryFilterOptions
{
    public string[]? TypeFilter { get; set; }
    public string? PubdateMin { get; set; }
    public string? PubdateMax { get; set; }
    public string[]? TagFilter { get; set; }
    public string? TagFilterMode { get; set; } = "any";
    public string[]? RegionFilter { get; set; }
    public string? RegionFilterMode { get; set; } = "any";
    public string[]? ResourceTypeFilter { get; set; }
    public string[]? JournalFilter { get; set; }
}

public class LibraryResult
{
    public LibraryItem[] Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int DurationInMs { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? SearchTerm { get; set; }
    public Dictionary<string, int> TypeCounts { get; set; } = [];
}

public class LibraryItemWithChunks
{
    public required LibraryItem Item { get; set; }
    public required List<string> MatchedChunks { get; set; }
    public required float Score { get; set; }
}