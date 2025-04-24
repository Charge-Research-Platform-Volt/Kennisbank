using KnowledgeBank.Models;

namespace KnowledgeBank.Responses;

public struct TagPageResponse
{
    public string Message { get; }
    public int PageIndex { get; }
    public int PageSize { get; }
    public int PageCount { get; }
    public Tag[] Tags { get; }

    public TagPageResponse(string message, int pageIndex, int pageSize, int pageCount, Tag[] tags)
    {
        this.Message = message;
        this.PageIndex = pageIndex;
        this.PageSize = pageSize;
        this.Tags = tags;
        this.PageCount = pageCount;
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)