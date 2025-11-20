using CQReetMediator.Abstractions;

namespace CQReetMediator;

public sealed class NotificationPublisher  {
    public NotificationPublisher(IEnumerable<Type> handlerTypes) { }

    public async Task PublishAsync(INotification notification, IServiceProvider provider, CancellationToken ct = default) {
        var notificationType = typeof(INotificationHandler<>).MakeGenericType(notification.GetType());
        var enumerableType = typeof(IEnumerable<>).MakeGenericType(notificationType);

        var handlers = provider.GetService(enumerableType) as IEnumerable<object>;
        if (handlers == null)
            return;

        foreach (var handler in handlers) {
            var method = handler.GetType().GetMethod("HandleAsync")!;
            var task = (Task)method.Invoke(handler, new object[] { notification, ct })!;
            await task;
        }
    }
}