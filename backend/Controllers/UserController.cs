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

namespace KnowledgeBank.Controllers;

/// <summary>
/// This controller manages user-related operations such as retrieving users, updating user emails, and deleting users.
/// 
/// Author: Rens van Moorsel, Jelle van het Schut
/// </summary>
[Authorize(Policy = "RequireAdminRole")]
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly Serilog.ILogger logger;
    private readonly DatabaseContext database;
    private readonly UserManager<User> userManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserController"/> class.
    /// 
    /// Author: Rens van Moorsel
    /// </summary>
    /// <param name="databaseContext">The database context for accessing user data.</param>
    /// <param name="userManager">The user manager for managing user-related operations.</param>
    public UserController(DatabaseContext databaseContext, UserManager<User> userManager)
    {
        this.logger = Log.ForContext<UserController>();
        this.database = databaseContext;
        this.userManager = userManager;
    }

    /// <summary>
    /// Gets all users in the system.
    /// 
    /// Author: Jelle van het Schut
    /// </summary>
    /// <returns>Returns a list of all users in the system.</returns>
    [HttpGet("all-users")]
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
            return StatusCode(500, new StorageResponse("Error listing all users."));
        }
    }

    /// <summary>
    /// Gets a paginated list of users.
    /// 
    /// Author: Rens van Moorsel
    /// </summary>
    /// <param name="pageIndex">The index of the page to retrieve (1-based).</param>
    /// <param name="pageSize">The number of users to retrieve per page.</param>
    /// <param name="searchQuery">An optional search query to filter users by email.</param>
    /// <returns>Returns a paginated list of users.</returns>
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
                return BadRequest(new StorageResponse("Page index cannot be lower than 1."));

        if (pageSize < 1)
            return BadRequest(new StorageResponse("Page size cannot be lower than 1."));
        
        try
        {
            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            // filter based on the search query
            IQueryable<User> filteredUsers;
            if(!string.IsNullOrEmpty(searchQuery))
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

            return Ok(new UserPageResponse($"{users.Length} users found.", pageIndex, pageSize,  pageCount, userResponses));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing users on page {PageIndex} of size {PageSize}.", pageIndex, pageSize);
            return StatusCode(500, new StorageResponse("Error listing users."));
        }
    }    
    
    /// <summary>
    /// Updates the email of a user.
    /// 
    /// Author: Rens van Moorsel
    /// </summary>
    /// <param name="dto">The data transfer object containing the user ID and new email.</param>
    /// <returns>Returns an IActionResult indicating the result of the email update operation.</returns>
    [HttpPatch("update-mail")]
    [SwaggerOperation(
        Summary = "Updates the user email.",
        Description = "Updates the email of the user."
    )]
    [SwaggerResponse(200, "User email updated successfully.")]
    [SwaggerResponse(400, "User email already exists.")]
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<IActionResult> UpdateMail([FromBody] UpdateEmailDto dto)
    {
        try
        {
            User? user = await userManager.FindByIdAsync(dto.UserId);

            if (user == null)
                return NotFound("Invalid user ID.");

            if (user.Email == dto.Email)
                return BadRequest($"User has already the email '{dto.Email}'.");
                
            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await database.Database.BeginTransactionAsync())
            {
                try
                {
                    string token = await userManager.GenerateChangeEmailTokenAsync(user, dto.Email);
                    IdentityResult emailResponse = await userManager.ChangeEmailAsync(user, dto.Email, token);

                    if(!emailResponse.Succeeded)
                        return BadRequest(emailResponse.Errors);

                    IdentityResult usernameResponse = await userManager.SetUserNameAsync(user, dto.Email);

                    if (!usernameResponse.Succeeded)
                        return BadRequest(usernameResponse.Errors);

                    await database.SaveChangesAsync();
                    await transaction.CommitAsync();
                    logger.Information("User email changed successfully. User ID: {UserId}, New email: {Email}", user.Id, dto.Email);
                    
                    return Ok(new { message = $"User email changed successfully."});
                }   
                 catch(Exception e)
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
    /// Deletes a user by ID.
    /// If no user ID is provided, the current user will be deleted.
    /// 
    /// Author: Rens van Moorsel
    /// </summary>
    /// <param name="userId">Optional user ID to delete. If not provided, the current user's ID will be used.</param>
    /// <returns>Returns an IActionResult indicating the result of the delete operation.</returns>
    [HttpDelete("delete")]
    [SwaggerOperation(
        Summary = "Delete a user.",
        Description = "Deletes a user by ID."
    )]
    [SwaggerResponse(200, "User deleted successfully.")]
    [SwaggerResponse(404, "User not found.")]
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

            // Prevent deleting the current user
            if (currentUser.Id != userId)
            {
                IList<string> roles = await userManager.GetRolesAsync(currentUser);
                if (!roles.Contains("admin"))
                    return BadRequest("Only admins can delete users.");
            }

            User? user = await userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound("Invalid user ID.");

            IdentityResult response = await userManager.DeleteAsync(user);

            if(!response.Succeeded)
                return BadRequest(response.Errors);

            return Ok(new { message = $"User deleted succesfully."});
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


