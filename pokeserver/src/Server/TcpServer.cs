using System.Net;
using System.Net.Sockets;

namespace PokeServer.Server;

public class TcpServer(Delegates.ProcessConnection processConnection)
{
    public static readonly string ServerId = Guid.NewGuid().ToString();
    
    public async Task Listen(CancellationToken cancellationToken)
    {
        var senderPool = new SenderPool(1024);
        var listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(new IPEndPoint(IPAddress.Any, 6969));
        listenSocket.Listen(128);

        while (true)
        {
            var socket = await listenSocket.AcceptAsync(cancellationToken);
            var connection = new Connection(socket, senderPool);
            _ = processConnection(connection);

            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}