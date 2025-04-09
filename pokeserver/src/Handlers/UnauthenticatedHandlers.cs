using System.Buffers.Binary;
using PokeServer.Game;
using PokeServer.Server;

namespace PokeServer.Handlers;

public static class UnauthenticatedHandlers
{
    public static async Task HandleAuthenticate(GameServer gameServer, Connection connection, Command command)
    {
        // eventually, this should be real authentication
        var userId = (byte)42;
        connection.UserId = userId.ToString();
        
        // send the success number as the authentication result
        var successBytes = BitConverter.GetBytes(Constants.Success);
        var successCommand = new Command(PacketCommandType.AuthenticateResult, 4, successBytes);
        
        await Utility.Send(connection, [successCommand]);
    }
}