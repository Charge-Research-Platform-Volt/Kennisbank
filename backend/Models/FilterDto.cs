namespace KnowledgeBank.Models;

public class FilterDto
{
    public string[]? TagFilters { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}