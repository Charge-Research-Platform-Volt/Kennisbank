using Npgsql;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Data;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace backend.Tests.Infrastructure;


/// <summary>
/// Extended base class for tests that need both a PostgreSQL database and Azure Blob Storage.
/// 
/// This class provides all the features of TestBase plus:
/// 1. Setup of Azure Blob Storage services with Azurite emulator configuration
/// 2. IAzureBlobService instance available for blob operations
/// 3. Default container naming and management
/// 
/// Inherit from this class when your tests need both database access and blob storage capabilities.
/// </summary>
public abstract class TestBaseBlob : TestBase
{
    /// <summary>Azure Blob Service for blob storage operations</summary>
    protected IAzureBlobService BlobService;
    
    /// <summary>Default container name for blob storage</summary>
    protected string DefaultContainerName;
    
    /// <summary>Host that contains the service provider</summary>
    protected IHost Host;
    
    protected List<string> UploadedBlobs = new();

    /// <summary>
    /// Extension point for test class-specific one-time setup.
    /// Sets up the blob storage configuration.
    /// </summary>
    [OneTimeSetUp]
    protected override async Task OnGlobalSetUp()
    {
        await base.OnGlobalSetUp();
        
        // Initialize default container name
        DefaultContainerName = "text";
        
        // Configure host with blob storage services
        Host = new HostBuilder()
            .ConfigureAppConfiguration((context, config) =>
            {
                // Configure Azurite blob storage emulator connection string
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "AZURE_STORAGE_CONNECTION_STRING", "DefaultEndpointsProtocol=http;AccountName=devstoreaccount1;AccountKey=Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==;BlobEndpoint=http://localhost:10000/devstoreaccount1;" }
                });
            })
            .ConfigureServices(services =>
            {
                services.AddSingleton<IAzureBlobService, AzureBlobService>();
            })
            .Build();

        // Get the blob service from the configured host
        BlobService = Host.Services.GetRequiredService<IAzureBlobService>();
    }
    
    /// <summary>
    /// Teardown for each individual test method.
    /// Cleans up the blob storage and disposes services.
    /// </summary>
    [OneTimeTearDown]
    protected override async Task OnTestTearDown()
    {
        // Clean up blob storage resources
        await CleanupBlobStorage();
        
        // Dispose the host
        Host.Dispose();
        
        await base.OnTestTearDown();
    }

    /// <summary>
    /// Cleans up blob storage resources created during the test.
    /// </summary>
    protected virtual async Task CleanupBlobStorage()
    {
       foreach (string blobItemId in UploadedBlobs)
        {
            await BlobService.DeleteBlobAsync(DefaultContainerName, blobItemId);

        }

        UploadedBlobs.Clear();    
    }
}