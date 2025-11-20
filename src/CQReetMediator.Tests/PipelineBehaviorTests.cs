using CQReetMediator.Abstractions;
using Xunit;

namespace CQReetMediator.Tests;

public class PipelineBehaviorTests {
    [Fact]
    public async Task Pipeline_Should_Run_Before_And_After_Handler() {
        var steps = new List<string>();

        var pipeline = new TestPipeline((req, next, ct) => {
            steps.Add("before");
            var result = next();
            steps.Add("after");
            return result;
        });

        var handler = new EchoQueryHandler();

        var registry = new HandlerRegistry();
        var executor = new PipelineExecutor(new[] { pipeline });
        var publisher = new NotificationPublisher([]);
        var provider = new SimpleProvider(new Dictionary<Type, object> {
            { typeof(IQueryHandler<EchoQuery, string>), handler }
        });

        var mediator = new Mediator(provider, registry, executor, publisher);

        string result = await mediator.SendAsync(new EchoQuery("hi"));

        Assert.Equal("hi", result);
        Assert.Collection(steps, 
            item => Assert.Equal("before", item),
            item => Assert.Equal("after", item)
        );
    }

    [Fact]
    public async Task Multiple_Pipelines_Should_Run_In_Order() {
        var order = new List<string>();

        var pipeline1 = new TestPipeline(async (req, next, ct) => {
            order.Add("1-before");
            var result = await next();
            order.Add("1-after");
            return result;
        });

        var pipeline2 = new TestPipeline(async (req, next, ct) => {
            order.Add("2-before");
            var result = await next();
            order.Add("2-after");
            return result;
        });

        var handler = new EchoQueryHandler();

        var registry = new HandlerRegistry();
        var executor = new PipelineExecutor(new[] { pipeline1, pipeline2 });
        var publisher = new NotificationPublisher([]);
        var provider = new SimpleProvider(new Dictionary<Type, object> {
            { typeof(IQueryHandler<EchoQuery, string>), handler }
        });

        var mediator = new Mediator(provider, registry, executor, publisher);

        await mediator.SendAsync(new EchoQuery("ok"));
        
        Assert.Collection(order,
            item => Assert.Equal("1-before", item),
            item => Assert.Equal("2-before", item),
            item => Assert.Equal("2-after", item),
            item => Assert.Equal("1-after", item)
        );
    }

    [Fact]
    public async Task Pipeline_Should_Modify_Result() {
        var pipeline = new TestPipeline(async (req, next, ct) => {
            var result = (string) (await next())!;
            return result.ToUpperInvariant();
        });

        var handler = new EchoQueryHandler();

        var registry = new HandlerRegistry();
        var executor = new PipelineExecutor(new[] { pipeline });
        var publisher = new NotificationPublisher([]);
        var provider = new SimpleProvider(new Dictionary<Type, object> {
            { typeof(IQueryHandler<EchoQuery, string>), handler }
        });

        var mediator = new Mediator(provider, registry, executor, publisher);

        string result = await mediator.SendAsync(new EchoQuery("hola"));

        Assert.Equal("HOLA", result);
    }

    [Fact]
    public async Task Pipeline_Should_ShortCircuit_Handler() {
        var pipeline = new TestPipeline((req, next, ct) => { return ValueTask.FromResult<object?>("intercepted"); });

        var handler = new EchoQueryHandler(); // never runs

        var registry = new HandlerRegistry();
        var executor = new PipelineExecutor(new[] { pipeline });
        var publisher = new NotificationPublisher([]);
        var provider = new SimpleProvider(new Dictionary<Type, object> {
            { typeof(IQueryHandler<EchoQuery, string>), handler }
        });

        var mediator = new Mediator(provider, registry, executor, publisher);

        string result = await mediator.SendAsync(new EchoQuery("ignored"));

        Assert.Equal("intercepted", result);
    }

    // ==== Helpers ====

    public sealed record EchoQuery(string Message) : IQuery<string>;

    public sealed class EchoQueryHandler : IQueryHandler<EchoQuery, string> {
        public ValueTask<string> HandleAsync(EchoQuery request, CancellationToken ct)
            => ValueTask.FromResult(request.Message);
    }

    public sealed class TestPipeline : IPipelineBehavior {
        private readonly PipelineDelegate _delegate;

        public TestPipeline(PipelineDelegate del) => _delegate = del;

        public ValueTask<object?> InvokeAsync(object request, Func<ValueTask<object?>> next, CancellationToken ct)
            => _delegate(request, next, ct);

        public delegate ValueTask<object?> PipelineDelegate(
            object request,
            Func<ValueTask<object?>> next,
            CancellationToken ct
        );
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