using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.Tests;

public class MediatorQueryTests {
    [Fact]
    public async Task Mediator_Should_Execute_Query_Handler() {
        var handler = new PingQueryHandler();

        var registry = new HandlerRegistry();
        var pipelines = new PipelineExecutor([]);
        var publisher = new NotificationPublisher([]);
        var mediator = new Mediator(
            new TestServiceProvider(handler),
            registry,
            pipelines,
            publisher
        );

        string result = await mediator.SendAsync(new PingQuery("OK"));
        Assert.Equal("OK", result);
    }

    public sealed record PingQuery(string Message) : IQuery<string>;

    public sealed class PingQueryHandler : IQueryHandler<PingQuery, string> {
        public ValueTask<string> HandleAsync(PingQuery query, CancellationToken ct)
            => ValueTask.FromResult(query.Message);
    }

    private sealed class TestServiceProvider(PingQueryHandler handler) : IServiceProvider {
        public object? GetService(Type serviceType) {
            if (serviceType == typeof(IQueryHandler<PingQuery, string>))
                return handler;

            return null;
        }
    }
}