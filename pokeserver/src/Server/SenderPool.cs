using System.Collections.Concurrent;

namespace PokeServer.Server;

public class SenderPool(int maxNumberOfSenders = 128) : IDisposable
{
    private int _count;
    private readonly ConcurrentQueue<Sender> _senders = new();
    private bool _disposed = false;
    
    public Sender Rent()
    {
        if (!_senders.TryDequeue(out var sender))
        {
            return new Sender();
        }
        
        Interlocked.Decrement(ref _count);
        sender.Reset();
        return sender;
    }

    public void Return(Sender sender)
    {
        if (_disposed || _count >= maxNumberOfSenders)
        {
            sender.Dispose();
        }
        else
        {
            Interlocked.Increment(ref _count);
            _senders.Enqueue(sender);
        }
    }
    
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        while (_senders.TryDequeue(out var sender))
        {
            sender.Dispose();
        }
    }
}