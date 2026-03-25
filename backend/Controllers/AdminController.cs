using Microsoft.AspNetCore.Mvc;
using Serilog;
using KnowledgeBank.Responses;
using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using KnowledgeBank.BackgroundServices;
using KnowledgeBank.Services;
using KnowledgeBank.Services.Storage;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class AdminController(IBackgroundTaskQueue taskQueue, EnvironmentConfig environmentConfig, IServiceScopeFactory serviceScopeFactory) : ControllerBase
    {
        private readonly Serilog.ILogger logger = Log.ForContext<AdminController>();
        private readonly string bucketName = environmentConfig.GetVariableValue(EnvironmentVariable.S3_BUCKET_NAME);

        /// <summary>
        /// Re-embeds all resources, persons, and organisations using the current embedding model.
        /// Useful after switching embedding models.
        /// </summary>
        [HttpPost("reembed")]
        [Authorize(Policy = "RequireAdminRole")]
        public IActionResult ReEmbed()
        {
            taskQueue.QueueBackgroundWorkItem(async token =>
            {
                using var scope = serviceScopeFactory.CreateScope();
                var ragManager = scope.ServiceProvider.GetRequiredService<RAGManager>();
                var rm = scope.ServiceProvider.GetRequiredService<ResourceManager>();
                var storage = scope.ServiceProvider.GetRequiredService<IStorageService>();

                // Re-embed all resources
                var resources = await rm.GetAllResourcesAsync();
                logger.Information("Re-embedding {Count} resources", resources.Length);

                foreach (var resource in resources)
                {
                    try
                    {
                        if (resource.FileType == "document" && resource.FileExt != null)
                        {
                            var dlResponse = await storage.DownloadObjectAsync(bucketName, resource.Id.ToString());
                            await using var dlStream = dlResponse.Stream;
                            await ragManager.ResourcePipeline(resource.Id, $"{resource.Title}\n{resource.Description}", resource.FileExt, dlStream);
                        }
                        else if (resource.FileType == "website")
                        {
                            await ragManager.ResourcePipeline(resource.Id, $"{resource.Title}\n{resource.Description}\n{resource.SourceUrl}");
                        }
                        else
                        {
                            await ragManager.ResourcePipeline(resource.Id, $"{resource.Title}\n{resource.Description}");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error(ex, "Failed to re-embed resource {Id}", resource.Id);
                    }
                }

                // Re-embed all persons
                var persons = await rm.GetAllPersonsAsync();
                logger.Information("Re-embedding {Count} persons", persons.Length);

                foreach (var person in persons)
                {
                    try { await ragManager.PersonEntityPipeline(person.Id); }
                    catch (Exception ex) { logger.Error(ex, "Failed to re-embed person {Id}", person.Id); }
                }

                // Re-embed all organisations
                var organisations = await rm.GetAllOrganisationsAsync();
                logger.Information("Re-embedding {Count} organisations", organisations.Length);

                foreach (var organisation in organisations)
                {
                    try { await ragManager.OrganisationEntityPipeline(organisation.Id); }
                    catch (Exception ex) { logger.Error(ex, "Failed to re-embed organisation {Id}", organisation.Id); }
                }

                logger.Information("Re-embedding complete");
            });

            return Accepted(new ApiResponse(true, "Re-embedding started in the background"));
        }
    }
}
