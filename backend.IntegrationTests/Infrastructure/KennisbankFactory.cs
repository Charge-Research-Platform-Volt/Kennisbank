using KnowledgeBank.Services.Background;
using KnowledgeBank.Services.Storage;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace KnowledgeBank.IntegrationTests.Infrastructure;

/// <summary>The real app, with storage faked and the scheduled background jobs switched off.</summary>
public sealed class KennisbankFactory(InMemoryStorageService storage) : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IStorageService>();
            services.AddSingleton<IStorageService>(storage);

            // Scheduled cleanup and retry jobs would act on test data (and call Headscale/S3) on their
            // own timing; only the work queue that runs ingestion stays
            ServiceDescriptor[] scheduled = [.. services.Where(d =>
                d.ServiceType == typeof(IHostedService) && d.ImplementationType != typeof(QueuedHostedService))];
            foreach (ServiceDescriptor descriptor in scheduled)
                services.Remove(descriptor);
        });
    }
}
