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

            foreach (Guid id in resources) await resourceService.DeleteAsync(id);
            foreach (Guid id in persons) await personService.DeleteAsync(id);
            foreach (Guid id in organisations) await organisationService.DeleteAsync(id);

            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken);
        }
    }
}