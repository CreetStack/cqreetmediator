namespace CQReetMediator.Abstractions;

public interface IMediator {
    ValueTask<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken ct = default);

    Task PublishAsync(INotification notification, CancellationToken ct = default);
}