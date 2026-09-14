using KnowledgeBank.Services.AI;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("[controller]")]
public class StatusController(MistralStatusService mistralStatusService) : AppControllerBase
{
    [HttpGet]
    [SwaggerOperation(
        Summary = "Retrieves the status of the system",
        Description = "Retrieves the status of the system"
    )]
    [SwaggerResponse(200, "Server online")]
    public IActionResult Status()
    {
        return Ok("online");
    }

    [HttpGet("mistral")]
    [SwaggerOperation(
        Summary = "Retrieves Mistral's current availability as tracked from real API call outcomes",
        Description = "Reflects whether recent calls to Mistral (chat completions, OCR) are succeeding. Not a live check - see status.mistral.ai for Mistral's own incident reporting."
    )]
    [SwaggerResponse(200, "Status returned")]
    public IActionResult MistralStatus()
    {
        MistralStatusSnapshot snapshot = mistralStatusService.Current;

        return Ok(new
        {
            status = snapshot.Status == MistralAvailability.Down ? "down" : "operational",
            downSince = snapshot.DownSinceUtc,
            lastChecked = snapshot.LastCheckedUtc,
            lastError = snapshot.LastError
        });
    }
}
