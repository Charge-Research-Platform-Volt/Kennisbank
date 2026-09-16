using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Utils;
using KnowledgeBank.Models;
using KnowledgeBank.Services;
using Serilog;

/// <summary>
/// A background service that runs daily to clean up invitations older than 7 days from the database.
/// </summary>
public class InvitationsCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly VPNService _vpnService;
    private readonly Serilog.ILogger _logger = Log.ForContext<InvitationsCleanupService>();

    /// <summary>
    /// Initializes the cleanup service with the specified service provider.
    /// </summary>
    public InvitationsCleanupService(IServiceProvider serviceProvider, VPNService vpnService)
    {
        _serviceProvider = serviceProvider;
        _vpnService = vpnService;
    }

    /// <summary>
    /// Executes the cleanup task once every 24 hours or at a scheduled time.
    /// </summary>
    /// <param name="stoppingToken">Token used to cancel execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                DatabaseContext dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                DateTime threshold = DateTime.UtcNow.AddDays(-7);

                List<Invitation> oldInvitations = await dbContext.Invitations
                    .Where(i => i.CreatedAt < threshold)
                    .ToListAsync(stoppingToken);

                await ConcurrencyUtils.RunBoundedAsync(oldInvitations, 10, async invitation =>
                {
                    try
                    {
                        await _vpnService.DeleteUserWithNodes(invitation.Id.ToString());
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to delete VPN user for expired invitation {InvitationId}", invitation.Id);
                    }
                }, stoppingToken);

                if (oldInvitations.Count != 0)
                {
                    dbContext.Invitations.RemoveRange(oldInvitations);
                    await dbContext.SaveChangesAsync(stoppingToken);
                }
            }

            await WaitUntilUtils.WaitUntilTime(new TimeSpan(0, 0, 0), stoppingToken); // Next midnight
        }
    }
}
