using KnowledgeBank.Services.Domain;
using KnowledgeBank.Utils;
using Microsoft.EntityFrameworkCore;

public class TrashbinCleanupService(IServiceProvider serviceProvider) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using IServiceScope scope = serviceProvider.CreateScope();
            ResourceService resourceService = scope.ServiceProvider.GetRequiredService<ResourceService>();
            PersonService personService = scope.ServiceProvider.GetRequiredService<PersonService>();
            OrganisationService organisationService = scope.ServiceProvider.GetRequiredService<OrganisationService>();

            DateTime threshold = DateTime.UtcNow.AddDays(-30);

            Guid[] resources = (await resourceService.GetAllAsync(predicate: r => r.TrashDate < threshold)).Select(r => r.Id).ToArray();
            Guid[] persons = (await personService.GetAllAsync(predicate: p => p.TrashDate < threshold)).Select(p => p.Id).ToArray();
            Guid[] organisations = (await organisationService.GetAllAsync(predicate: o => o.TrashDate < threshold)).Select(o => o.Id).ToArray();

            await ConcurrencyUtils.RunBoundedAsync(resources, 10, async id =>
            {
                using var itemScope = serviceProvider.CreateScope();
                await itemScope.ServiceProvider.GetRequiredService<ResourceService>().DeleteAsync(id);
            }, stoppingToken);

            await ConcurrencyUtils.RunBoundedAsync(persons, 10, async id =>
            {
                using var itemScope = serviceProvider.CreateScope();
                await itemScope.ServiceProvider.GetRequiredService<PersonService>().DeleteAsync(id);
            }, stoppingToken);

            await ConcurrencyUtils.RunBoundedAsync(organisations, 10, async id =>
            {
                using var itemScope = serviceProvider.CreateScope();
                await itemScope.ServiceProvider.GetRequiredService<OrganisationService>().DeleteAsync(id);
            }, stoppingToken);

            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken);
        }
    }
}