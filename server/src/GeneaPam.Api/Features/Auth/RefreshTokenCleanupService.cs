using Microsoft.Extensions.Options;

namespace GeneaPam.Api.Features.Auth;

internal sealed class RefreshTokenCleanupService(
    IServiceScopeFactory scopeFactory,
    IOptions<AuthOptions> options,
    ILogger<RefreshTokenCleanupService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(options.Value.CleanupIntervalHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(interval, stoppingToken);

                using var scope = scopeFactory.CreateScope();
                var job = scope.ServiceProvider.GetRequiredService<RefreshTokenCleanupJob>();
                await job.RunAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RefreshTokenCleanupService encountered an error.");
            }
        }
    }
}
