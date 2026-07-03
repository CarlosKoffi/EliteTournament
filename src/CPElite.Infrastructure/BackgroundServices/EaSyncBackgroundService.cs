using CPElite.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPElite.Infrastructure.BackgroundServices;

public sealed class EaSyncBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EaSyncBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;

    public EaSyncBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<EaSyncBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enabled = configuration.GetValue("EaSync:Enabled", true);
        _interval = TimeSpan.FromMinutes(configuration.GetValue("EaSync:IntervalMinutes", 120));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("EA sync background service is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(_interval);
        await SyncOnceAsync(stoppingToken);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await SyncOnceAsync(stoppingToken);
        }
    }

    private async Task SyncOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<EaSyncService>();
            var count = await service.SyncAllLinkedTeamsAsync(cancellationToken);
            _logger.LogInformation("EA sync completed for {TeamCount} linked teams.", count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EA sync background job failed.");
        }
    }
}
