using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;

namespace OAS.Infrastructure.Persistence;

public sealed class EfUnitOfWork(OasDbContext dbContext) : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try { return await dbContext.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException ex) { throw new ConcurrencyException("The record was changed by another operation. Reload it and try again.", ex); }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);
        if (dbContext.Database.CurrentTransaction is not null)
        {
            var nestedResult = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            return nestedResult;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var result = await operation(cancellationToken);
            await SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
