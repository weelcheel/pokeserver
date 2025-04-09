using System.Buffers;
using PokeServer.Game;
using PokeServer.Server;

namespace PokeServer;

public static class Utility
{
    public static async Task Send(Connection connection, List<Command> commands)
    {
        if (commands.Count == 0)
        {
            return;
        }
        
        // calculate the size of each command
        var commandDataSize = commands.Sum(command => 1 + 1 + command.CommandParams.Length);

        var commandData = new List<byte>(commandDataSize);
        foreach (var command in commands)
        {
            commandData.Add((byte)command.CommandType);
            commandData.Add((byte)command.CommandParams.Length);
            commandData.AddRange(command.CommandParams);
        }
        
        byte[] packet = new byte[7 + commandDataSize];
        
        // first four bytes is the 32 bit unsigned integer magic number from Constants.PacketMagic
        var magic = BitConverter.GetBytes(Constants.PacketMagic);
        Array.Copy(magic, 0, packet, 0, 4);
        
        // next two bytes is the 16 bit unsigned integer of length of the command data + the number of commands
        var dataLength = BitConverter.GetBytes((ushort)commandDataSize + 1);
        Array.Copy(dataLength, 0, packet, 4, 2);
        
        // next byte is the number of commands
        packet[6] = (byte)commands.Count;
        
        // next bytes are the command data
        Array.Copy(commandData.ToArray(), 0, packet, 7, commandDataSize);
        
        // packet is constructed, send it
        await connection.Output.WriteAsync(packet);
        await connection.Output.FlushAsync();
    }
}