using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PokeFramework.Service;

public abstract class PokeBackgroundService(ILogger<PokeBackgroundService> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("{service} running at: {time}", this, DateTimeOffset.Now);
        }

        return Task.CompletedTask;
    }
}