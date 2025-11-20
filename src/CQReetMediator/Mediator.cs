using CQReetMediator.Abstractions;

namespace CQReetMediator;

public sealed class Mediator : IMediator {
    private readonly IServiceProvider _provider;
    private readonly HandlerRegistry _registry;
    private readonly PipelineExecutor _pipelines;
    private readonly NotificationPublisher _publisher;

    public Mediator(
        IServiceProvider provider,
        HandlerRegistry registry,
        PipelineExecutor pipelines,
        NotificationPublisher publisher
    ) {
        _provider = provider;
        _registry = registry;
        _pipelines = pipelines;
        _publisher = publisher;
    }

    public ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default) {
        var requestType = request.GetType();

        var handlerDelegate = _registry.GetOrAddHandler(requestType, () => {
            var handlerGenericInterface = requestType
                .GetInterfaces()
                .First(i => i.IsGenericType && typeof(IRequest<>).IsAssignableFrom(i.GetGenericTypeDefinition()));

            var responseType = handlerGenericInterface.GetGenericArguments()[0];

            var handlerInterface =
                request is ICommand<TResponse>
                    ? typeof(ICommandHandler<,>).MakeGenericType(requestType, responseType)
                    : typeof(IQueryHandler<,>).MakeGenericType(requestType, responseType);

            var handler = _provider.GetService(handlerInterface) ??
                          throw new InvalidOperationException($"No handler registered for {requestType.Name}");

            var method = handlerInterface.GetMethod("HandleAsync") ??
                         throw new InvalidOperationException($"HandleAsync not found in {handlerInterface.Name}");

            return async (object req, CancellationToken ct) => {
                var result = method.Invoke(handler, new object[] { req, ct });

                if (result is ValueTask vtNonGeneric) {
                    await vtNonGeneric;
                    return null;
                }

                if (result is ValueTask<object?> vt)
                    return await vt;

                dynamic dyn = result!;
                return await dyn;
            };
        });

        return ExecuteInternalAsync<TResponse>(request, handlerDelegate, ct);
    }

    private async ValueTask<TResponse> ExecuteInternalAsync<TResponse>(
        object request,
        Func<object, CancellationToken, ValueTask<object?>> handlerDelegate,
        CancellationToken ct) {
        var result = await _pipelines.ExecuteAsync(
            request,
            () => handlerDelegate(request, ct),
            ct);

        return (TResponse)result!;
    }


    public Task PublishAsync(INotification notification, CancellationToken ct = default)
        => _publisher.PublishAsync(notification, _provider, ct);
}