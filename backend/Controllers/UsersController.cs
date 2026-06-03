using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using KnowledgeBank.Utils;
using Microsoft.Extensions.Options;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services;
using KnowledgeBank.Services.Domain;
using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeBank.Controllers;

[Route("users")]
public class UsersController(UserService userService, IStorageService storageService, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IOptions<OwnerUserConfig> ownerConfig, EnvironmentConfig environmentConfig, VPNService vpnService, IWebHostEnvironment env, MailUtils mailUtils) : AppControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<UsersController>();
    private readonly OwnerUserConfig ownerConfig = ownerConfig.Value;
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

    [HttpGet("me")]
    [Authorize]
    [SwaggerOperation(Summary = "Get the current user's account info.")]
    [SwaggerResponse(200, "Account info found.")]
    public async Task<IActionResult> GetCurrentAccount()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        User? user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();

        IList<string> roles = await userManager.GetRolesAsync(user);

        return Ok(new {
            Id = new Guid(user.Id),
            user.FirstName,
            user.LastName,
            user.Email,
            CustomAvatarVersion = user.HasCustom ? user.CustomAvatarVersion : (int?)null,
            user.EmailConfirmed,
            Role = roles.FirstOrDefault() ?? "No Role"
        });
    }

    [HttpGet("{userId}/avatar")]
    [SwaggerOperation(Summary = "Get a user's profile avatar.")]
    [SwaggerResponse(200, "Profile avatar returned.")]
    [SwaggerResponse(404, "No avatar found for user.")]
    public async Task<IActionResult> GetAvatar(string userId)
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

    [HttpGet]
    [Authorize]
    [SwaggerOperation(Summary = "Gets a paged, searchable list of users")]
    [SwaggerResponse(200, "Users loaded successfully")]
    public async Task<IActionResult> GetUsersPaged(int page = 1, int pageSize = 100, string? search = null, string? excludeId = null)
    {
        if (page < 1) return Problem("Page index cannot be lower than 1.", statusCode: 400);
        if (pageSize < 1) return Problem("Page size cannot be lower than 1.", statusCode: 400);

        var (users, totalCount) = await userService.GetPageAsync(page, pageSize, search, excludeId);
        var roleMap = await userService.GetRoleMapAsync(users.Select(u => u.Id));

        int pageCount = (int)Math.Ceiling((double)totalCount / pageSize);

        var userItems = users.Select(user => new {
            Id = new Guid(user.Id),
            user.FirstName,
            user.LastName,
            user.Email,
            CustomAvatarVersion = user.HasCustom ? user.CustomAvatarVersion : (int?)null,
            user.EmailConfirmed,
            Role = roleMap.TryGetValue(user.Id, out var role) ? role : "No Role"
        });

        return Ok(new { Users = userItems, Page = page, PageSize = pageSize, PageCount = pageCount });
    }

    [HttpGet("combined")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Gets a combined list of registered users and pending invitations")]
    [SwaggerResponse(200, "Combined list loaded successfully")]
    public async Task<IActionResult> GetUsersCombined(int page = 1, int pageSize = 50, string? search = null)
    {
        if (page < 1) return Problem("Page index cannot be lower than 1.", statusCode: 400);
        if (pageSize < 1) return Problem("Page size cannot be lower than 1.", statusCode: 400);

        var (matchedUsers, matchedInvites) = await userService.GetCombinedAsync(search);
        var roleMap = await userService.GetRoleMapAsync(matchedUsers.Select(u => u.Id));

        var combined = new List<object>();

        foreach (var invite in matchedInvites)
            combined.Add(new { type = "invited", id = invite.Id, email = invite.Email, role = invite.Role, createdAt = invite.CreatedAt, firstName = (string?)null, lastName = (string?)null, emailConfirmed = false, customAvatarVersion = (int?)null });

        foreach (var user in matchedUsers)
            combined.Add(new { type = "user", id = new Guid(user.Id), email = user.Email, role = roleMap.TryGetValue(user.Id, out var r) ? r : "user", createdAt = (DateTime?)null, firstName = user.FirstName, lastName = user.LastName, emailConfirmed = user.EmailConfirmed, customAvatarVersion = user.HasCustom ? user.CustomAvatarVersion : (int?)null });

        int totalCount = combined.Count;
        int pageCount = (int)Math.Ceiling((double)totalCount / pageSize);
        var paged = combined.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Ok(new { Items = paged, Page = page, PageSize = pageSize, PageCount = pageCount, TotalCount = totalCount });
    }

    [HttpGet("roles")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "List all available roles.")]
    [SwaggerResponse(200, "A list of roles.")]
    public async Task<IActionResult> GetRoles()
    {
        IdentityRole[] roles = await roleManager.Roles.ToArrayAsync();
        return Ok(roles);
    }

    [HttpPatch("{userId}/role")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(
        Summary = "Assign a role to a user.",
        Description = "Replaces the user's current role. Users can only have one role."
    )]
    [SwaggerResponse(200, "Role assigned successfully.")]
    [SwaggerResponse(400, "User already has this role.")]
    [SwaggerResponse(403, "Cannot change the owner's role.")]
    [SwaggerResponse(404, "User or role not found.")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] RoleAssignDto dto)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Problem("User not found.", statusCode: 404);

        if (user.Email == ownerConfig.Email)
            return Problem("Role of the owner account cannot be changed.", statusCode: 403);

        if (!await roleManager.RoleExistsAsync(dto.RoleName))
            return Problem("Role does not exist.", statusCode: 404);

        if (await userManager.IsInRoleAsync(user, dto.RoleName))
            return Problem("User already has this role.", statusCode: 400);

        IList<string> currentRoles = await userManager.GetRolesAsync(user);
        IdentityResult removeResult = await userManager.RemoveFromRolesAsync(user, currentRoles);

        if (!removeResult.Succeeded)
            throw new InvalidOperationException($"Failed to remove roles: {string.Join(", ", removeResult.Errors.Select(e => e.Description))}");

        IdentityResult result = await userManager.AddToRoleAsync(user, dto.RoleName);

        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to assign role: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        string actorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        logger.Information("Role {Role} assigned to user {UserId} by admin {ActorId}", dto.RoleName, userId, actorId);
        return Ok();
    }

    [HttpPatch("{userId}/email")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Update a user's email (admin only).")]
    [SwaggerResponse(200, "User email updated successfully.")]
    [SwaggerResponse(400, "Bad request.")]
    [SwaggerResponse(403, "This action is forbidden.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> UpdateEmail(string userId, [FromBody] UpdateEmailDto dto)
    {
        User? user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("Invalid user ID.");

        if (user.Email == ownerConfig.Email)
            return Problem("Email of owner account cannot be changed.", statusCode: 403);

        if (user.Email == dto.Email)
            return BadRequest($"User already has the email '{dto.Email}'.");

        string token = await userManager.GenerateChangeEmailTokenAsync(user, dto.Email);
        IdentityResult emailResponse = await userManager.ChangeEmailAsync(user, dto.Email, token);
        if (!emailResponse.Succeeded)
            return BadRequest(emailResponse.Errors);

        IdentityResult usernameResponse = await userManager.SetUserNameAsync(user, dto.Email);
        if (!usernameResponse.Succeeded)
            return BadRequest(usernameResponse.Errors);

        logger.Information("User email changed. User ID: {UserId}, New email: {Email}", userId, dto.Email);
        return Ok(new { message = "User email changed successfully." });
    }

    [HttpPost("avatar")]
    [Authorize]
    [SwaggerOperation(Summary = "Upload or replace the current user's avatar.")]
    [SwaggerResponse(200, "Avatar uploaded successfully.")]
    [SwaggerResponse(400, "Invalid file.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> UploadAvatar(IFormFile newAvatar)
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        User? user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

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

        return Ok(user.CustomAvatarVersion);
    }

    [HttpDelete("avatar")]
    [Authorize]
    [SwaggerOperation(Summary = "Remove the current user's avatar.")]
    [SwaggerResponse(200, "Avatar removed successfully.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> DeleteAvatar()
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        User? user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        if (user.HasCustom)
        {
            await storageService.DeleteObjectAsync(bucketName, userId);
            user.HasCustom = false;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                return StatusCode(500, "Failed to update user record.");
        }

        return Ok();
    }

    [HttpPatch("me")]
    [Authorize]
    [SwaggerOperation(Summary = "Update the current user's details.")]
    [SwaggerResponse(200, "User updated successfully.")]
    [SwaggerResponse(400, "Bad request.")]
    [SwaggerResponse(403, "This action is forbidden.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> UpdateDetails([FromBody] UpdateUserDto dto)
    {
        string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        User? user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("User not found.");

        if (user.Email == ownerConfig.Email)
            return Problem("The owner account cannot be altered.", statusCode: 403);

        if (!string.IsNullOrEmpty(dto.NewFirstName)) user.FirstName = dto.NewFirstName;
        if (!string.IsNullOrEmpty(dto.NewLastName)) user.LastName = dto.NewLastName;

        if (!string.IsNullOrEmpty(dto.NewEmail))
        {
            string token = await userManager.GenerateChangeEmailTokenAsync(user, dto.NewEmail);
            IdentityResult emailResponse = await userManager.ChangeEmailAsync(user, dto.NewEmail, token);
            if (!emailResponse.Succeeded)
                return BadRequest(new { message = "User email already exists." });

            IdentityResult usernameResponse = await userManager.SetUserNameAsync(user, dto.NewEmail);
            if (!usernameResponse.Succeeded)
                return BadRequest(new { message = "Email format is not supported in our database." });
        }

        IdentityResult updateResponse = await userManager.UpdateAsync(user);
        if (!updateResponse.Succeeded)
            return BadRequest(new { message = "Failed to update the data." });

        return Ok();
    }

    [HttpDelete("{userId?}")]
    [Authorize]
    [SwaggerOperation(Summary = "Delete a user. Omit userId to delete own account; admins can pass a userId to delete others.")]
    [SwaggerResponse(200, "User deleted successfully.")]
    [SwaggerResponse(400, "Bad request.")]
    [SwaggerResponse(403, "This action is forbidden.")]
    [SwaggerResponse(404, "User not found.")]
    public async Task<IActionResult> Delete(string? userId)
    {
        string currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        User? currentUser = await userManager.FindByIdAsync(currentUserId);
        if (currentUser == null) return NotFound("Current user not found.");

        if (string.IsNullOrEmpty(userId))
            userId = currentUser.Id;

        if (currentUser.Id != userId)
        {
            IList<string> roles = await userManager.GetRolesAsync(currentUser);
            if (!roles.Contains("admin"))
                return BadRequest("Only admins can delete users.");
        }

        User? user = await userManager.FindByIdAsync(userId);
        if (user == null) return NotFound("Invalid user ID.");

        if (user.Email == ownerConfig.Email)
            return Problem("Owner user cannot be deleted.", statusCode: 403);

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

        logger.Information("User {Email} deleted by {ActorId}", user.Email, currentUserId);
        return Ok(new { message = "User deleted successfully." });
    }

    [HttpPost("invitations")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Invite a new user")]
    [SwaggerResponse(200, "Invitation sent")]
    [SwaggerResponse(409, "A pending invitation already exists for this email")]
    public async Task<IActionResult> Invite([FromBody] InviteDto dto)
    {
        var result = await userService.CreateInvitationAsync(dto.Email, dto.Role);
        if (result == null)
            return Conflict(new { message = "A pending invitation already exists for this email." });

        var (invitationId, rawToken) = result.Value;

        string preAuthKey = string.Empty;

        if (!env.IsDevelopment())
        {
            try
            {
                string vpnUserId = await vpnService.CreateUser(invitationId.ToString());
                preAuthKey = await vpnService.GetPreAuthKey(vpnUserId);
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to create VPN user for invitation {InvitationId}, cleaning up", invitationId);
                await userService.CancelInvitationAsync(invitationId);
                return BadRequest(new { message = "Failed to create VPN user" });
            }
        }

        mailUtils.SendInviteMail(dto.Email, preAuthKey, $"{environmentConfig.GetVariableValue(EnvironmentVariable.HOST_URL)}/signup?token={rawToken}");

        return Ok();
    }

    [HttpGet("invitations")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "List pending invitations")]
    [SwaggerResponse(200, "List of pending invitations")]
    public async Task<IActionResult> ListInvitations()
    {
        Invitation[] invitations = await userService.GetPendingInvitationsAsync();
        return Ok(invitations.Select(i => new { i.Id, i.Email, i.Role, i.CreatedAt }));
    }

    [HttpPost("invitations/{id}/resend")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Resend an invitation email")]
    [SwaggerResponse(200, "Invitation resent")]
    [SwaggerResponse(404, "Invitation not found or expired")]
    public async Task<IActionResult> ResendInvite(Guid id)
    {
        var result = await userService.RefreshInvitationAsync(id);
        if (result == null)
            return NotFound(new { message = "Invitation not found or expired." });

        var (invitation, rawToken) = result.Value;

        string preAuthKey = string.Empty;
        if (!env.IsDevelopment())
        {
            string? vpnUserId = await vpnService.GetUserId(invitation.Id.ToString());
            if (!string.IsNullOrEmpty(vpnUserId))
                preAuthKey = await vpnService.GetPreAuthKey(vpnUserId);
        }

        mailUtils.SendInviteMail(invitation.Email, preAuthKey, $"{environmentConfig.GetVariableValue(EnvironmentVariable.HOST_URL)}/signup?token={rawToken}");

        return Ok();
    }

    [HttpDelete("invitations/{id}")]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Cancel a pending invitation")]
    [SwaggerResponse(200, "Invitation cancelled")]
    [SwaggerResponse(404, "Invitation not found")]
    public async Task<IActionResult> CancelInvitation(Guid id)
    {
        bool found = await userService.CancelInvitationAsync(id);
        return found ? Ok() : NotFound(new { message = "Invitation not found." });
    }
}
