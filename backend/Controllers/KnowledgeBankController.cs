using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class KnowledgeBankController : ControllerBase
    {
        private static readonly Serilog.ILogger _logger = Log.ForContext(typeof(KnowledgeBankController));

        [HttpGet]
        [Route("members")]
        [SwaggerOperation(
            Summary = "Get all team members",
            Description = "Returns a list of all team members")]
        public IResult Members()
        {
            _logger.Debug("Getting all team members");

            var names = new[] { "Elia", "Yorick", "Abel", "Aiden", "Jason", "Jelle", "Rens", "Justin" };

            var members = names.Select(name => new TeamMember(name)).ToArray();
            return Results.Json(members);
        }
    }

    record TeamMember(string Name) { }
}