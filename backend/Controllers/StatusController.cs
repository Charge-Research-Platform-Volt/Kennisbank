using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
public class StatusController : AppControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Retrieves the status of the system",
        Description = "Retrieves the status of the system"
    )]
    [SwaggerResponse(200, "Server online")]
    public IActionResult Status()
    {
        return Ok("online");
    }
}
