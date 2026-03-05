using PokeFramework.Commands;
using PokeFramework.Redis;
using PokeWorld.Trainer;

namespace PokeWorld.Instance;

public class GameInstance(RedisClient redisClient, ILogger logger)
{
    private readonly Dictionary<byte, TrainerAvatar> _players = [];
    private readonly Dictionary<string, byte> _platformToGameUserIds = [];
    private readonly SemaphoreSlim _playersLock = new(1, 1);

    public async Task Tick(CancellationToken stoppingToken)
    {
        var command = GenerateCommandFromGameState();
        await _playersLock.WaitAsync(stoppingToken);
        try
        {
            foreach (var player in _players.Select(pair => pair.Value))
            {
                // send the game state command to each player in the instance
                var connectionId = await RedisHelper.GetConnectionIdFromUserId(redisClient, player.UserId);
                await RedisHelper.SendMessageToConnectionAsync(redisClient, connectionId, command);
            }
        }
        finally
        {
            _playersLock.Release();
        }
    }

    public async Task AddPlayer(string userId)
    {
        await _playersLock.WaitAsync();
        try
        {
            if (_players.Any(pair => pair.Value.UserId == userId))
            {
                logger.LogInformation("Player already added to instance.");
                return;
            }

            byte gameUserId = 0;
            var foundGameUserId = false;
            for (byte i = 0; i < 255; i++)
            {
                if (_players.ContainsKey(i))
                {
                    continue;
                }

                gameUserId = i;
                foundGameUserId = true;
                break;
            }

            if (!foundGameUserId)
            {
                logger.LogError("No available game user IDs.");
                throw new InvalidOperationException("No available game user IDs.");
            }

            var newPlayer = new TrainerAvatar(userId, gameUserId);
            _players.Add(gameUserId, newPlayer);
            _platformToGameUserIds.Add(userId, gameUserId);

            var connectionId = await RedisHelper.GetConnectionIdFromUserId(redisClient, userId);
            if (connectionId == null)
            {
                logger.LogError($"Connection ID not found for user {userId}.");
                throw new InvalidOperationException($"Connection ID not found for user {userId}.");
            }
            
            var resultId = new byte[1];
            resultId[0] = gameUserId;
            await RedisHelper.SendMessageToConnectionAsync(redisClient, connectionId,
                new Command(CommandType.JoinMapResult, connectionId, userId, resultId));

            logger.LogInformation($"Player {userId} added to instance with game user ID {gameUserId}. There are {_players.Count} players in the instance.");
        }
        finally
        {
            _playersLock.Release();
        }
    }

    public void RemovePlayer(string userId)
    {
        _playersLock.Wait();
        try
        {
            if (!_platformToGameUserIds.Remove(userId, out var gameUserId))
            {
                logger.LogInformation("Player not found in instance.");
                return;
            }

            _players.Remove(gameUserId);
            logger.LogInformation($"Player {userId} removed from instance.");
        }
        finally
        {
            _playersLock.Release();
        }
    }

    public void UpdatePlayerLocation(string userId, TrainerLocation newLocation)
    {
        if (!_platformToGameUserIds.TryGetValue(userId, out var gameUserId))
            return;
        if (!_players.TryGetValue(gameUserId, out var player))
            return;
        player.Location = newLocation;
        player.HasPosition = true;
    }

    public async Task RelayMovement(string userId, byte movementAction)
    {
        if (!_platformToGameUserIds.TryGetValue(userId, out var senderGameUserId))
            return;

        var commandParams = new byte[] { senderGameUserId, movementAction };
        var command = new Command(CommandType.PlayerMovement, "", null, commandParams);

        await _playersLock.WaitAsync();
        try
        {
            foreach (var player in _players.Select(pair => pair.Value))
            {
                if (player.UserId == userId)
                    continue;
                var connectionId = await RedisHelper.GetConnectionIdFromUserId(redisClient, player.UserId);
                await RedisHelper.SendMessageToConnectionAsync(redisClient, connectionId, command);
            }
        }
        finally
        {
            _playersLock.Release();
        }
    }

    public int GetPlayerCount()
    {
        return _players.Count;
    }

    private Command GenerateCommandFromGameState()
    {
        _playersLock.Wait();
        try
        {
            var playersWithPosition = _players.Where(p => p.Value.HasPosition).ToList();
            var commandParams = new byte[1 + (48 * playersWithPosition.Count)];
            commandParams[0] = (byte)playersWithPosition.Count;
            for (int i = 0; i < playersWithPosition.Count; i++)
            {
                var playerPair = playersWithPosition[i];
                commandParams[1 + (i * 48)] = playerPair.Key;

                var xBytes = BitConverter.GetBytes(playerPair.Value.Location.X);
                var yBytes = BitConverter.GetBytes(playerPair.Value.Location.Y);

                commandParams[2 + (i * 48)] = xBytes[0];
                commandParams[3 + (i * 48)] = xBytes[1];
                commandParams[4 + (i * 48)] = yBytes[0];
                commandParams[5 + (i * 48)] = yBytes[1];
                commandParams[6 + (i * 48)] = playerPair.Value.Location.Action;
                commandParams[7 + (i * 48)] = playerPair.Value.Location.CurrentElevation;
                commandParams[8 + (i * 48)] = playerPair.Value.Location.FacingDirection;
            }

            return new Command(CommandType.GameState, "", "", commandParams);
        }
        finally
        {
            _playersLock.Release();
        }
    }
}