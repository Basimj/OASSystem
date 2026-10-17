using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OAS.Application.Purchasing.Abstractions;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Purchasing.Services;

public sealed class PurchasePaymentAllocationConcurrencyPort(OasDbContext db)
    : IPurchasePaymentAllocationConcurrencyPort
{
    public Task LockInvoiceAsync(
        Guid purchaseInvoiceId,
        CancellationToken cancellationToken = default)
    {
        if (purchaseInvoiceId == Guid.Empty)
            throw new ArgumentException("Purchase invoice id is required.", nameof(purchaseInvoiceId));

        return LockRowsAsync(null, purchaseInvoiceId, cancellationToken);
    }

    public Task LockPaymentLineAndInvoiceAsync(
        Guid paymentVoucherLineId,
        Guid purchaseInvoiceId,
        CancellationToken cancellationToken = default)
    {
        if (paymentVoucherLineId == Guid.Empty)
            throw new ArgumentException("Payment voucher line id is required.", nameof(paymentVoucherLineId));
        if (purchaseInvoiceId == Guid.Empty)
            throw new ArgumentException("Purchase invoice id is required.", nameof(purchaseInvoiceId));

        return LockRowsAsync(paymentVoucherLineId, purchaseInvoiceId, cancellationToken);
    }

    private async Task LockRowsAsync(
        Guid? paymentVoucherLineId,
        Guid purchaseInvoiceId,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Purchase payment allocation requires an active database transaction.");

        await db.Database.OpenConnectionAsync(cancellationToken);

        // Deterministic order: source row first, then target row. Quick-pay has no
        // persisted source line yet, so it locks only the target invoice before reading
        // the outstanding balance.
        if (paymentVoucherLineId.HasValue)
            await LockRowAsync("tbl_PaymentVoucherLines", paymentVoucherLineId.Value, cancellationToken);

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
