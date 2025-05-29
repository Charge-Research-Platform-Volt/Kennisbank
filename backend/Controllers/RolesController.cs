using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using KnowledgeBank.Models;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// This controller manages user roles within the KnowledgeBank application.
    /// 
    /// Author: Abel Dietrich, Rens van Moorsel
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Authorize(Policy = "RequireAdminRole")]
    public class RolesController : ControllerBase
    {
        private readonly RoleManager<IdentityRole> roleManager;
        private readonly UserManager<User> userManager;
        private readonly Serilog.ILogger logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RolesController"/> class.
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <param name="roleManager">The role manager for managing roles.</param>
        /// <param name="userManager">The user manager for managing users.</param>
        public RolesController(RoleManager<IdentityRole> roleManager, UserManager<User> userManager)
        {
            this.roleManager = roleManager;
            this.userManager = userManager;
            this.logger = Log.ForContext<RolesController>();
        }

        /// <summary>
        /// Gets the current user's role.
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <returns>The current role of the user.</returns>
        [HttpGet("current")]
        [AllowAnonymous]
        [SwaggerOperation(
            Summary = "Get current user's role.",
            Description = "Returns the current user's role or an empty string if not authenticated"
        )]
        [SwaggerResponse(200, "The current user's role.")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> GetCurrentUserRole()
        {
            try
            {
                // Check if user is authenticated
                if (User.Identity == null || !User.Identity.IsAuthenticated)
                    return Ok(new { role = "", isAuthenticated = false });

                string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId))
                    return Ok(new { role = "", isAuthenticated = true });

                User? user = await userManager.FindByIdAsync(userId);

                if (user == null)
                    return Ok(new { role = "", isAuthenticated = true });

                IList<string> roles = await userManager.GetRolesAsync(user);
                string role = roles.Count > 0 ? roles[0] : "";

                return Ok(new { role, isAuthenticated = true });
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving current user's role");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Lists all roles available in the application.
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Lists all roles available.",
            Description = "Lists all roles that exist in the application."
        )]
        [SwaggerResponse(200, "A list of the roles")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> GetRoles()
        {
            try
            {
                IdentityRole[] roles = await roleManager.Roles.ToArrayAsync();
                return Ok(roles);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving roles.");
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Assigns a role to a user.
        /// 
        /// Author: Abel Dietrich, Rens van Moorsel
        /// </summary>
        /// <param name="dto">The role assignment data transfer object containing the user ID and role name.</param>
        /// <returns>An IActionResult indicating the result of the role assignment operation.</returns>
        [HttpPatch("assign")]
        [SwaggerOperation(
            Summary = "Assigns a role to a user.",
            Description = "Assigns a role to the given user. Strips all other roles, since users only can have one role."
        )]
        [SwaggerResponse(200, "Role was assigned succesfully.")]
        [SwaggerResponse(404, "User or role not found.")]
        [SwaggerResponse(400, "Cannot assign role.")]
        [SwaggerResponse(500, "Internal server error.")]
        public async Task<IActionResult> AssignRole([FromBody] RoleAssignDto dto)
        {
            try
            {
                User? user = await userManager.FindByIdAsync(dto.UserId);

                if (user == null)
                    return NotFound(new {message = "Invalid user ID."});

                if (!await roleManager.RoleExistsAsync(dto.RoleName))
                    return NotFound(new { message = "Invalid role name."});

                if (await userManager.IsInRoleAsync(user, dto.RoleName))
                    return BadRequest(new {message = $"User is already has the role '{dto.RoleName}'"});

                IList<string> currentRoles = await userManager.GetRolesAsync(user);
                IdentityResult removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);

                if (!removeResult.Succeeded)
                    return StatusCode(500, "Error removing current roles: " + removeResult.Errors);

                IdentityResult result = await userManager.AddToRoleAsync(user, dto.RoleName);

                if (result.Succeeded)
                    return Ok(new { message = $"User '{user.UserName}' added to role '{dto.RoleName}' successfully." });

                return BadRequest(result.Errors);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error assigning role {RoleName} to user with ID {UserID}", dto.RoleName, dto.UserId);
                return StatusCode(500, new { message = "Internal server error." });
            }
        }

        /// <summary>
        /// Retrieves the role of a user by their ID.
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <param name="id">The ID of the user whose role is to be retrieved.</param>
        /// <returns>An IActionResult containing the role of the user or an error message.</returns>
        [HttpGet("user/{id}")]
        [SwaggerOperation(
            Summary = "Retrieve role of user",
            Description = "Retrieves the role of the given user ID."
        )]
        [SwaggerResponse(200, "The role of the user")]
        [SwaggerResponse(404, "User not found")]
        [SwaggerResponse(500, "Internal server error")]
        public async Task<IActionResult> RetrieveUserRole(string id)
        {
            try
            {
                User? user = await userManager.FindByIdAsync(id);

                if (user == null)
                    return NotFound("User not found.");

                IList<string> roles = await userManager.GetRolesAsync(user);

                if (roles.Count == 0)
                    return Ok("No role assigned.");

                return Ok(roles[0]);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving role of user with ID {id}", id);
                return StatusCode(500, "Internal server error.");
            }
        }

        /// <summary>
        /// Retrieves all users in a specific role.
        /// 
        /// Author: Abel Dietrich
        /// </summary>
        /// <param name="roleName">The name of the role for which to retrieve users.</param>
        /// <returns>An IActionResult containing a list of users in the specified role or an error message.</returns>
        [HttpGet("{roleName}")]
        [SwaggerOperation(
            Summary = "Retrieves users in the given role.",
            Description = "Retrieves all users in the given role."
        )]
        [SwaggerResponse(200, "All users in the role.")]
        [SwaggerResponse(404, "Role not found.")]
        [SwaggerResponse(500, "Internal server error.")]
        public async Task<IActionResult> RetrieveUsersInRole(string roleName)
        {
            try
            {
                if (string.IsNullOrEmpty(roleName) || !await roleManager.RoleExistsAsync(roleName))
                    return NotFound("Role does not exist.");

                IList<User> users = await userManager.GetUsersInRoleAsync(roleName);

                return Ok(users);
            }
            catch (Exception e)
            {
                logger.Error(e, "Error retrieving users in role {RoleName}", roleName);
                return StatusCode(500, "Internal server error.");
            }
        }
    }
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


