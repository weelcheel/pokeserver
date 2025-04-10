using PokeServer.Game.Server;
using PokeServer.Server;

namespace PokeServer;

public class Worker(ILogger<Worker> logger, GameServer gameServer) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
            }
            
            var tcpServer = new TcpServer(gameServer.ProcessConnection);
            await tcpServer.Listen(stoppingToken);
        }
    }
}