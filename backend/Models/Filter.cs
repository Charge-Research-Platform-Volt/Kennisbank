using KnowledgeBank.Data;

namespace KnowledgeBank.Models;

public class FilterDto
{
    public string[]? TagFilters { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    public IQueryable<Resource> ToQueryBuilder(DatabaseContext database)
    {
        IQueryable<Resource> queryBuilder = database.Resources.AsQueryable();
            
        if(this.TagFilters != null && this.TagFilters.Length > 0)
        {
            IQueryable<Guid> tagFilterQuery = database.ResourceTagRelations
                .Where(dt => this.TagFilters.Contains(dt.TagId.ToString())) // Filter by tags
                .GroupBy(dt => dt.ResourceId)
                .Where(g => g.Count() == this.TagFilters.Length) // Ensure that resources have all tags
                .Select(g => g.Key);  // get the resources IDs

            queryBuilder = queryBuilder.Where(f => tagFilterQuery.Contains(f.Id));
        }

        if(this.StartDate != null)
        {
            queryBuilder = queryBuilder.Where(f => f.PublicationDate >= this.StartDate);
        }

        if(this.EndDate != null)
        {
            queryBuilder = queryBuilder.Where(f => f.PublicationDate <= this.EndDate);
        }

        return queryBuilder;
    }
}

// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


