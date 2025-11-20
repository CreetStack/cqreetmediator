using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.Tests;

public class TestNotification : INotification {
}

public class TestHandlerA : INotificationHandler<TestNotification> {
    public bool Invoked { get; private set; }

    public Task HandleAsync(TestNotification notification, CancellationToken ct = default) {
        Invoked = true;
        return Task.CompletedTask;
    }
}

public class TestHandlerB : INotificationHandler<TestNotification> {
    public bool Invoked { get; private set; }

    public Task HandleAsync(TestNotification notification, CancellationToken ct = default) {
        Invoked = true;
        return Task.CompletedTask;
    }
}

public class FakeProvider : IServiceProvider {
    private readonly Dictionary<Type, object> _services = new();

    public void Register(Type type, object instance)
        => _services[type] = instance;

    public object? GetService(Type type)
        => _services.TryGetValue(type, out var obj)
            ? obj
            : null;
}

public class MultipleNotificationTests {
    [Fact]
    public async Task Mediator_Should_Invoke_All_Notification_Handlers() {
        var provider = new FakeProvider();

        var handlerA = new TestHandlerA();
        var handlerB = new TestHandlerB();

        // Register list of handlers for this notification type
        provider.Register(
            typeof(IEnumerable<INotificationHandler<TestNotification>>),
            new INotificationHandler<TestNotification>[] { handlerA, handlerB }
        );

        var registry = new HandlerRegistry();
        var pipelines = new PipelineExecutor([]);

        var publisher = new NotificationPublisher([]);

        var mediator = new Mediator(provider, registry, pipelines, publisher);

        await mediator.PublishAsync(new TestNotification());

        Assert.True(handlerA.Invoked, "Handler A should be invoked");
        Assert.True(handlerB.Invoked, "Handler B should be invoked");
    }
}