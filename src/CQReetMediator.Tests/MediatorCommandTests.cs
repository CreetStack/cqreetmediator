using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.Tests;

public class MediatorCommandTests {
    [Fact]
    public async Task Mediator_Should_Execute_Command_Handler() {
        var handler = new AddCommandHandler();

        var registry = new HandlerRegistry();
        var pipelines = new PipelineExecutor([]);
        var publisher = new NotificationPublisher([]);
        var mediator = new Mediator(
            new TestServiceProvider(handler),
            registry,
            pipelines,
            publisher
        );

        string result = await mediator.SendAsync(new AddCommand("OK"));
        Assert.Equal("OK", result);
    }

    public sealed record AddCommand(string Message) : ICommand<string>;

    public sealed class AddCommandHandler : ICommandHandler<AddCommand, string> {
        public ValueTask<string> HandleAsync(AddCommand query, CancellationToken ct)
            => ValueTask.FromResult(query.Message);
    }

    private sealed class TestServiceProvider(AddCommandHandler handler) : IServiceProvider {
        public object? GetService(Type serviceType) {
            if (serviceType == typeof(ICommandHandler<AddCommand, string>))
                return handler;

            return null;
        }
    }
}