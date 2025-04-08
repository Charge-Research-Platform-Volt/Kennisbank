using KnowledgeBank.Data;

namespace KnowledgeBank.Models;

public class FilterDto
{
    public string[]? TagFilters { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public IQueryable<FileItem> ToQueryBuilder(DatabaseContext database)
    {
        var queryBuilder = database.Files.AsQueryable();
            
        if(this.TagFilters != null && this.TagFilters.Length > 0)
        {
            var tagFilterQuery = database.FileTagLinks
                .Where(dt => this.TagFilters.Contains(dt.TagId.ToString())) // Filter by tags
                .GroupBy(dt => dt.DocId)
                .Where(g => g.Count() == this.TagFilters.Length) // Ensure that files have all tags
                .Select(g => g.Key);  // get the file IDs

            queryBuilder = queryBuilder.Where(f => tagFilterQuery.Contains(f.Id));
        }

        if(this.StartDate != null)
        {
            queryBuilder = queryBuilder.Where(f => f.CreatedAt >= this.StartDate);
        }

        if(this.EndDate != null)
        {
            queryBuilder = queryBuilder.Where(f => f.CreatedAt <= this.EndDate);
        }

        return queryBuilder;
    }
}