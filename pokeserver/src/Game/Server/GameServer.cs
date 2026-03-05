using System.Buffers;
using System.Text.Json;
using PokeFramework.Commands;
using PokeFramework.Redis;
using PokeFramework.User;
using PokeServer.Server;

namespace PokeServer.Game.Server;

public class GameServer(RedisClient redisClient, ILogger<GameServer> logger)
{
    public async Task ProcessConnection(Connection connection)
    {
        var context = new UserContext(connection.ConnectionId);
        await redisClient.SetAsync($"userContext-{connection.ConnectionId}", context, TimeSpan.FromHours(2));
        await redisClient.SubscribeToChannelAsync($"connection-{connection.ConnectionId}", (channel, value) =>
        {
            var command = JsonSerializer.Deserialize<Command>(value.ToString());
            if (command == null)
            {
                throw new InvalidOperationException("Command deserialization failed.");
            }
            _ = Utility.Send(connection, [command]);
        });
        
        logger.LogInformation("Connection received, context created, and redis subscription created!");
        
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
                await ProcessIncomingPacket(connection, buff);
                connection.Input.AdvanceTo(buff.End, buff.End);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error processing connection");
            throw;
        }
        finally
        {
            var userId = await RedisHelper.GetUserIdFromConnectionId(redisClient, connection.ConnectionId);
            if (userId != null)
            {
                var disconnectCommand = new Command(CommandType.Disconnect, connection.ConnectionId, userId);
                await redisClient.PublishMessageAsync($"command{CommandType.Disconnect}",
                    System.Text.Json.JsonSerializer.Serialize(disconnectCommand));
            }
            await redisClient.DeleteAsync($"userContext-{connection.ConnectionId}");
            connection.Shutdown();
            await connection.DisposeAsync();
            logger.LogInformation("Connection closed!");
        }
    }

    private async Task ProcessIncomingPacket(Connection connection, ReadOnlySequence<byte> packetBytes)
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

                    var userId = await RedisHelper.GetUserIdFromConnectionId(redisClient, connection.ConnectionId);
                    var command = new Command((CommandType)commandType, connection.ConnectionId, userId, commandParams);
                    await redisClient.PublishMessageAsync($"command{command.CommandType}",
                        JsonSerializer.Serialize(command));

                    bytesRead += 2 + commandParamsSize;
                }
            }
        }
    }
}