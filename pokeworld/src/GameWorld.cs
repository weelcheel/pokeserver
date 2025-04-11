using PokeFramework.Redis;
using PokeWorld.Instance;
using PokeWorld.Trainer;

namespace PokeWorld;

public class GameWorld(ILogger<GameWorld> logger, RedisClient redisClient)
{
    private readonly Dictionary<ushort, List<GameInstance>> _instances = new();
    private readonly Dictionary<string, GameInstance> _platformUserInstanceMap = new();
    private readonly SemaphoreSlim _instancesLock = new(1, 1);

    public async Task Tick(CancellationToken stoppingToken)
    {
        await _instancesLock.WaitAsync(stoppingToken);
        try
        {
            foreach (var instance in _instances.Select(pair => pair.Value).SelectMany(instances => instances))
            {
                await instance.Tick(stoppingToken);
            }
        }
        finally
        {
            _instancesLock.Release();
        }

        await Task.Delay(TimeSpan.FromMilliseconds(33), stoppingToken);
    }

    private void PlayerLeftMap(string userId)
    {
        if (!_platformUserInstanceMap.TryGetValue(userId, out var instance))
        {
            return;
        }

        instance.RemovePlayer(userId);
    }

    public async Task PlayerJoinedMap(ushort mapId, string userId)
    {
        await _instancesLock.WaitAsync();
        try
        {
            PlayerLeftMap(userId);

            if (!_instances.TryGetValue(mapId, out var instances))
            {
                instances = [];
            }

            GameInstance? gameInstance = null;
            if (instances.Count == 0)
            {
                gameInstance = new GameInstance(redisClient, logger);
                instances.Add(gameInstance);
            }
            else
            {
                foreach (var instance in instances.Where(instance => instance.GetPlayerCount() + 1 <= 64))
                {
                    gameInstance = instance;
                    break;
                }

                if (gameInstance == null)
                {
                    gameInstance = new GameInstance(redisClient, logger);
                    instances.Add(gameInstance);
                }
            }

            await gameInstance.AddPlayer(userId);
            _platformUserInstanceMap[userId] = gameInstance;
            _instances[mapId] = instances;
        }
        finally
        {
            _instancesLock.Release();
        }
    }

    public void PlayerMove(TrainerMovement newMovement, string userId)
    {
        if (!_platformUserInstanceMap.TryGetValue(userId, out var instance))
        {
            return;
        }

        instance.UpdatePlayerMovement(userId, newMovement);
    }
}