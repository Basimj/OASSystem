using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;

namespace OAS.Infrastructure.Persistence;

public sealed class EfUnitOfWork(OasDbContext dbContext) : IUnitOfWork
{
    private const int UniqueConstraintViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyException(
                "The record was changed by another operation. Reload it and try again.",
                ex);
        }
        catch (DbUpdateException ex) when (TryMapUniqueConstraint(ex, out var conflict))
        {
            throw conflict;
        }
    }

    public async Task<TResult> ExecuteInTransactionAsync<TResult>(
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
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

    private static bool TryMapUniqueConstraint(DbUpdateException exception, out ConflictException conflict)
    {
        conflict = null!;
        if (exception.InnerException is not SqlException sqlException ||
            sqlException.Number is not (UniqueConstraintViolation or UniqueIndexViolation))
        {
            return false;
        }

        var message = sqlException.Message;
        if (message.Contains("IX_Users_NormalizedUserName", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("identity_username_exists", "User name already exists.");
            return true;
        }

        if (message.Contains("IX_Users_NormalizedEmail", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("identity_email_exists", "Email already exists.");
            return true;
        }

        if (message.Contains("IX_UserRoles_UserId_RoleId", StringComparison.OrdinalIgnoreCase))
        {
            conflict = new ConflictException("identity_user_role_exists", "The role is already assigned to the user.");
            return true;
        }

        conflict = new ConflictException("unique_constraint_conflict", "A unique value already exists.");
        return true;
    }
}
