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


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


