using KnowledgeBank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly Serilog.ILogger _logger;
        private readonly SignInManager<IdentityUser> _signInManager;

        public AuthController(SignInManager<IdentityUser> signInManager)
        {
            _signInManager = signInManager;
            _logger = Log.ForContext<AuthController>();
        }

        [HttpPost]
        [Route("logout")]
        [SwaggerOperation(Summary = "Logs out the current user", Description = "Logs out the current user")]
        [SwaggerResponse(200, "The user has been logged out")]
        [SwaggerResponse(401, "The user is not authenticated")]
        public async Task<IActionResult> Logout([FromBody] object empty)
        {
            if (empty != null)
            {
                await _signInManager.SignOutAsync();
                return Ok();
            }

            return Unauthorized();
        }
    }
}