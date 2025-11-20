namespace CQReetMediator.Abstractions;

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse> {
    ValueTask<TResponse> HandleAsync(TCommand command, CancellationToken ct);
}