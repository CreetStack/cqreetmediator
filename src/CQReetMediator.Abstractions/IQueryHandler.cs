namespace CQReetMediator.Abstractions;

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse> {
    ValueTask<TResponse> HandleAsync(TQuery query, CancellationToken ct);
}