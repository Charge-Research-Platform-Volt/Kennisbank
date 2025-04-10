using KnowledgeBank.Data;

namespace KnowledgeBank.Models;

public class FilterDto
{
    public string[]? TagFilters { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public IQueryable<Resource> ToQueryBuilder(DatabaseContext database)
    {
        var queryBuilder = database.Resources.AsQueryable();
            
        if(this.TagFilters != null && this.TagFilters.Length > 0)
        {
            var tagFilterQuery = database.ResourceTagRelations
                .Where(dt => this.TagFilters.Contains(dt.TagId.ToString())) // Filter by tags
                .GroupBy(dt => dt.ResourceId)
                .Where(g => g.Count() == this.TagFilters.Length) // Ensure that resources have all tags
                .Select(g => g.Key);  // get the resources IDs

            queryBuilder = queryBuilder.Where(f => tagFilterQuery.Contains(f.Id));
        }

        if(this.StartDate != null)
        {
            queryBuilder = queryBuilder.Where(f => f.CreationDate >= this.StartDate);
        }

        if(this.EndDate != null)
        {
            queryBuilder = queryBuilder.Where(f => f.CreationDate <= this.EndDate);
        }

        return queryBuilder;
    }
}