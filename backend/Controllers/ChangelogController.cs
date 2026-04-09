using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using Serilog;
using KnowledgeBank.Models;
using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;

namespace KnowledgeBank.Controllers;

[ApiController]
[Route("[controller]")]
public class ChangelogController(IDbContextFactory<DatabaseContext> dbFactory, UserManager<User> userManager) : ControllerBase
{
    private readonly Serilog.ILogger logger = Log.ForContext<ChangelogController>();

    [HttpGet("unseen")]
    [Authorize]
    [SwaggerOperation(Summary = "Get unseen changelog entries for current user.")]
    [SwaggerResponse(200, "Unseen entries returned.")]
    [SwaggerResponse(500, "Internal server error.")]
    public async Task<IActionResult> GetUnseen()
    {
        try
        {
            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new Exception("User authenticated yet not found.");
            User user = await userManager.FindByIdAsync(userId) ?? throw new Exception("User authenticated yet not found.");

            await using var database = await dbFactory.CreateDbContextAsync();

            List<ChangelogEntry> entries = await database.Changelog
                .Where(c => c.Id > user.LastSeenChangelogId)
                .OrderBy(c => c.Id)
                .ToListAsync();

            if (entries.Count > 0)
            {
                user.LastSeenChangelogId = entries[^1].Id;
                await userManager.UpdateAsync(user);
            }

            return Ok(new ApiResponse(true, $"{entries.Count} unseen entries.", entries));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error getting unseen changelog entries.");
            return StatusCode(500, new ApiResponse(false, "Internal server error."));
        }
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdminRole")]
    [SwaggerOperation(Summary = "Add a new changelog entry.")]
    [SwaggerResponse(200, "Entry added.")]
    [SwaggerResponse(500, "Internal Server Error.")]
    public async Task<IActionResult> AddEntry([FromBody] ChangelogEntryCreateDto dto)
    {
        try
        {
            await using var database = await dbFactory.CreateDbContextAsync();

            var entry = new ChangelogEntry
            {
                Title = dto.Title,
                Body = dto.Body,
            };

            database.Changelog.Add(entry);
            await database.SaveChangesAsync();

            return Ok(new ApiResponse(true, "Entry added.", entry));
        }
        catch (Exception e)
        {
            logger.Error(e, "Error adding changelog entry.");
            return StatusCode(500, new ApiResponse(false, "Internal Server Error."));
        }
    }
}