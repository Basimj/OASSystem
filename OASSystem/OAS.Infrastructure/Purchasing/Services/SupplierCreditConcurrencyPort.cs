using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OAS.Application.Purchasing.Abstractions;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class SupplierCreditConcurrencyPort(OasDbContext db) : ISupplierCreditConcurrencyPort
{
    public async Task LockAsync(
        Guid purchaseReturnId,
        Guid purchaseInvoiceId,
        CancellationToken cancellationToken = default)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Supplier credit settlement requires an active database transaction.");

        await db.Database.OpenConnectionAsync(cancellationToken);

        // Always lock source then target to keep a deterministic lock order.
        await LockRowAsync("tbl_PurchaseReturns", purchaseReturnId, cancellationToken);
        await LockRowAsync("tbl_PurchaseInvoices", purchaseInvoiceId, cancellationToken);
    }

    private async Task LockRowAsync(string tableName, Guid id, CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText = $"SELECT [Id] FROM [dbo].[{tableName}] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = id;
        command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
