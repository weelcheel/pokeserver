using System.Buffers;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace PokeServer.Server;

public class Sender : AwaitableEventArgs
{
    private short _token;
    private List<ArraySegment<byte>>? _buffers;

    public ValueTask<int> SendAsync(Socket socket, in ReadOnlyMemory<byte> data)
    {
        SetBuffer(MemoryMarshal.AsMemory(data));
        if (socket.SendAsync(this))
        {
            return new ValueTask<int>(this, _token++);
        }

        var transferred = BytesTransferred;
        var err = SocketError;
        return err == SocketError.Success
            ? new ValueTask<int>(transferred)
            : ValueTask.FromException<int>(new SocketException((int)err));
    }

    public ValueTask<int> SendAsync(Socket socket, in ReadOnlySequence<byte> data)
    {
        if (data.IsSingleSegment)
        {
            return SendAsync(socket, data.First);
        }
        _buffers ??= [];
        foreach (var buff in data)
        {
            if (!MemoryMarshal.TryGetArray(buff, out var array))
            {
                throw new InvalidOperationException("Buffer is not backed by an array.");
            }
            
            _buffers.Add(array);
        }

        BufferList = _buffers;

        if (socket.SendAsync(this))
        {
            return new ValueTask<int>(this, _token++);
        }
        
        var transferred = BytesTransferred;
        var err = SocketError;
        return err == SocketError.Success
            ? new ValueTask<int>(transferred)
            : ValueTask.FromException<int>(new SocketException((int)err));
    }

    public void Reset()
    {
        if (BufferList == null)
        {
            SetBuffer(null, 0, 0);
            return;
        }
        BufferList = null;
        _buffers?.Clear();
    }
}