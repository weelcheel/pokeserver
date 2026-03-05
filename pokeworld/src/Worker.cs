using PokeFramework.Service;

namespace PokeWorld;

public class Worker(ILogger<Worker> logger, GameWorld gameWorld) : PokeBackgroundService(logger)
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
        }
        
        while (!stoppingToken.IsCancellationRequested)
        {
            await gameWorld.Tick(stoppingToken);
        }
    }
}