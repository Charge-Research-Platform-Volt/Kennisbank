using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Models;

namespace KnowledgeBank.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public AuthController(UserManager<User> userManager, SignInManager<User> signInManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
        }

        [HttpGet("status")]
        public async Task<IActionResult> GetAuthStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { isAuthenticated = false });
            }

            return Ok(new
            {
                isAuthenticated = true,
                user = new
                {
                    id = user.Id,
                    email = user.Email,
                    roles = await _userManager.GetRolesAsync(user)
                }
            });
        }
    }
}
