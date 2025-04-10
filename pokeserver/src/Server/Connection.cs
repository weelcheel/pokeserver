using System.IO.Pipelines;
using System.Net.Sockets;

namespace PokeServer.Server;

public class Connection : IAsyncDisposable
{
    private const int MinBuffSize = 1024;
    private readonly Socket _socket;
    private readonly Receiver _receiver;
    private Sender? _sender;
    private readonly SenderPool _senderPool;
    private Task? _receiveTask;
    private Task? _sendTask;
    private readonly Pipe _transportPipe;
    private readonly Pipe _applicationPipe;
    private readonly Lock _shutdownLock = new ();
    private volatile bool _socketDisposed;
    public PipeWriter Output { get;}
    public PipeReader Input { get;}
    
    public string ConnectionId { get; } = Guid.NewGuid().ToString();

    public Connection(Socket socket, SenderPool senderPool)
    {
        _socket = socket;
        _receiver = new Receiver();
        _senderPool = senderPool;
        _transportPipe = new Pipe();
        Output = _transportPipe.Writer;
        _applicationPipe = new Pipe();
        Input = _applicationPipe.Reader;
    }

    public void Start()
    {
        try
        {
            _sendTask = SendLoop();
            _receiveTask = ReceiveLoop();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private async Task SendLoop()
    {
        try
        {
            while (true)
            {
                var result = await _transportPipe.Reader.ReadAsync();
                if (result.IsCanceled)
                {
                    break;
                }
                var buff = result.Buffer;
                if (!buff.IsEmpty)
                {
                    _sender = _senderPool.Rent();
                    await _sender.SendAsync(_socket, result.Buffer);
                    _senderPool.Return(_sender);
                    _sender = null;
                }
                _transportPipe.Reader.AdvanceTo(buff.End);
                if (result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await _applicationPipe.Writer.CompleteAsync();
            Shutdown();
        }
    }

    private async Task ReceiveLoop()
    {
        try
        {
            while (true)
            {
                var buff = _applicationPipe.Writer.GetMemory(MinBuffSize);
                var bytes = await _receiver.ReceiveAsync(_socket, buff);
                if (bytes == 0)
                {
                    continue;
                }
                _applicationPipe.Writer.Advance(bytes);
                var result = await _applicationPipe.Writer.FlushAsync();
                if (result.IsCanceled || result.IsCompleted)
                {
                    break;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await _applicationPipe.Writer.CompleteAsync();
            Shutdown();
        }
    }
    
    public async ValueTask DisposeAsync()
    {
        await _transportPipe.Reader.CompleteAsync();
        await _applicationPipe.Writer.CompleteAsync();
        try
        {
            if (_receiveTask != null)
            {
                await _receiveTask;
            }

            if (_sendTask != null)
            {
                await _sendTask;
            }
        }
        finally
        {
            _receiver.Dispose();
            _sender?.Dispose();
        }
    }
    
    public void Shutdown()
    {
        lock (_shutdownLock)
        {
            if (_socketDisposed)
            {
                return;
            }
            _socketDisposed = true;
            try
            {
                _socket.Shutdown(SocketShutdown.Both);
            }
            finally
            {
                _socket.Dispose();
            }
        }
    }
}