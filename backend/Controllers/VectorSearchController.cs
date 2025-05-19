using KnowledgeBank.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[ApiController]
// [Authorize]
[Route("[controller]")]
[Produces("application/json")]
public class VectorSearchController : ControllerBase
{
    private readonly Serilog.ILogger _logger;
    private readonly IRAGSystem _ragSystem;

    public VectorSearchController(IRAGSystem ragSystem)
    {
        _logger = Log.ForContext<VectorSearchController>();
        _ragSystem = ragSystem;
    }


    [HttpPost("vector-search")]
    public IActionResult VectorSearch(string query)
    {
        _logger.Information("Vector search initiated with query: {Query}", query);
        return Ok(new
        {
            Message = "Vector search initiated",
            Query = query
        });
    }
}