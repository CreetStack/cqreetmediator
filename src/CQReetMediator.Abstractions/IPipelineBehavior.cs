namespace CQReetMediator.Abstractions;

public interface IPipelineBehavior {
    ValueTask<object?> InvokeAsync(object request, Func<ValueTask<object?>> next, CancellationToken ct);
}