using System.Net.Sockets;
using System.Threading.Tasks.Sources;

namespace PokeServer.Server;

public class AwaitableEventArgs()
    : SocketAsyncEventArgs(unsafeSuppressExecutionContextFlow: true), IValueTaskSource<int>
{
    private ManualResetValueTaskSourceCore<int> _source;
    
    public int GetResult(short token)
    {
        var result = _source.GetResult(token);
        _source.Reset();
        return result;
    }

    public ValueTaskSourceStatus GetStatus(short token)
    {
        return _source.GetStatus(token);
    }

    public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
    {
        _source.OnCompleted(continuation, state, token, flags);
    }

    protected override void OnCompleted(SocketAsyncEventArgs e)
    {
        if (SocketError != SocketError.Success)
        {
            _source.SetException(new SocketException((int)SocketError));
        }

        _source.SetResult(BytesTransferred);
    }
}