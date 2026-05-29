using KnowledgeBank.Services.AI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace KnowledgeBank.Controllers;

[Route("ai")]
[Authorize]
public class AIController(ExtractionJobService jobService) : AppControllerBase
{
    [HttpPost("extract-metadata/start")]
    [SwaggerOperation(Summary = "Start a metadata extraction job")]
    [SwaggerResponse(200, "Job created")]
    [SwaggerResponse(400, "Invalid type or value")]
    public IActionResult StartMetadataExtraction([FromQuery] string type, [FromQuery] string value)
    {
        if (type != "file" && type != "web")
            return Problem("Invalid extraction type. Use 'file' or 'web'.", statusCode: 400);

        if (type == "file" && !Guid.TryParse(value, out _))
            return Problem("Invalid file ID.", statusCode: 400);

        if (type == "web" && !Uri.TryCreate(value, UriKind.Absolute, out _))
            return Problem("Invalid URL.", statusCode: 400);

        var job = jobService.CreateJob(type, value);
        _ = Task.Run(async () => await jobService.ProcessJobAsync(job.JobId, type, value));

        return Ok(new { jobId = job.JobId });
    }

    [HttpGet("extract-metadata/status/{jobId}")]
    [SwaggerOperation(Summary = "Get metadata extraction job status")]
    [SwaggerResponse(200, "Job status")]
    [SwaggerResponse(404, "Job not found")]
    public IActionResult GetJobStatus(string jobId)
    {
        var job = jobService.GetJob(jobId);
        if (job == null) return NotFound();

        return Ok(new
        {
            jobId = job.JobId,
            status = job.Status.ToString(),
            statusMessage = job.StatusMessage,
            progressPercentage = job.ProgressPercentage,
            result = job.Result,
            errorMessage = job.ErrorMessage,
            createdAt = job.CreatedAt,
            completedAt = job.CompletedAt
        });
    }
}