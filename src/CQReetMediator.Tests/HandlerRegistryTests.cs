using Xunit;

namespace CQReetMediator.Tests;

public class HandlerRegistryTests {
    [Fact]
    public void Registry_Should_Add_Handler_To_Cache() {

        var registry = new HandlerRegistry();
        int factoryCalls = 0;

        Func<Func<object, CancellationToken, ValueTask<object?>>> factory = () => {
            factoryCalls++;
            return (o, ct) => ValueTask.FromResult<object?>(123);
        };

        var h1 = registry.GetOrAddHandler(typeof(string), factory);
        var h2 = registry.GetOrAddHandler(typeof(string), factory);

        Assert.Same(h1, h2);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public void Registry_Should_Return_Different_Handlers_For_Different_Types() {
        var registry = new HandlerRegistry();

        var factoryA = new Func<Func<object, CancellationToken, ValueTask<object?>>>(() =>
            (o, ct) => ValueTask.FromResult<object?>(1));

        var factoryB = new Func<Func<object, CancellationToken, ValueTask<object?>>>(() =>
            (o, ct) => ValueTask.FromResult<object?>(2));

        var h1 = registry.GetOrAddHandler(typeof(int), factoryA);
        var h2 = registry.GetOrAddHandler(typeof(string), factoryB);

        Assert.NotSame(h1, h2);
    }

    [Fact]
    public async Task Registry_Should_Be_Thread_Safe() {
        // Arrange
        var registry = new HandlerRegistry();
        int factoryCalls = 0;

        Func<Func<object, CancellationToken, ValueTask<object?>>> factory = () => {
            Interlocked.Increment(ref factoryCalls);
            return (o, ct) => ValueTask.FromResult<object?>(999);
        };

        var tasks = Enumerable.Range(0, 50)
            .Select(_ => Task.Run(() =>
                registry.GetOrAddHandler(typeof(Guid), factory)))
            .ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(1, Volatile.Read(ref factoryCalls));
    }
}