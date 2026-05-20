using Microsoft.AspNetCore.Mvc;
using Serilog;
using KnowledgeBank.Data;
using KnowledgeBank.Utils;
using Microsoft.AspNetCore.Authorization;
using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.AI;
using KnowledgeBank.Services.Storage;

namespace KnowledgeBank.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Produces("application/json")]
    [Authorize]
    public class AdminController(IBackgroundTaskQueue taskQueue, EnvironmentConfig environmentConfig, IServiceScopeFactory serviceScopeFactory) : AppControllerBase
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
                var ragManager = scope.ServiceProvider.GetRequiredService<IngestionService>();
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
                            using var dlMemStream = new MemoryStream();
                            await dlResponse.Stream.CopyToAsync(dlMemStream, token);
                            dlMemStream.Position = 0;
                            await ragManager.RunResourcePipelineAsync(resource.Id, resource.FileExt, dlMemStream);
                        }
                        else
                        {
                            await ragManager.RunResourcePipelineAsync(resource.Id);
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
                    try { await ragManager.RunPersonEntityPipelineAsync(person.Id); }
                    catch (Exception ex) { logger.Error(ex, "Failed to re-embed person {Id}", person.Id); }
                }

                // Re-embed all organisations
                var organisations = await rm.GetAllOrganisationsAsync();
                logger.Information("Re-embedding {Count} organisations", organisations.Length);

                foreach (var organisation in organisations)
                {
                    try { await ragManager.RunOrganisationEntityPipelineAsync(organisation.Id); }
                    catch (Exception ex) { logger.Error(ex, "Failed to re-embed organisation {Id}", organisation.Id); }
                }

                logger.Information("Re-embedding complete");
            });

            return Accepted();
        }
    }
}
