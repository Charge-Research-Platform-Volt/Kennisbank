using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class StatusController : Controller
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
}
