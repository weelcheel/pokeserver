using System.Buffers;

using PokeServer.Delegates;
using PokeServer.Handlers;
using PokeServer.Server;

namespace PokeServer.Game;

public class GameServer
{
    private Dictionary<PacketCommandType, CommandHandler> CommandHandlers { get; } = new()
    {
        [PacketCommandType.Authenticate] = UnauthenticatedHandlers.HandleAuthenticate
    };

    public async Task ProcessConnection(Connection connection)
    {
        Console.WriteLine("[GAME SERVER]: Connection received!");
        connection.Start();
        try
        {
            while (true)
            {
                var result = await connection.Input.ReadAsync();
                if (result.IsCanceled)
                {
                    break;
                }

                if (result.Buffer.IsEmpty)
                {
                    continue;
                }

                var buff = result.Buffer;
                ProcessIncomingPacket(connection, buff);
                connection.Input.AdvanceTo(buff.End, buff.End);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            connection.Shutdown();
            await connection.DisposeAsync();
            Console.WriteLine("[GAME SERVER]: Connection closed!");   
        }
    }

    private void ProcessIncomingPacket(Connection connection, ReadOnlySequence<byte> packetBytes)
    {   
        if (packetBytes.Length < 7)
        {
            return;
        }
        
        // first four bytes should be the PacketMagic 32 bit unsigned integer
        var magic = BitConverter.ToUInt32(packetBytes.Slice(0, 4).ToArray());
        if (magic == Constants.PacketMagic)
        {
            // next four bytes should be the data length as a 16 bit unsigned integer
            var dataLength = BitConverter.ToUInt16(packetBytes.Slice(4, 2).ToArray());
            if (dataLength == packetBytes.Length - 6)
            {
                // this is a valid packet
                // the next bytes to the end of the sequence should be an array of commands
                var commandBytes = packetBytes.Slice(6, dataLength).ToArray();
                
                // first byte is the number of commands in the array
                var commandCount = commandBytes[0];
                
                // a command has its first byte as the command type
                // the second byte is the number of bytes in the command's data params
                // the rest of the bytes of the command are the data params
                var bytesRead = 0;
                for (var i = 0; i < commandCount; i++)
                {
                    var commandType = commandBytes[1 + bytesRead];
                    var commandParamsSize = commandBytes[1 + bytesRead + 1];
                    var commandParams = new byte[commandParamsSize];
                    Array.Copy(commandBytes, 3 + bytesRead, commandParams, 0, commandParamsSize);

                    var command = new Command((PacketCommandType)commandType, commandParamsSize, commandParams);
                    if (CommandHandlers.TryGetValue(command.CommandType, out var handler))
                    {
                        handler(this, connection, command);
                    }
                    
                    bytesRead += 2 + commandParamsSize;
                }
            }
        }
    }
}