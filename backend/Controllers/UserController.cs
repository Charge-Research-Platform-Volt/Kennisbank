using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using KnowledgeBank.Utils;
using Microsoft.Extensions.Options;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services;
using Microsoft.AspNetCore.Hosting;

namespace KnowledgeBank.Controllers;

/// <summary>
/// Controller for all sorts of functionality that 
/// might be of use during the Development stage.
/// </summary>
[ApiController]
[Route("[controller]")]
public class UserController(IDbContextFactory<DatabaseContext> dbFactory, IStorageService storageService, UserManager<User> userManager, IOptions<OwnerUserConfig> ownerConfig, EnvironmentConfig environmentConfig, VPNService vpnService, IWebHostEnvironment env) : ControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<UserController>();
    private readonly OwnerUserConfig ownerConfig = ownerConfig.Value;
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    /// <summary>
    /// Gets the current user's account info.
    /// </summary>
    /// <returns>The user's account info.</returns>
    [HttpGet("current/account")]
    [Authorize]
    [SwaggerOperation(
        Summary = "Get the current user's account info.",
        Description = "Gets the current user's account info."
    )]
    [SwaggerResponse(200, "Account info found.")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<ObjectResult> GetCurrentAccount()
    {
        try
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("User authenticated yet not found, probably a concurrency fault");
            User user = await userManager.FindByIdAsync(userId) ?? throw new Exception("User authenticated yet not found, probably a concurrency fault");

            IList<string> roles = await userManager.GetRolesAsync(user);

            return Ok(new ApiResponse(true, "Account info found.", new {
                Id = new Guid(user.Id),
                user.FirstName,
                user.LastName,
                user.Email,
                CustomAvatarVersion = user.HasCustom ? user.CustomAvatarVersion : (int?)null,
                user.EmailConfirmed,
                Role = roles.FirstOrDefault() ?? "No Role"
            }));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error getting the current user's account info.");
            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
    }

    [HttpGet("avatar/{userId}")]
    [SwaggerOperation(
        Summary = "Get the current user's profile avatar.",
        Description = "Gets the current user's profile avatar."
    )]
    [SwaggerResponse(200, "Profile avatar returned.")]
    [SwaggerResponse(404, "No avatar found for user.")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<IActionResult> GetCurrentAvatar(string userId)
    {
        try
        {
            User? user = await userManager.FindByIdAsync(userId);
            if (user == null || !user.HasCustom)
                return NotFound();

            ObjectDownloadResponse response = await storageService.DownloadObjectAsync(bucketName, userId);

            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");
            return File(response.Stream, response.ContentType!);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error getting the current user's avatar.");
            return StatusCode(500, new { message = "Internal server error." });
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
            await using var database = await dbFactory.CreateDbContextAsync();
        
            User[]? users = await database.Users.OrderBy(u => u.Email).ToArrayAsync();

            if (users == null)
                return Ok(new ApiResponse(true, "No users found.", Array.Empty<object>()));

            var userResponses = new List<object>();
            foreach (var user in users)
            {
                IList<string> roles = await userManager.GetRolesAsync(user);
                userResponses.Add(new {
                    Id = new Guid(user.Id),
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    CustomAvatarVersion = user.HasCustom ? user.CustomAvatarVersion : (int?)null,
                    user.EmailConfirmed,
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            return Ok(new ApiResponse(true, $"{userResponses.Count} users found.", userResponses));
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
    public async Task<IActionResult> GetUsersPaged(int pageIndex = 1, int pageSize = 100, string? searchQuery = null, string? excludeId = null)
    {
        if (pageIndex < 1)
                return BadRequest(new ApiResponse(false, "Page index cannot be lower than 1."));

        if (pageSize < 1)
            return BadRequest(new ApiResponse(false, "Page size cannot be lower than 1."));

        try
        {
            await using var database = await dbFactory.CreateDbContextAsync();

            // Calculate how many records we need to skip
            int skip = (pageIndex - 1) * pageSize;

            // filter based on the search query
            IQueryable<User> filteredUsers = database.Users;
            if (!string.IsNullOrEmpty(searchQuery))
                filteredUsers = filteredUsers.Where(u => EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchQuery}%"));
            if (!string.IsNullOrEmpty(excludeId))
                filteredUsers = filteredUsers.Where(u => u.Id != excludeId);

            // Get the users for the current page
            User[]? users = await filteredUsers.OrderBy(u => u.Email).Skip(skip).Take(pageSize).ToArrayAsync();

            // Calculate total amount of pages
            int totalUsers = await database.Users.CountAsync();
            int pageCount = (int)Math.Ceiling((double)totalUsers / pageSize);

            // Create the response
            var userResponses = new List<object>();
            foreach (var user in users)
            {
                IList<string> roles = await userManager.GetRolesAsync(user);
                userResponses.Add(new {
                    Id = new Guid(user.Id),
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    CustomAvatarVersion = user.HasCustom ? user.CustomAvatarVersion : (int?)null,
                    user.EmailConfirmed,
                    Role = roles.FirstOrDefault() ?? "No Role"
                });
            }

            return Ok(new ApiResponse(true, $"{users.Length} users found.", new { Users = userResponses, PageIndex = pageIndex, PageSize = pageSize, PageCount = pageCount }));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing users on page {PageIndex} of size {PageSize}.", pageIndex, pageSize);
            return StatusCode(500, new ApiResponse(false, "Error listing users."));
        }
    }

    [HttpGet("list-combined")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Gets a paged, searchable list of both registered users and pending invitations")]
    [SwaggerResponse(200, "Combined list loaded successfully")]
    [SwaggerResponse(500, "Server error")]
    public async Task<IActionResult> GetUsersCombined(int pageIndex = 1, int pageSize = 50, string? searchQuery = null)
    {
        if (pageIndex < 1)
            return BadRequest(new ApiResponse(false, "Page index cannot be lower than 1."));

        if (pageSize < 1)
            return BadRequest(new ApiResponse(false, "Page size cannot be lower than 1."));

        try
        {
            await using var database = await dbFactory.CreateDbContextAsync();

            // Query matching users
            IQueryable<User> userQuery = database.Users;
            if (!string.IsNullOrEmpty(searchQuery))
                userQuery = userQuery.Where(u =>
                    EF.Functions.ILike(u.Email!, $"%{searchQuery}%") ||
                    EF.Functions.ILike(u.FirstName + " " + u.LastName, $"%{searchQuery}%"));

            User[] matchedUsers = await userQuery.OrderBy(u => u.Email).ToArrayAsync();

            // Query matching pending invitations
            IQueryable<Invitation> inviteQuery = database.Invitations
                .Where(i => i.CreatedAt > DateTime.UtcNow.AddHours(-168));
            if (!string.IsNullOrEmpty(searchQuery))
                inviteQuery = inviteQuery.Where(i => EF.Functions.ILike(i.Email, $"%{searchQuery}%"));

            Invitation[] matchedInvites = await inviteQuery.OrderBy(i => i.Email).ToArrayAsync();

            // Build unified entries — invitations first so pending actions are visible at the top
            var combined = new List<object>();

            foreach (var invite in matchedInvites)
                combined.Add(new { type = "invited", id = invite.Id, email = invite.Email, role = invite.Role, createdAt = invite.CreatedAt, firstName = (string?)null, lastName = (string?)null, emailConfirmed = false, customAvatarVersion = (int?)null });

            foreach (var user in matchedUsers)
            {
                IList<string> roles = await userManager.GetRolesAsync(user);
                combined.Add(new { type = "user", id = new Guid(user.Id), email = user.Email, role = roles.FirstOrDefault() ?? "user", createdAt = (DateTime?)null, firstName = user.FirstName, lastName = user.LastName, emailConfirmed = user.EmailConfirmed, customAvatarVersion = user.HasCustom ? user.CustomAvatarVersion : (int?)null });
            }

            int totalCount = combined.Count;
            int pageCount = (int)Math.Ceiling((double)totalCount / pageSize);
            var page = combined.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToList();

            return Ok(new ApiResponse(true, $"{totalCount} entries found.", new { Items = page, PageIndex = pageIndex, PageSize = pageSize, PageCount = pageCount, TotalCount = totalCount }));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error listing combined users and invitations");
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
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(500, "Internal server error.")]
    [SwaggerResponse(403, "This action is forbidden")]
    public async Task<IActionResult> UpdateMail([FromBody] UpdateEmailDto dto)
    {
        try
        {
            await using var database = await dbFactory.CreateDbContextAsync();
        
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

    [HttpPost("avatar")]
    [Authorize]
    [SwaggerOperation(Summary = "Upload or replace the current user's avatar.")]
    [SwaggerResponse(200, "Avatar uploaded successfully.")]
    [SwaggerResponse(400, "Invalid file.")]
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<IActionResult> UploadAvatar(IFormFile newAvatar)
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return BadRequest("User not found.");

        User? user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        if (newAvatar.Length > Constants.MaxAvatarSize)
            return BadRequest($"Avatar file is too large (max {Constants.MaxAvatarSizeInMb}MB)");

        string[] allowedTypes = ["image/png", "image/jpeg", "image/webp"];
        if (!allowedTypes.Contains(newAvatar.ContentType))
            return BadRequest("Invalid image type. PNG, JPEG or WebP expected");

        await storageService.UploadObjectAsync(bucketName, userId, newAvatar.OpenReadStream());

        user.CustomAvatarVersion++;
        user.HasCustom = true;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return StatusCode(500, "Failed to update user record.");

        return Ok(new ApiResponse(true, "Avatar uploaded successfully.", user.CustomAvatarVersion));
    }

    [HttpDelete("avatar")]
    [Authorize]
    [SwaggerOperation(Summary = "Remove the current user's avatar.")]
    [SwaggerResponse(200, "Avatar removed successfully.")]
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<IActionResult> DeleteAvatar()
    {
        string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
            return BadRequest("User not found.");

        User? user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound("User not found.");

        if (user.HasCustom)
        {
            await storageService.DeleteObjectAsync(bucketName, userId);
            user.HasCustom = false;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return StatusCode(500, "Failed to update user record.");
        }

        return Ok(new ApiResponse(true, "Avatar removed successfully."));
    }

    /// <summary>
    /// Updates the current user.
    /// This method updates the current user's information, including their first name, last name, and email.
    /// It requires the user to be authenticated.
    /// </summary>
    /// <param name="dto">The data transfer object containing the user's updated information.</param>
    /// <returns>An IActionResult with information about the success of the action.</returns>
    [HttpPatch("update-details")]
    [SwaggerOperation(
        Summary = "Update the current user.",
        Description = "Updates the current user's information."
    )]
    [SwaggerResponse(200, "User updated successfully.")]
    [SwaggerResponse(400, "User email already exists.")]
    [SwaggerResponse(404, "User not found.")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<IActionResult> UpdateDetails([FromBody] UpdateUserDto dto)
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

            await using var database = await dbFactory.CreateDbContextAsync();

            // Use a transaction to ensure that all changes are saved or none
            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await database.Database.BeginTransactionAsync())
            {
                try
                {
                    // Update the first and last name
                    if (!string.IsNullOrEmpty(dto.NewFirstName))
                        user.FirstName = dto.NewFirstName;

                    if (!string.IsNullOrEmpty(dto.NewLastName))
                        user.LastName = dto.NewLastName;

                    // Update the email
                    if (!string.IsNullOrEmpty(dto.NewEmail))
                    {
                        string token = await userManager.GenerateChangeEmailTokenAsync(user, dto.NewEmail);
                        IdentityResult emailResponse = await userManager.ChangeEmailAsync(user, dto.NewEmail, token);

                        // Check if the email update was successful
                        if (!emailResponse.Succeeded)
                            return BadRequest(new ApiResponse(false, "User email already exists."));

                        // Update the username
                        IdentityResult usernameResponse = await userManager.SetUserNameAsync(user, dto.NewEmail);

                        // Check if the username update was successful
                        if (!usernameResponse.Succeeded)
                            return BadRequest(new ApiResponse(false, "Email format is not supported in our database."));
                    }

                    IdentityResult updateResponse = await userManager.UpdateAsync(user);

                    // Check if the update was successful
                    if (!updateResponse.Succeeded)
                        return BadRequest(new ApiResponse(false, "Failed to update the data."));

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
        Summary = "Delete current user.",
        Description = "Deletes current user."
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

            // Delete user from VPN first (can be retried if app deletion fails)
            if (!env.IsDevelopment())
            {
                try
                {
                    await vpnService.DeleteUserWithNodes(user.Id);
                }
                catch (Exception vpnEx)
                {
                    logger.Error(vpnEx, "Failed to delete VPN user {UserId}", user.Id);
                    return StatusCode(500, new { message = "Failed to delete VPN user" });
                }
            }

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
