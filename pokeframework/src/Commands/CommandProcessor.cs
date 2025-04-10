using System.Reflection;
using System.Text.Json;
using PokeFramework.Attributes;
using PokeFramework.Redis;
using PokeFramework.User;

namespace PokeFramework.Commands;

public abstract class CommandProcessor
{
    protected readonly RedisClient RedisClient;

    public CommandProcessor(RedisClient redisClient)
    {
        RedisClient = redisClient;
        _ = Initialize();
    }

    private async Task Initialize()
    {
        var methods = GetType().GetMethods();
        foreach (var method in methods)
        {
            var attributes = method.GetCustomAttributes(typeof(CommandHandlerAttribute), false);
            foreach (CommandHandlerAttribute attribute in attributes)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != 1)
                {
                    throw new TargetParameterCountException("Command handler must have exactly 1 parameter");
                }

                if (parameters[0].ParameterType != typeof(Command))
                {
                    throw new InvalidOperationException("Command handler must have a Command parameter.");
                }

                foreach (var commandType in attribute.CommandTypes)
                {
                    await RedisClient.SubscribeToChannelAsync($"command{commandType}", (channel, value) =>
                    {
                        var command = JsonSerializer.Deserialize<Command>(value.ToString());
                        if (command == null)
                        {
                            throw new InvalidOperationException("Command deserialization failed.");
                        }

                        if (attribute is CommandHandlerAuthenticatedAttribute)
                        {
                            var context = RedisClient.Get<UserContext>($"userContext-{command.ConnectionId}");
                            if (context == null)
                            {
                                throw new UnauthorizedAccessException("User has no valid context.");
                            }
                            if (context.UserId == null)
                            {
                                throw new UnauthorizedAccessException("User is not authenticated.");
                            }
                        }

                        var parametersArray = new object[] { command };
                        method.Invoke(this, parametersArray);
                    });
                }
            }
        }
    }
}