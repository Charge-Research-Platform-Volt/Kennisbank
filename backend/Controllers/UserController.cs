using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Options;

namespace KnowledgeBank.Controllers;

/// <summary>
/// Controller for all sorts of functionality that 
/// might be of use during the Development stage.
/// </summary>
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly Serilog.ILogger logger;
    private readonly DatabaseContext database;
    private readonly UserManager<User> userManager;
    private readonly OwnerUserConfig ownerConfig;

    public UserController(DatabaseContext databaseContext, UserManager<User> userManager, IOptions<OwnerUserConfig> ownerConfig)
    {
        this.logger = Log.ForContext<UserController>();
        this.database = databaseContext;
        this.userManager = userManager;
        this.ownerConfig = ownerConfig.Value;
    }

    /// <summary>
    /// Gets the current user's full name.
    /// </summary>
    /// <returns>The user's full name.</returns>
    [HttpGet("current-user-name")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Get current user's name.",
        Description = "Returns the current user's name or an empty string if not authenticated"
    )]
    [SwaggerResponse(200, "The current user's name.")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetCurrentUserName()
    {
        try
        {
            // Check if user is authenticated
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Ok(new { name = "", isAuthenticated = false });

            // Get the user ID from the claims
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check if the user ID is null or empty
            if (string.IsNullOrEmpty(userId))
                return Ok(new { name = "", isAuthenticated = true });

            // Find the user by ID
            User? user = await userManager.FindByIdAsync(userId);

            // Check if the user exists
            if (user == null)
                return Ok(new { name = "", isAuthenticated = true });
            
            // Return the user's full name
            return Ok(new { name = user.FirstName + " " + user.LastName, isAuthenticated = true });
        }
        catch (Exception e)
        {
            logger.Error(e, "Error retrieving current user's name");
            return StatusCode(500, "Internal server error.");
        }
    }

    /// <summary>
    /// Gets the current user's first name.
    /// </summary>
    /// <returns>The user's first name.</returns>
    [HttpGet("current-user-first-name")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Get current user's first name.",
        Description = "Returns the current user's first name or an empty string if not authenticated"
    )]
    [SwaggerResponse(200, "The current user's first name.")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetCurrentUserFirstName()
    {
        try
        {
            // Check if user is authenticated
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Ok(new { firstName = "", isAuthenticated = false });

            // Get the user ID from the claims
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check if the user ID is null or empty
            if (string.IsNullOrEmpty(userId))
                return Ok(new { firstName = "", isAuthenticated = true });

            // Find the user by ID
            User? user = await userManager.FindByIdAsync(userId);

            // Check if the user exists
            if (user == null)
                return Ok(new { firstName = "", isAuthenticated = true });

            // Return the user's first name
            return Ok(new { firstName = user.FirstName, isAuthenticated = true });
        }
        catch (Exception e)
        {
            logger.Error(e, "Error retrieving current user's first name");
            return StatusCode(500, "Internal server error.");
        }
    }

    /// <summary>
    /// Gets the current user's last name.
    /// </summary>
    /// <returns>The user's last name</returns>
    [HttpGet("current-user-last-name")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Get current user's last name.",
        Description = "Returns the current user's name or an empty string if not authenticated"
    )]
    [SwaggerResponse(200, "The current user's last name.")]
    [SwaggerResponse(500, "Internal server error")]
    public async Task<IActionResult> GetCurrentUserLastName()
    {
        try
        {
            // Check if user is authenticated
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return Ok(new { lastName = "", isAuthenticated = false });

            // Get the user ID from the claims
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check if the user ID is null or empty
            if (string.IsNullOrEmpty(userId))
                return Ok(new { lastName = "", isAuthenticated = true });

            // Find the user by ID
            User? user = await userManager.FindByIdAsync(userId);

            // Check if the user exists
            if (user == null)
                return Ok(new { lastName = "", isAuthenticated = true });

            // Return the user's last name
            return Ok(new { lastName = user.LastName, isAuthenticated = true });
        }
        catch (Exception e)
        {
            logger.Error(e, "Error retrieving current user's last name");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpGet("all-users")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
        Summary = "Gets all users",
        Description = "Gets a list of all users."
    )]
    [SwaggerResponse(200, "Users are loaded succesfully")]
    [SwaggerResponse(500, "Server error")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            User[]? users = await database.AppUsers.OrderBy(u => u.Email).ToArrayAsync();

            if (users == null)
                return Ok(Array.Empty<UserResponse>());

            UserResponse[]? userResponses = new UserResponse[users.Length];
            for (int i = 0; i < users.Length; i++)
            {
                User user = users[i];
                IList<string> roles = await userManager.GetRolesAsync(user);
                userResponses[i] = new UserResponse(new Guid(user.Id), user.UserName, user.Email, user.EmailConfirmed, roles.FirstOrDefault() ?? "No Role");
            }

            return Ok(userResponses);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing all users.");
            return StatusCode(500, new ApiResponse(false, "Error listing all users."));
        }
    }

    [AllowAnonymous]
    [HttpGet("list-paged")]
    [SwaggerOperation(
        Summary = "Gets a page of users",
        Description = "Gets a page of users."
    )]
    [SwaggerResponse(200, "Users are loaded succesfully")]
    [SwaggerResponse(500, "Server error")]
    public async Task<IActionResult> GetUsersPaged(int pageIndex = 1, int pageSize = 100, string? searchQuery = null)
    {
        if (pageIndex < 1)
                return BadRequest(new ApiResponse(false, "Page index cannot be lower than 1."));

        if (pageSize < 1)
            return BadRequest(new ApiResponse(false, "Page size cannot be lower than 1."));
        
        try
        {
            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            // filter based on the search query
            IQueryable<User> filteredUsers;
            if (!string.IsNullOrEmpty(searchQuery))
                filteredUsers = database.AppUsers.Where(u => EF.Functions.ILike(u.Email ?? "", $"%{searchQuery}%"));
            else
                filteredUsers = database.AppUsers;

            // Get the users for the current page
            User[]? users = await filteredUsers.OrderBy(u => u.Email).Skip(skip).Take(pageSize).ToArrayAsync();

            // Calculate total amount of pages
            int totalUsers = await database.AppUsers.CountAsync();
            int pageCount = (int)Math.Ceiling((double)totalUsers / pageSize);

            // Create the response
            UserResponse[]? userResponses = new UserResponse[users.Length];
            for (int i = 0; i < users.Length; i++)
            {
                User user = users[i];
                IList<string> roles = await userManager.GetRolesAsync(user);
                userResponses[i] = new UserResponse(new Guid(user.Id), user.UserName, user.Email, user.EmailConfirmed, roles[0].ToString());
            }

            // Check if there are no users on this page
            if (users == null)
                return Ok(new UserPageResponse("No users on this page.", pageIndex, pageSize, pageCount, Array.Empty<UserResponse>()));

            return Ok(new UserPageResponse($"{users.Length} users found.", pageIndex, pageSize, pageCount, userResponses));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing users on page {PageIndex} of size {PageSize}.", pageIndex, pageSize);
            return StatusCode(500, new ApiResponse(false, "Error listing users."));
        }
    }

    [HttpPatch("update-mail")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
        Summary = "Updates the user email.",
        Description = "Updates the email of the user."
    )]
    [SwaggerResponse(200, "User email updated successfully.")]
    [SwaggerResponse(400, "User email already exists.")]
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(500, "Internal server error.")]
    [SwaggerResponse(403, "This action is forbidden")]
    public async Task<IActionResult> UpdateMail([FromBody] UpdateEmailDto dto)
    {
        try
        {
            User? user = await userManager.FindByIdAsync(dto.UserId);

            if (user == null)
                return NotFound("Invalid user ID.");

            if (user.Email == ownerConfig.Email)
                return StatusCode(403, "Email of owner account cannot be changed!");
            
            if (user.Email == dto.Email)
                return BadRequest($"User has already the email '{dto.Email}'.");

            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await database.Database.BeginTransactionAsync())
            {
                try
                {
                    string token = await userManager.GenerateChangeEmailTokenAsync(user, dto.Email);
                    IdentityResult emailResponse = await userManager.ChangeEmailAsync(user, dto.Email, token);

                    if (!emailResponse.Succeeded)
                        return BadRequest(emailResponse.Errors);

                    IdentityResult usernameResponse = await userManager.SetUserNameAsync(user, dto.Email);

                    if (!usernameResponse.Succeeded)
                        return BadRequest(usernameResponse.Errors);

                    await database.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.Information("User email changed successfully. User ID: {UserId}, New email: {Email}", user.Id, dto.Email);

                    return Ok(new { message = $"User email changed successfully." });
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    logger.Error(e, "Error changing the email for user {UserId}.", user.Id);
                    return StatusCode(500, "Internal server error.");
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Error changing the email.");
            return StatusCode(500, "Internal server error.");
        }
    }

    /// <summary>
    /// Updates the current user.
    /// This method updates the current user's information, including their first name, last name, and email.
    /// It requires the user to be authenticated.
    /// </summary>
    /// <param name="dto">The data transfer object containing the user's updated information.</param>
    /// <returns>An IActionResult with information about the success of the action.</returns>
    [HttpPut("update")]
    [SwaggerOperation(
        Summary = "Update the current user.",
        Description = "Updates the current user's information."
    )]
    [SwaggerResponse(200, "User updated successfully.")]
    [SwaggerResponse(400, "User email already exists.")]
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(500, "Internal server error.")]
    [SwaggerResponse(403, "This action is forbidden")]
    public async Task<IActionResult> Update([FromBody] UpdateUserDto dto)
    {
        try
        {
            logger.Information("Updating user information.");

            // Check if user is authenticated
            if (User.Identity == null || !User.Identity.IsAuthenticated)
                return BadRequest("User not authenticated.");
            
            // Get the user ID from the claims
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check if the user ID is null or empty
            if (string.IsNullOrEmpty(userId))
                return BadRequest("User not found.");

            // Find the user by ID
            User user = (await userManager.FindByIdAsync(userId))!;

            // Check if the user exists
            if (user == null)
                return NotFound("User not found.");

            if (user.Email == ownerConfig.Email)
                return StatusCode(403, "The owner account cannot be altered.");

            // Use a transaction to ensure that all changes are saved or none
            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await database.Database.BeginTransactionAsync())
            {
                try
                {
                    // Update the first and last name
                    user.FirstName = dto.FirstName;
                    user.LastName = dto.LastName;
                    IdentityResult updateResponse = await userManager.UpdateAsync(user);

                    // Check if the update was successful
                    if (!updateResponse.Succeeded)
                        return Ok(new ApiResponse(false, "Failed to update the data."));

                    // Update the email
                    string token = await userManager.GenerateChangeEmailTokenAsync(user, dto.Email);
                    IdentityResult emailResponse = await userManager.ChangeEmailAsync(user, dto.Email, token);

                    // Check if the email update was successful
                    if (!emailResponse.Succeeded)
                        return Ok(new ApiResponse(false, "User email already exists."));

                    // Update the username
                    IdentityResult usernameResponse = await userManager.SetUserNameAsync(user, dto.Email);

                    // Check if the username update was successful
                    if (!usernameResponse.Succeeded)
                        return Ok(new ApiResponse(false, "User email already exists."));

                    // Save the changes to the database
                    await database.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // Log the successful update
                    logger.Information("User updated successfully.");

                    // Return a success response
                    return Ok(new ApiResponse(true, "User updated successfully."));
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    logger.Error(e, "Error updating the user.", user.Id);
                    return StatusCode(500, "Internal server error.");
                }
            }
        }
        catch (Exception e)
        {
            logger.Error(e, "Error updating the user.");
            return StatusCode(500, "Internal server error.");
        }
    }

    [HttpDelete("delete")]
    [SwaggerOperation(
        Summary = "Delete a user.",
        Description = "Deletes a user by ID."
    )]
    [SwaggerResponse(200, "User deleted successfully.")]
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(403, "This action is forbidden")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<IActionResult> Delete(string? userId)
    {
        try
        {
            string? currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (currentUserId == null)
                return BadRequest("Current user ID not found in claims.");

            User? currentUser = await userManager.FindByIdAsync(currentUserId);

            if (currentUser == null)
                return NotFound("Current user not found.");
            
            if (string.IsNullOrEmpty(userId))
            {
                userId = currentUser.Id;
            }

            // Prevent deleting anything other than the current user
            if (currentUser.Id != userId)
            {
                IList<string> roles = await userManager.GetRolesAsync(currentUser);
                if (!roles.Contains("admin"))
                    return BadRequest("Only admins can delete users.");
            }

            User? user = await userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound("Invalid user ID.");
                
            if (user.Email == ownerConfig.Email)
                return StatusCode(403, "Owner user cannot be deleted");

            IdentityResult response = await userManager.DeleteAsync(user);

            if (!response.Succeeded)
                return BadRequest(response.Errors);

            return Ok(new { message = $"User deleted succesfully." });
        }
        catch (Exception e)
        {
            logger.Error(e, "Error deleting the user.");
            return StatusCode(500, "Internal server error.");
        }
    }
    
}


// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// © Copyright Utrecht University (Department of Information and Computing Sciences)


