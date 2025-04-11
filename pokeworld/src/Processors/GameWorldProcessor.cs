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
    public Task ProcessPlayerMove(Command command)
    {
        if (command.CommandParams is not { Length: 7 } || command.UserId == null)
        {
            throw new Exception("Invalid move command parameters");
        }
        
        // first 2 bytes are the X coordinate
        var x = BitConverter.ToUInt16(command.CommandParams, 0);
        var y = BitConverter.ToUInt16(command.CommandParams, 2);
        var action = command.CommandParams[4];
        var currentElevation = command.CommandParams[5];
        var facingDirection = command.CommandParams[6];
        var trainerMovement = new TrainerMovement
        {
            X = x,
            Y = y,
            Action = action,
            CurrentElevation = currentElevation,
            FacingDirection = facingDirection
        };
        
        gameWorld.PlayerMove(trainerMovement,command.UserId);
        return Task.CompletedTask;
    }
}