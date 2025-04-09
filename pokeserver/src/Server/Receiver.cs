using System.Net.Sockets;

namespace PokeServer.Server;

public class Receiver : AwaitableEventArgs
{
    private short _token;
    
    public ValueTask<int> ReceiveAsync(Socket socket, Memory<byte> memory)
    {
        SetBuffer(memory);
        if (socket.ReceiveAsync(this))
        {
            return new ValueTask<int>(this, _token++);
        }

        var transferred = BytesTransferred;
        var err = SocketError;
        return err == SocketError.Success
            ? new ValueTask<int>(transferred)
            : ValueTask.FromException<int>(new SocketException((int)err));
    }
}