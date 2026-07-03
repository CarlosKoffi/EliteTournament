using CPElite.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CPElite.Infrastructure.BackgroundServices;

public sealed class EaMatchVerificationBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EaMatchVerificationBackgroundService> _logger;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;

    public EaMatchVerificationBackgroundService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<EaMatchVerificationBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enabled = configuration.GetValue("EaMatchVerification:Enabled", true);
        _interval = TimeSpan.FromMinutes(configuration.GetValue("EaMatchVerification:IntervalMinutes", 2));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            _logger.LogInformation("EA match verification background service is disabled.");
            return;
        }

        using var timer = new PeriodicTimer(_interval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await VerifyDueMatchesAsync(stoppingToken);
        }
    }

    private async Task VerifyDueMatchesAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<TournamentService>();
            var count = await service.VerifyDueEaMatchesAsync(25, cancellationToken);
            if (count > 0)
            {
                _logger.LogInformation("EA match verification checked {MatchCount} due match(es).", count);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EA match verification background job failed.");
        }
    }
}
