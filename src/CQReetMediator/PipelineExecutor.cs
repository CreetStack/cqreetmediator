using CQReetMediator.Abstractions;

namespace CQReetMediator;

public sealed class PipelineExecutor(IEnumerable<IPipelineBehavior> behaviors) {

    public ValueTask<object?> ExecuteAsync(object request, Func<ValueTask<object?>> handler, CancellationToken ct) {
        var next = handler;

        foreach (var behavior in behaviors.Reverse()) {
            var current = next;
            next = () => behavior.InvokeAsync(request, current, ct);
        }

        return next();
    }
}