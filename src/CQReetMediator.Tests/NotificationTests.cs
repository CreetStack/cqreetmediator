using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.Tests;

public class NotificationTests {
    [Fact]
    public async Task Mediator_Should_Invoke_Notification_Handler() {
        var handler = new TestNotificationHandler();

        var registry = new HandlerRegistry();
        var pipelines = new PipelineExecutor([]);

        var publisher = new NotificationPublisher(
            new[] { typeof(INotificationHandler<UserCreated>) }
        );

        var provider = new SimpleProvider(new Dictionary<Type, object> {
            { typeof(IEnumerable<INotificationHandler<UserCreated>>), new object[] { handler } }
        });

        var mediator = new Mediator(provider, registry, pipelines, publisher);

        await mediator.PublishAsync(new UserCreated("John"));

        Assert.True(handler.Called);
        Assert.Equal("John", handler.ReceivedName);
    }


    public sealed record UserCreated(string Name) : INotification;

    public sealed class TestNotificationHandler : INotificationHandler<UserCreated> {
        public bool Called { get; private set; }
        public string? ReceivedName { get; private set; }

        public Task HandleAsync(UserCreated notification, CancellationToken ct) {
            Called = true;
            ReceivedName = notification.Name;
            return Task.CompletedTask;
        }
    }

    private sealed class SimpleProvider : IServiceProvider {
        private readonly Dictionary<Type, object> _map;

        public SimpleProvider(Dictionary<Type, object> map) => _map = map;

        public object? GetService(Type type)
            => _map.TryGetValue(type, out var impl)
                ? impl
                : null;
    }
}