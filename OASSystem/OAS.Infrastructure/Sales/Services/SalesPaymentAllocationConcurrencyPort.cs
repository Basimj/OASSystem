using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OAS.Application.Sales.Abstractions;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Sales.Services;

public sealed class SalesPaymentAllocationConcurrencyPort(OasDbContext db)
    : ISalesPaymentAllocationConcurrencyPort
{
    public Task LockInvoiceAsync(
        Guid salesInvoiceId,
        CancellationToken cancellationToken = default)
    {
        if (salesInvoiceId == Guid.Empty)
            throw new ArgumentException("Sales invoice id is required.", nameof(salesInvoiceId));
        return LockRowsAsync(null, salesInvoiceId, cancellationToken);
    }

    public Task LockReceiptLineAndInvoiceAsync(
        Guid receiptVoucherLineId,
        Guid salesInvoiceId,
        CancellationToken cancellationToken = default)
    {
        if (receiptVoucherLineId == Guid.Empty)
            throw new ArgumentException("Receipt voucher line id is required.", nameof(receiptVoucherLineId));
        if (salesInvoiceId == Guid.Empty)
            throw new ArgumentException("Sales invoice id is required.", nameof(salesInvoiceId));
        return LockRowsAsync(receiptVoucherLineId, salesInvoiceId, cancellationToken);
    }

    private async Task LockRowsAsync(
        Guid? receiptVoucherLineId,
        Guid salesInvoiceId,
        CancellationToken cancellationToken)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException(
                "Sales payment allocation requires an active database transaction.");

        await db.Database.OpenConnectionAsync(cancellationToken);

        if (receiptVoucherLineId.HasValue)
            await LockRowAsync(
                "tbl_ReceiptVoucherLines",
                receiptVoucherLineId.Value,
                cancellationToken);

        await LockRowAsync(
            "tbl_SalesInvoices",
            salesInvoiceId,
            cancellationToken);
    }

    private async Task LockRowAsync(
        string tableName,
        Guid id,
        CancellationToken cancellationToken)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
        command.CommandText =
            $"SELECT [Id] FROM [dbo].[{tableName}] WITH (UPDLOCK,HOLDLOCK) WHERE [Id] = @id";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@id";
        parameter.Value = id;
        command.Parameters.Add(parameter);
        await command.ExecuteScalarAsync(cancellationToken);
    }
}
