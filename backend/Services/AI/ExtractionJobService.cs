using System.Collections.Concurrent;
using KnowledgeBank.Data;
using KnowledgeBank.Models;
using KnowledgeBank.Services.Storage;
using KnowledgeBank.Utils;
using Serilog;

namespace KnowledgeBank.Services.AI;

public class ExtractionJobService(
    IServiceScopeFactory serviceScopeFactory,
    IStorageService storageService,
    TextExtractionService textExtractionService,
    EnvironmentConfig environmentConfig)
{
    private readonly Serilog.ILogger logger = Log.ForContext<ExtractionJobService>();
    private readonly ConcurrentDictionary<string, MetadataExtractionJob> _jobs = new();
    private readonly TimeSpan _jobRetentionPeriod = TimeSpan.FromHours(24);
    private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

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
                job.CompletedAt = DateTime.UtcNow;
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

    public async Task ProcessJobAsync(string jobId, string type, string value)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var metadataExtractionService = scope.ServiceProvider.GetRequiredService<MetadataExtractionService>();

        try
        {
            UpdateJobStatus(jobId, JobStatus.Processing, "Starting extraction...", 10);
            ExtractedMetadata? metadata = null;

            if (type == "file")
            {
                logger.Information("Metadata extraction requested for file '{Id}'", value);
                UpdateJobStatus(jobId, JobStatus.Processing, "Retrieving file...", 20);

                ObjectDownloadResponse response = await storageService.DownloadObjectAsync(bucketName, value);
                string extension = response.Metadata["extension"];
                string fileName = Uri.UnescapeDataString(response.Metadata["originalFileName"]);

                if (!Filetype.SupportedText(extension))
                {
                    SetJobError(jobId, "This filetype is not supported for metadata extraction");
                    return;
                }

                UpdateJobStatus(jobId, JobStatus.Processing, "Extracting text from document...", 30);

                using Stream blobStream = response.Stream;
                using MemoryStream memoryStream = new();
                await blobStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                string extractedText = await textExtractionService.ExtractTextFromFileAsync(memoryStream, extension);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    SetJobError(jobId, "No text could be extracted from the document");
                    return;
                }

                metadata = await metadataExtractionService.ExtractMetadataFromFileAsync(extractedText, fileName,
                    (msg, pct) => UpdateJobStatus(jobId, JobStatus.Processing, msg, pct));
            }
            else if (Filetype.IsDocumentUrl(value))
            {
                logger.Information("Metadata extraction requested for document URL '{Url}'", value);
                UpdateJobStatus(jobId, JobStatus.Processing, "Downloading document...", 20);

                using HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(60) };
                using HttpResponseMessage httpResponse = await httpClient.GetAsync(value);
                httpResponse.EnsureSuccessStatusCode();

                string extension = Filetype.GetDocumentUrlExtension(value);
                string fileName = Path.GetFileNameWithoutExtension(new Uri(value).LocalPath);

                if (!Filetype.SupportedText(extension))
                {
                    SetJobError(jobId, "This filetype is not supported for metadata extraction");
                    return;
                }

                using Stream downloadStream = await httpResponse.Content.ReadAsStreamAsync();
                using MemoryStream memoryStream = new();
                await downloadStream.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                UpdateJobStatus(jobId, JobStatus.Processing, "Extracting text from document...", 30);
                string extractedText = await textExtractionService.ExtractTextFromFileAsync(memoryStream, extension);

                if (string.IsNullOrWhiteSpace(extractedText))
                {
                    SetJobError(jobId, "No text could be extracted from the document");
                    return;
                }

                metadata = await metadataExtractionService.ExtractMetadataFromFileAsync(extractedText, fileName,
                    (msg, pct) => UpdateJobStatus(jobId, JobStatus.Processing, msg, pct));
            }
            else
            {
                logger.Information("Metadata extraction requested for web URL '{Url}'", value);
                UpdateJobStatus(jobId, JobStatus.Processing, "Fetching webpage...", 20);

                ReadabilityResult readabilityResult = await textExtractionService.ExtractTextFromWebAsync(value);

                if (string.IsNullOrWhiteSpace(readabilityResult.TextContent))
                {
                    SetJobError(jobId, "No text could be extracted from the webpage");
                    return;
                }

                if (readabilityResult.TextContent.Length < 200)
                {
                    SetJobError(jobId, "The webpage returned too little content. It may be behind a login or access restriction.");
                    return;
                }

                metadata = await metadataExtractionService.ExtractMetadataFromWebAsync(readabilityResult, value,
                    (msg, pct) => UpdateJobStatus(jobId, JobStatus.Processing, msg, pct));
            }

            if (metadata == null)
            {
                SetJobError(jobId, "Failed to extract metadata");
                return;
            }

            SetJobResult(jobId, metadata);
        }
        catch (InvalidOperationException e)
        {
            logger.Warning(e, "Metadata extraction failed for {Type} '{Value}'", type, value);
            SetJobError(jobId, e.Message);
        }
        catch (Exception e)
        {
            logger.Error(e, "Error extracting metadata from {Type} '{Value}'", type, value);
            SetJobError(jobId, $"Internal error: {e.Message}");
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
            _jobs.TryRemove(jobId, out _);
    }
}