using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Models;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using KnowledgeBank.Data;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager, IOptions<OwnerUserConfig> ownerConfig) : AppControllerBase
    {
        [HttpGet("current")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Get current user's role.",
            Description = "Returns the current user's role or an empty string if not authenticated"
        )]
        [SwaggerResponse(200, "The current user's role.")]
        public async Task<IActionResult> GetCurrentUserRole()
        {
            // Check if user is authenticated
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Ok(new { role = "", isAuthenticated = false });

            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Authenticated user has no NameIdentifier claim.");

            User? user = await userManager.FindByIdAsync(userId);

            if (user == null)
                return Ok(new { role = "", isAuthenticated = true });

            IList<string> roles = await userManager.GetRolesAsync(user);
            string role = roles.Count > 0 ? roles[0] : "";

            return Ok(new { role, isAuthenticated = true });
        }

        [HttpGet]
        [SwaggerOperation(
            Summary = "Lists all roles available.",
            Description = "Lists all roles that exist in the application."
        )]
        [SwaggerResponse(200, "A list of the roles")]
        public async Task<IActionResult> GetRoles()
        {
            IdentityRole[] roles = await roleManager.Roles.ToArrayAsync();
            return Ok(roles);
        }

        [HttpPut("assign")]
        [SwaggerOperation(
            Summary = "Assigns a role to a user.",
            Description = "Assigns a role to the given user. Strips all other roles, since users only can have one role."
        )]
        [SwaggerResponse(200, "Role was assigned succesfully.")]
        [SwaggerResponse(404, "User or role not found.")]
        [SwaggerResponse(400, "Cannot assign role.")]
        [SwaggerResponse(403, "This action is forbidden")]
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignDto dto)
        {
            User? user = await userManager.FindByIdAsync(dto.UserId);

            if (user == null)
                return Problem("User not found", statusCode: 404);

            if (user.Email == ownerConfig.Value.Email)
                return Problem("Role of the owner account cannot be changed.", statusCode: 403);           

            if (!await roleManager.RoleExistsAsync(dto.RoleName))
                return Problem("Role does not exist", statusCode: 404);

            if (await userManager.IsInRoleAsync(user, dto.RoleName))
                return Problem("User already has this role", statusCode: 400);

            IList<string> currentRoles = await userManager.GetRolesAsync(user);
            IdentityResult removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!removeResult.Succeeded)
                throw new InvalidOperationException($"Failed to remove roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");

            IdentityResult result = await userManager.AddToRoleAsync(user, dto.RoleName);

            if (!result.Succeeded)
                throw new InvalidOperationException($"Failed to assign role: {string.Join(", ", result.Errors.Select(e => e.Description))}");

            return Ok();
        }

        [HttpGet("user/{id}")]
        [SwaggerOperation(
            Summary = "Retrieve role of user",
            Description = "Retrieves the role of the given user ID."
        )]
        [SwaggerResponse(200, "The role of the user")]
        [SwaggerResponse(404, "User not found")]
        public async Task<IActionResult> RetrieveUserRole(string id)
        {
            User? user = await userManager.FindByIdAsync(id);

            if (user == null)
                return Problem("User not found.", statusCode: 404);

            IList<string> roles = await userManager.GetRolesAsync(user);
            return Ok(roles.Count > 0 ? roles[0] : "");
        }

        [HttpGet("members")]
        [SwaggerOperation(
            Summary = "Retrieves users in the given role",
            Description = "Retrieves all users in the given role"
        )]
        [SwaggerResponse(200, "All users in the role")]
        [SwaggerResponse(404, "Role not found")]
        public async Task<IActionResult> RetrieveUsersInRole([FromQuery] string roleName)
        {
            if (string.IsNullOrEmpty(roleName) || !await roleManager.RoleExistsAsync(roleName))
                return Problem("Role does not exist.", statusCode: 404);

            IList<User> users = await userManager.GetUsersInRoleAsync(roleName);
            return Ok(users);
        }
    }
}
