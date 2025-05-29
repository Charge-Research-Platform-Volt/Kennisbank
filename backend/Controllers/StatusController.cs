using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// This controller provides an endpoint to check the status of the system.
    /// It returns a simple "online" message to indicate that the server is running.
    /// 
    /// Author: Abel Dietrich
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    public class StatusController : Controller
    {
        /// <summary>
        /// Retrieves the status of the system.
        /// This endpoint is used to check if the server is online and responsive.
        /// </summary>
        /// <returns>An IActionResult with a status message indicating the server status.</returns>
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


