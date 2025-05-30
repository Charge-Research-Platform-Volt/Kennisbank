using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace KnowledgeBank.Controllers;

/// <summary>
/// Controller for all sorts of functionality that 
/// might be of use during the Development stage.
/// </summary>
[Authorize(Policy = "RequireAdminRole")]
[ApiController]
[Route("[controller]")]
public class UserController : ControllerBase
{
    private readonly Serilog.ILogger logger;
    private readonly DatabaseContext database;
    private readonly UserManager<User> userManager;

    public UserController(DatabaseContext databaseContext, UserManager<User> userManager)
    {
        this.logger = Log.ForContext<UserController>();
        this.database = databaseContext;
        this.userManager = userManager;
    }

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

    [HttpDelete("delete")]
    [SwaggerOperation(
        Summary = "Delete a user.",
        Description = "Deletes a user by ID."
    )]
    [SwaggerResponse(200, "User deleted successfully.")]
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<IActionResult> Delete(string userId)
    {
        try
        {
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


