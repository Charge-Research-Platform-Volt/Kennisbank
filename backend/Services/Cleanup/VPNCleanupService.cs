using KnowledgeBank.Data;
using Microsoft.EntityFrameworkCore;
using KnowledgeBank.Utils;
using KnowledgeBank.Services;
using Serilog;

/// <summary>
/// A background service that runs daily to clean up orphaned VPN users
/// whose names don't correspond to any user ID or invitation ID in the database.
/// </summary>
public class VPNCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly VPNService _vpnService;
    private readonly Serilog.ILogger _logger = Log.ForContext<VPNCleanupService>();

    /// <summary>
    /// Initializes the cleanup service with the specified service provider.
    /// </summary>
    public VPNCleanupService(IServiceProvider serviceProvider, VPNService vpnService)
    {
        _serviceProvider = serviceProvider;
        _vpnService = vpnService;
    }

    /// <summary>
    /// Executes the cleanup task once every 24 hours at midnight.
    /// </summary>
    /// <param name="stoppingToken">Token used to cancel execution.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await WaitUntilUtils.WaitUntilTime(new TimeSpan(1, 0, 0), stoppingToken); // Run at 1 AM

            try
            {
                var vpnUsers = await _vpnService.GetAllUsers();

                using IServiceScope scope = _serviceProvider.CreateScope();
                DatabaseContext dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

                HashSet<string> userIds = (await dbContext.Users.Select(u => u.Id).ToListAsync(stoppingToken)).ToHashSet();
                HashSet<string> invitationIds = (await dbContext.Invitations.Select(i => i.Id.ToString()).ToListAsync(stoppingToken)).ToHashSet();

                foreach (var (id, name) in vpnUsers)
                {
                    if (!ValidityUtil.IsValidId(name) || userIds.Contains(name) || invitationIds.Contains(name))
                        continue;

                    try
                    {
                        string[] nodeIds = await _vpnService.GetUserNodes(name);
                        await _vpnService.DeleteNodes(nodeIds);
                        await _vpnService.DeleteUser(id);
                        _logger.Information("Deleted orphaned VPN user {VPNUserName}", name);
                    }
                    catch (Exception e)
                    {
                        _logger.Error(e, "Failed to delete orphaned VPN user {VPNUserName}", name);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error(e, "VPN cleanup service encountered an error");
            }
        }
    }
}
