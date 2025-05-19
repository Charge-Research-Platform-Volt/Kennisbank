using KnowledgeBank.Models;

namespace KnowledgeBank.Responses;

public struct TagPageResponse
{
    public Tag[] Tags { get; }
    public int? PageSize { get; } = null;
    public int? PageIndex { get; } = null;
    public int? PageCount { get; } = null;
    public TagPageResponse( Tag[] tags, int? pageIndex = null, int? pageSize = null, int? pageCount = null)
    {
        this.PageIndex = pageIndex;
        this.PageSize = pageSize;
        this.Tags = tags;
        this.PageCount = pageCount;
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)