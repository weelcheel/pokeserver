using System.Text.Json;
using PokeFramework.User;

namespace PokeFramework.Redis;

public static class RedisHelper
{
    public static async Task SendMessageToConnectionAsync(RedisClient redisClient, string connectionId,
        object message)
    {
        await redisClient.PublishMessageAsync($"connection-{connectionId}", JsonSerializer.Serialize(message));
    }

    public static async Task<string> GetConnectionIdFromUserId(RedisClient redisClient, string userId)
    {
        var connectionId = await redisClient.GetRawAsync($"connectionIdFromUser-{userId}");
        if (connectionId == null)
        {
            throw new InvalidOperationException($"Connection id not found for userId: {userId}");
        }
        return connectionId;
    }

    public static async Task<string?> GetUserIdFromConnectionId(RedisClient redisClient, string connectionId)
    {
        var userContext = await redisClient.GetAsync<UserContext>($"userContext-{connectionId}");
        return userContext?.UserId;
    }
}