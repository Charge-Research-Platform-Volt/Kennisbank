using System.Security.Claims;
using KnowledgeBank.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Swashbuckle.AspNetCore.Annotations;
using KnowledgeBank.Utils;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Services;
using KnowledgeBank.Services.Domain;

namespace KnowledgeBank.Controllers
{
    [Route("[controller]")]
    public class AuthController(SignInManager<User> signInManager, UserService userService, EnvironmentConfig environmentConfig, IStorageService storageService, VPNService vpnService, MailUtils mailUtils) : AppControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<AuthController>();
        private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

        [HttpPost("logout")]
        [Authorize]
        [SwaggerOperation(Summary = "Logs out the current user")]
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

        [HttpGet("ping")]
        [Authorize]
        [SwaggerOperation(Summary = "Gets the current user's email")]
        [SwaggerResponse(200, "The user's email was returned")]
        [SwaggerResponse(401, "The user is not authenticated")]
        public async Task<IActionResult> Ping()
        {
            string? userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId == null)
                return Unauthorized(new { message = "User not authenticated" });

            User? user = await signInManager.UserManager.FindByIdAsync(userId);
            if (user == null)
                return Unauthorized(new { message = "User not found" });

            return Ok(new { Email = user.Email });
        }

        [HttpPost("signup")]
        [SwaggerOperation(Summary = "Register a new user via invitation token")]
        [SwaggerResponse(200, "User registered successfully")]
        [SwaggerResponse(400, "Bad request")]
        public async Task<IActionResult> Signup([FromForm] SignUpDto signUpDto)
        {
            Invitation? invitation = await userService.ValidateAndConsumeInvitationAsync(signUpDto.Token);
            if (invitation == null)
                return BadRequest(new { message = "Invalid invitation" });

            User user = new(signUpDto.FirstName, signUpDto.LastName, signUpDto.Email)
            {
                LastSeenChangelogId = await userService.GetLatestChangelogIdAsync()
            };

            if (signUpDto.Avatar != null && signUpDto.Avatar.Length > 0)
            {
                string[] allowedTypes = ["image/png", "image/jpeg", "image/webp"];
                if (!allowedTypes.Contains(signUpDto.Avatar.ContentType))
                    return BadRequest(new { message = "Invalid image type. PNG, JPEG or WebP expected" });

                if (signUpDto.Avatar.Length > Constants.MaxAvatarSize)
                    return BadRequest(new { message = $"Avatar file is too large (max {Constants.MaxAvatarSizeInMb}MB)" });

                await storageService.UploadObjectAsync(bucketName, user.Id, signUpDto.Avatar.OpenReadStream(), new Dictionary<string, string>());
                user.CustomAvatarVersion++;
                user.HasCustom = true;
            }

            IdentityResult result = await signInManager.UserManager.CreateAsync(user, signUpDto.Password);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

            IdentityResult roleResult = await signInManager.UserManager.AddToRoleAsync(user, invitation.Role);
            if (!roleResult.Succeeded)
                return BadRequest(new { message = string.Join(" ", roleResult.Errors.Select(e => e.Description)) });

            try
            {
                string? vpnUserId = await vpnService.GetUserId(invitation.Id.ToString());
                if (string.IsNullOrEmpty(vpnUserId))
                    throw new Exception("VPN user not found");
                await vpnService.RenameUser(vpnUserId, user.Id.ToString());
            }
            catch (Exception vpnEx)
            {
                logger.Error(vpnEx, "Failed to rename VPN user from invitation {InvitationId} to user {UserId}", invitation.Id, user.Id);
            }

            logger.Information("New user registered: {Email} with role {Role}", user.Email, invitation.Role);
            return Ok(new { message = $"User '{user.UserName}' created successfully." });
        }

        [HttpPatch("password")]
        [Authorize]
        [SwaggerOperation(Summary = "Updates the current user's password")]
        [SwaggerResponse(200, "Password changed")]
        [SwaggerResponse(400, "Bad request")]
        public async Task<IActionResult> UpdatePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            User? user = await signInManager.UserManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound("User not found.");

            IdentityResult updateResponse = await signInManager.UserManager.ChangePasswordAsync(
                user,
                changePasswordDto.CurrentPassword,
                changePasswordDto.NewPassword
            );

            if (!updateResponse.Succeeded)
                return BadRequest(new { message = string.Join(" ", updateResponse.Errors.Select(e => e.Description)) });

            logger.Information("Password changed for user {UserId}", userId);
            return Ok(new { message = "Password updated successfully" });
        }

        [HttpPost("forgot-password")]
        [SwaggerOperation(Summary = "Sends a password reset email if the address is registered.")]
        [SwaggerResponse(200, "If the email is registered, a reset link has been sent.")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            User? user = await signInManager.UserManager.FindByEmailAsync(dto.Email);

            if (user != null)
            {
                string token = await signInManager.UserManager.GeneratePasswordResetTokenAsync(user);
                string resetUrl = $"{environmentConfig.GetVariableValue(EnvironmentVariable.HOST_URL)}/reset-password?email={Uri.EscapeDataString(dto.Email)}&token={Uri.EscapeDataString(token)}";

                mailUtils.SendResetPasswordMail(dto.Email, resetUrl);
                logger.Information("Password reset requested for {Email}", dto.Email);
            }

            // Always 200 regardless of whethere the email exists, to avoid leaking which emails are registered
            return Ok(new { message = "A reset link has been sent, if that email is registered." });
        }

        [HttpPost("reset-password")]
        [SwaggerOperation(Summary = "Resets a user's password using a token from the forgot-password email.")]
        [SwaggerResponse(200, "Password reset successfully.")]
        [SwaggerResponse(400, "Invalid or expired token.")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            User? user = await signInManager.UserManager.FindByEmailAsync(dto.Email);
            if (user == null)
                return BadRequest(new { message = "Invalid or expired reset link." });

            IdentityResult result = await signInManager.UserManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);
            if (!result.Succeeded)
                return BadRequest(new { message = string.Join(" ", result.Errors.Select(e => e.Description)) });

            logger.Information("Password reset completed for {Email}", dto.Email);
            return Ok(new { message = "Password updated successfully." });
        }
    }
}
