using System.Collections.Concurrent;

namespace CQReetMediator;

public sealed class HandlerRegistry {
    private readonly ConcurrentDictionary<Type, Delegate> _handlers = new();

    public Func<object, CancellationToken, ValueTask<object?>> GetOrAddHandler(
        Type requestType,
        Func<Func<object, CancellationToken, ValueTask<object?>>> factory) {
        return (Func<object, CancellationToken, ValueTask<object?>>)
            _handlers.GetOrAdd(requestType, _ => factory());
    }
}