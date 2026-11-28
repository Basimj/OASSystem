namespace OAS.Application.Abstractions.Persistence;

public interface IUnitOfWork
{
    Task<TResult> ExecuteSerializableAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Serializable transactions are required for opening balances.");
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default);
}
