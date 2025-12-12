using System.Collections.Concurrent;
using KnowledgeBank.Models;

namespace KnowledgeBank.Services;

public class MetadataExtractionJobService
{
    private readonly ConcurrentDictionary<string, MetadataExtractionJob> _jobs = new();
    private readonly TimeSpan _jobRetentionPeriod = TimeSpan.FromHours(24);

    public MetadataExtractionJob CreateJob(string type, string value)
    {
        var job = new MetadataExtractionJob
        {
            Type = type,
            Value = value,
            Status = JobStatus.Pending,
            StatusMessage = "Queued for processing"
        };

        _jobs[job.JobId] = job;

        // Clean up old jobs (older than 24 hours)
        CleanupOldJobs();

        return job;
    }

    public MetadataExtractionJob? GetJob(string jobId)
    {
        _jobs.TryGetValue(jobId, out var job);
        return job;
    }

    public void UpdateJobStatus(string jobId, JobStatus status, string? statusMessage = null, int? progressPercentage = null)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Status = status;
            if (statusMessage != null) job.StatusMessage = statusMessage;
            if (progressPercentage.HasValue) job.ProgressPercentage = progressPercentage.Value;

            if (status == JobStatus.Completed || status == JobStatus.Failed)
            {
                job.CompletedAt = DateTime.UtcNow;
            }
        }
    }

    public void SetJobResult(string jobId, object result)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.Result = result;
            job.Status = JobStatus.Completed;
            job.StatusMessage = "Extraction completed successfully";
            job.ProgressPercentage = 100;
            job.CompletedAt = DateTime.UtcNow;
        }
    }

    public void SetJobError(string jobId, string errorMessage)
    {
        if (_jobs.TryGetValue(jobId, out var job))
        {
            job.ErrorMessage = errorMessage;
            job.Status = JobStatus.Failed;
            job.StatusMessage = "Extraction failed";
            job.CompletedAt = DateTime.UtcNow;
        }
    }

    private void CleanupOldJobs()
    {
        var cutoffTime = DateTime.UtcNow - _jobRetentionPeriod;
        var oldJobIds = _jobs
            .Where(kvp => kvp.Value.CreatedAt < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var jobId in oldJobIds)
        {
            _jobs.TryRemove(jobId, out _);
        }
    }
}
