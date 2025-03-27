using System.Security.Claims;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using KnowledgeBank.Utils;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly Serilog.ILogger _logger;
        private readonly SignInManager<User> _signInManager;
        private readonly DatabaseContext _context;

        public AuthController(SignInManager<User> signInManager, DatabaseContext context)
        {
            _signInManager = signInManager;
            _logger = Log.ForContext<AuthController>();
            _context = context;
        }

        [HttpPost]
        [Authorize]
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

        [HttpGet]
        [Authorize]
        [Route("ping")]
        [SwaggerOperation(Summary = "Gets the current user's email", Description = "Returns the email of the currently authenticated user")]
        [SwaggerResponse(200, "The user's email was returned", typeof(object))]
        [SwaggerResponse(401, "The user is not authenticated")]
        public IActionResult Ping()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            return Ok(new { Email = email });
        }

        [HttpPost("invite")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Invite a new user", Description = "Invite a new user to the system")]
        [SwaggerResponse(200, "The user has been invited")]
        [SwaggerResponse(401, "The user is not authenticated")]
        public async Task<IActionResult> Invite([FromBody] string email)
        {
            // generate a token
            Guid token = Guid.NewGuid();
            
            // save the invitation
            _context.Invitations.Add(new Invitation
            {
                Id = Guid.NewGuid(),
                Email = ShaUtils.Sha256(email),
                Token = ShaUtils.Sha256(token.ToString()),
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // send the email
            try
            {
                MailUtils.SendMail(email, "Invitation", $"You have been invited to join KnowledgeBank. Create an account: http://localhost:3000/signup?token={token}");
            }
            catch (Exception e)
            {
                _logger.Error(e, "Failed to send email");
                return BadRequest(new {message = "Failed to send email"});
            }
            
            return Ok();
        }

        [HttpPost("signup")]
        [SwaggerOperation(Summary = "Register a new user", Description = "Register a new user")]
        [SwaggerResponse(200, "The user has been registered")]
        [SwaggerResponse(400, "Bad request")]
        public async Task<IActionResult> Register([FromBody] SignUpDto signUpDto)
        {
            // check if there is a recent invitation for the email and token
            var invitation = _context.Invitations.FirstOrDefault(i => i.Email == ShaUtils.Sha256(signUpDto.Email) 
                                                                    && i.Token == ShaUtils.Sha256(signUpDto.Token) 
                                                                    && i.CreatedAt > DateTime.UtcNow.AddHours(-168));
            if (invitation == null)
            {
                return BadRequest(new {message = "Invalid invitation"});
            }

            // Remove user
            _context.Remove(invitation);
            await _context.SaveChangesAsync();

            // create the user
            var user = new User
            {
                Email = signUpDto.Email,
                UserName = signUpDto.Email
            };

            // save the user
            var result = await _signInManager.UserManager.CreateAsync(user, signUpDto.Password);
            if (result.Succeeded)
            {
                return Ok();
            }

            return BadRequest(new {message = string.Join(" ", result.Errors.Select(e => e.Description))});
        }

    }
}
