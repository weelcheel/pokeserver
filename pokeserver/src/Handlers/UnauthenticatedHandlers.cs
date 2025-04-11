using PokeFramework.Attributes;
using PokeFramework.Commands;
using PokeFramework.Redis;
using PokeFramework.User;

namespace PokeServer.Handlers;

public class UnauthenticatedHandlers(RedisClient redisClient) : CommandProcessor(redisClient)
{
    [CommandHandler(CommandType.Authenticate)]
    public async Task HandleAuthenticate(Command command)
    {
        // eventually, this should be real authentication
        var context = await RedisClient.GetAsync<UserContext>($"userContext-{command.ConnectionId}");
        if (context == null)
        {
            var failBytes = BitConverter.GetBytes(Constants.Failure);
            var failCommand = new Command(CommandType.AuthenticateResult, command.ConnectionId, command.UserId,
                failBytes);
            await RedisHelper.SendMessageToConnectionAsync(RedisClient, command.ConnectionId, failCommand);
            return;
        }

        context.UserId = Guid.NewGuid().ToString();
        await RedisClient.SetAsync($"userContext-{command.ConnectionId}", context, TimeSpan.FromHours(2));
        await RedisClient.SetRawAsync($"connectionIdFromUser-{context.UserId}", command.ConnectionId,
            TimeSpan.FromHours(2));

        // send the success number as the authentication result
        var successBytes = BitConverter.GetBytes(Constants.Success);
        var successCommand = new Command(CommandType.AuthenticateResult, command.ConnectionId, command.UserId,
            successBytes);
        await RedisHelper.SendMessageToConnectionAsync(RedisClient, command.ConnectionId, successCommand);
    }
}