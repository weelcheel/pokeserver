using PokeFramework.Attributes;
using PokeFramework.Commands;
using PokeFramework.Redis;
using PokeWorld.Trainer;

namespace PokeWorld.Processors;

public class GameWorldProcessor(RedisClient redisClient, GameWorld gameWorld) : CommandProcessor(redisClient)
{
    [CommandHandlerAuthenticated(CommandType.JoinMap)]
    public async Task ProcessPlayerJoinMap(Command command)
    {
        if (command.CommandParams is not { Length: 2 } || command.UserId == null)
        {
            throw new Exception("Invalid join map command parameters");
        }

        var mapId = BitConverter.ToUInt16(command.CommandParams, 0);
        await gameWorld.PlayerJoinedMap(mapId, command.UserId);
    }

    [CommandHandlerAuthenticated(CommandType.Move)]
    public async Task ProcessPlayerMove(Command command)
    {
        if (command.CommandParams is not { Length: 7 } || command.UserId == null)
        {
            throw new Exception("Invalid move command parameters");
        }

        var action = command.CommandParams[0];
        var x = BitConverter.ToInt16(command.CommandParams, 1);
        var y = BitConverter.ToInt16(command.CommandParams, 3);
        var currentElevation = command.CommandParams[5];
        var facingDirection = command.CommandParams[6];

        var trainerLocation = new TrainerLocation
        {
            X = x,
            Y = y,
            Action = action,
            CurrentElevation = currentElevation,
            FacingDirection = facingDirection
        };

        await gameWorld.PlayerMove(command.UserId, trainerLocation, action);
    }

    [CommandHandler(CommandType.Disconnect)]
    public async Task ProcessPlayerDisconnect(Command command)
    {
        if (command.UserId == null)
            return;

        await gameWorld.PlayerDisconnected(command.UserId);
    }
}