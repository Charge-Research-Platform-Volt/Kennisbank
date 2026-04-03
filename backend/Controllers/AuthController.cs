using System.Security.Claims;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services;

namespace KnowledgeBank.Controllers
{
    /// <summary>
    /// Controller that handles authorization based actions.
    /// </summary>
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    public class AuthController (SignInManager<User> signInManager, IDbContextFactory<DatabaseContext> dbFactory, EnvironmentConfig _environmentConfig, MailUtils _mailUtils, IStorageService storageService, VPNService vpnService, IWebHostEnvironment env) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<AuthController>();
        private readonly string bucketName = _environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

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
                await signInManager.SignOutAsync();
                return Ok();
            }

            return Unauthorized();
        }

        /// <summary>
        /// Gets the id of the current user.
        ///
        /// </summary>
        /// <returns>User id of the current user</returns>
        [HttpGet("get-user-id")]
        [SwaggerOperation(
            Summary = "Gets current user id",
            Description = "Gets the id of the current user."
        )]
        [SwaggerResponse(200, "User id fetched successfully")]
        [SwaggerResponse(500, "Server error")]
        public IActionResult GetUserId()
        {
            string userID = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid guid) ? guid.ToString() : null ?? "";
            return Ok(new ApiResponse(true, "Fetched user id", userID ));
        }

        [HttpGet]
        [Authorize]
        [Route("ping")]
        [SwaggerOperation(Summary = "Gets the current user's email", Description = "Returns the email of the currently authenticated user")]
        [SwaggerResponse(200, "The user's email was returned", typeof(object))]
        [SwaggerResponse(401, "The user is not authenticated")]
        public IActionResult Ping()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
                return Unauthorized(new { message = "User not authenticated" });

            User? user = signInManager.UserManager.FindByIdAsync(userId).Result;
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            return Ok(new { Email = user.Email });
        }

        [HttpPost("invite")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Invite a new user", Description = "Invite a new user to the system")]
        [SwaggerResponse(200, "The user has been invited")]
        [SwaggerResponse(401, "The user is not authenticated")]
        public async Task<IActionResult> Invite([FromBody] InviteDto dto)
        {
            try
            {
                // generate a token
                Guid token = Guid.NewGuid();
                Guid invitationId = Guid.NewGuid();

                await using var database = await dbFactory.CreateDbContextAsync();

                // prevent duplicate pending invitations for the same email
                bool alreadyInvited = database.Invitations.Any(i => i.Email == dto.Email
                                                                  && i.CreatedAt > DateTime.UtcNow.AddHours(-168));
                if (alreadyInvited)
                    return Conflict(new ApiResponse(false, "A pending invitation already exists for this email."));

                // save the invitation
                database.Invitations.Add(new Invitation
                {
                    Id = invitationId,
                    Email = dto.Email,
                    Token = ShaUtils.Sha256(token.ToString()),
                    CreatedAt = DateTime.UtcNow,
                    Role = dto.Role,
                });

                await database.SaveChangesAsync();

                // Create VPN user
                string vpnUserId = string.Empty;
                string preAuthKey = string.Empty;

                if (!env.IsDevelopment())
                {
                    try
                    {
                        vpnUserId = await vpnService.CreateUser(invitationId.ToString());
                        preAuthKey = await vpnService.GetPreAuthKey(vpnUserId);
                    }
                    catch (Exception e)
                    {
                        logger.Error(e, "Failed to create VPN user for invitation {InvitationId}, cleaning up", invitationId);
                        database.Invitations.Remove(database.Invitations.First(i => i.Id == invitationId));
                        await database.SaveChangesAsync();
                        return BadRequest(new { message = "Failed to create VPN user" });
                    }
                }

                // send the email
                //S_mailUtils.SendMail(email, "Invitation", $"You have been invited to join KnowledgeBank. Create an account: {_environmentConfig.GetVariableValue(EnvironmentVariable.HOST_URL)}/signup?token={token} \n\n Preauthkey: {preAuthKey}");
                _mailUtils.SendInviteMail(dto.Email, preAuthKey, $"{_environmentConfig.GetVariableValue(EnvironmentVariable.HOST_URL)}/signup?token={token}");
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to send invitation");
                return BadRequest(new { message = "Failed to send invitation" });
            }

            return Ok();
        }

        [HttpGet("invitations")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "List pending invitations")]
        [SwaggerResponse(200, "List of pending invitations")]
        public async Task<IActionResult> ListInvitations()
        {
            await using var database = await dbFactory.CreateDbContextAsync();

            var invitations = database.Invitations
                .Where(i => i.CreatedAt > DateTime.UtcNow.AddHours(-168))
                .Select(i => new { i.Id, i.Email, i.Role, i.CreatedAt })
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            return Ok(new ApiResponse(true, $"Found {invitations.Count} pending invitation(s)", invitations));
        }

        [HttpPost("resend-invite/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Resend an invitation email")]
        [SwaggerResponse(200, "Invitation resent")]
        [SwaggerResponse(404, "Invitation not found or expired")]
        public async Task<IActionResult> ResendInvite(Guid id)
        {
            await using var database = await dbFactory.CreateDbContextAsync();

            Invitation? invitation = database.Invitations
                .FirstOrDefault(i => i.Id == id && i.CreatedAt > DateTime.UtcNow.AddHours(-168));

            if (invitation == null)
                return NotFound(new ApiResponse(false, "Invitation not found or expired."));

            try
            {
                // Generate a fresh token and extend expiry
                Guid newToken = Guid.NewGuid();
                invitation.Token = ShaUtils.Sha256(newToken.ToString());
                invitation.CreatedAt = DateTime.UtcNow;
                await database.SaveChangesAsync();

                string preAuthKey = string.Empty;
                if (!env.IsDevelopment())
                {
                    string? vpnUserId = await vpnService.GetUserId(invitation.Id.ToString());
                    if (!string.IsNullOrEmpty(vpnUserId))
                        preAuthKey = await vpnService.GetPreAuthKey(vpnUserId);
                }

                _mailUtils.SendInviteMail(invitation.Email, preAuthKey, $"{_environmentConfig.GetVariableValue(EnvironmentVariable.HOST_URL)}/signup?token={newToken}");

                return Ok(new ApiResponse(true, "Invitation resent."));
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to resend invitation {InvitationId}", id);
                return StatusCode(500, new ApiResponse(false, "Failed to resend invitation."));
            }
        }

        [HttpDelete("invitations/{id}")]
        [Authorize(Policy = "RequireAdminRole")]
        [SwaggerOperation(Summary = "Cancel a pending invitation")]
        [SwaggerResponse(200, "Invitation cancelled")]
        [SwaggerResponse(404, "Invitation not found")]
        public async Task<IActionResult> CancelInvitation(Guid id)
        {
            await using var database = await dbFactory.CreateDbContextAsync();

            Invitation? invitation = database.Invitations.FirstOrDefault(i => i.Id == id);
            if (invitation == null)
                return NotFound(new ApiResponse(false, "Invitation not found."));

            database.Invitations.Remove(invitation);
            await database.SaveChangesAsync();

            return Ok(new ApiResponse(true, "Invitation cancelled."));
        }

        [HttpPost("signup")]
        [SwaggerOperation(Summary = "Register a new user", Description = "Register a new user")]
        [SwaggerResponse(200, "The user has been registered")]
        [SwaggerResponse(400, "Bad request")]
        public async Task<IActionResult> Register([FromForm] SignUpDto signUpDto)
        {
            await using var context = await dbFactory.CreateDbContextAsync();
        
            using (Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction = await context.Database.BeginTransactionAsync())
            {
                try
                {
                    // check if there is a recent invitation for the email and token
                    Invitation? invitation = context.Invitations.FirstOrDefault(i => i.Email == signUpDto.Email
                                                                            && i.Token == ShaUtils.Sha256(signUpDto.Token)
                                                                            && i.CreatedAt > DateTime.UtcNow.AddHours(-168));
                    if (invitation == null)
                    {
                        return BadRequest(new ApiResponse(false, "Invalid invitation"));
                    }

                    // Remove the user's invitation
                    context.Remove(invitation);
                    await context.SaveChangesAsync();

                    // create the user
                    User user = new User(signUpDto.FirstName, signUpDto.LastName, signUpDto.Email);

                    // Upload avatar if provided
                    if (signUpDto.Avatar != null && signUpDto.Avatar.Length > 0)
                    {
                        string[] allowedTypes = ["image/png", "image/jpeg", "image/webp"];
                        if (!allowedTypes.Contains(signUpDto.Avatar.ContentType))
                            return BadRequest(new ApiResponse(false, "Invalid image type. PNG, JPEG or WebP expected"));

                        if (signUpDto.Avatar.Length > Constants.MaxAvatarSize)
                            return BadRequest(new ApiResponse(false, $"Avatar file is too large (max {Constants.MaxAvatarSizeInMb}MB)"));

                        await storageService.UploadObjectAsync(bucketName, user.Id, signUpDto.Avatar.OpenReadStream(), new Dictionary<string, string>());

                        user.CustomAvatarVersion++;
                        user.HasCustom = true;
                    }

                    // save the user
                    IdentityResult result = await signInManager.UserManager.CreateAsync(user, signUpDto.Password);
                    if (!result.Succeeded)
                        return BadRequest(new ApiResponse(false, string.Join(" ", result.Errors.Select(e => e.Description))));

                    IdentityResult roleResult = await signInManager.UserManager.AddToRoleAsync(user, invitation.Role);

                    if (!roleResult.Succeeded)
                        return BadRequest(new ApiResponse(false, string.Join(" ", roleResult.Errors.Select(e => e.Description))));

                    await transaction.CommitAsync();

                    // Update the VPN user name from invitation ID to user ID
                    try
                    {
                        string? vpnUserId = await vpnService.GetUserId(invitation.Id.ToString());

                        if (string.IsNullOrEmpty(vpnUserId))
                            throw new Exception("User not found");
                        
                        await vpnService.RenameUser(vpnUserId, user.Id.ToString());
                    }
                    catch (Exception vpnEx)
                    {
                        logger.Error(vpnEx, "Failed to rename VPN user from invitation {InvitationId} to user {UserId}", invitation.Id, user.Id);
                    }

                    return Ok(new ApiResponse(true, $"User '{user.UserName}' created successfully."));
                }
                catch (Exception e)
                {
                    await transaction.RollbackAsync();
                    logger.Error(e, "Error creating user");
                    return BadRequest(new ApiResponse(false, "Error creating user"));
                }
            }
        }

        [HttpPut("update-password")]
        [SwaggerOperation(Summary = "Updates the user's password", Description = "Updates the user's password")]
        [SwaggerResponse(200, "The password has been changed")]
        [SwaggerResponse(400, "Bad request")]
        public async Task<IActionResult> UpdatePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (userId == null)
                    return BadRequest(new ApiResponse(false, "User not found"));

                User? user = await signInManager.UserManager.FindByIdAsync(userId);

                if (user == null)
                    return NotFound("User not found.");

                IdentityResult updateResponse = await signInManager.UserManager.ChangePasswordAsync(
                    user,
                    changePasswordDto.CurrentPassword,
                    changePasswordDto.NewPassword
                );

                if (!updateResponse.Succeeded)
                    return Ok(new ApiResponse(false, string.Join(" ", updateResponse.Errors.Select(e => e.Description))));

                return Ok(new ApiResponse(true, "Password updated successfully"));
            }
            catch (Exception e)
            {
                logger.Error(e, "Error creating user");
                return BadRequest(new { message = "Error creating user" });
            }
        }
    }
}
