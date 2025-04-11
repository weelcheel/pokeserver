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

        await Task.Delay(TimeSpan.FromMilliseconds(33), stoppingToken);
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

            logger.LogInformation($"Player {userId} added to instance with game user ID {gameUserId}.");
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

    public void UpdatePlayerMovement(string userId, TrainerMovement newMovement)
    {
        if (!_platformToGameUserIds.TryGetValue(userId, out var gameUserId))
        {
            throw new InvalidOperationException("Player not found in instance.");
        }

        if (!_players.TryGetValue(gameUserId, out var player))
        {
            throw new InvalidOperationException("Player not found in instance.");
        }

        player.UpdateMovement(newMovement);
    }

    public int GetPlayerCount()
    {
        return _players.Count;
    }

    private Command GenerateCommandFromGameState()
    {
        // first byte is the count of players
        // for each player in the instance
        // next 2 bytes are the X coordinate
        // next 2 bytes are the Y coordinate
        // next byte is the action
        // next byte is the current elevation
        // next byte is the facing direction
        // total bytes = 1 + (7 * playerCount)

        _playersLock.Wait();
        try
        {
            var commandParams = new byte[1 + (8 * _players.Count)];
            commandParams[0] = (byte)_players.Count;
            for (int i = 0; i < _players.Count; i++)
            {
                var playerPair = _players.ElementAt(i);
                commandParams[1 + ((i + 1) * 7)] = playerPair.Key;

                var xBytes = BitConverter.GetBytes(playerPair.Value.Movement.X);
                var yBytes = BitConverter.GetBytes(playerPair.Value.Movement.Y);

                commandParams[2 + (i * 7)] = xBytes[0];
                commandParams[3 + (i * 7)] = xBytes[1];
                commandParams[4 + (i * 7)] = yBytes[0];
                commandParams[5 + (i * 7)] = yBytes[1];
                commandParams[6 + (i * 7)] = playerPair.Value.Movement.Action;
                commandParams[7 + (i * 7)] = playerPair.Value.Movement.CurrentElevation;
                commandParams[8 + (i * 7)] = playerPair.Value.Movement.FacingDirection;
            }

            return new Command(CommandType.GameState, "", "", commandParams);
        }
        finally
        {
            _playersLock.Release();
        }
    }
}