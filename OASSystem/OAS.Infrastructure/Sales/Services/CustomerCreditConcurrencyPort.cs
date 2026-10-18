using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OAS.Application.Sales.Abstractions;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Sales.Services;

public sealed class CustomerCreditConcurrencyPort(OasDbContext db) : ICustomerCreditConcurrencyPort
{
    public async Task LockAsync(Guid salesReturnId, Guid? targetSalesInvoiceId = null, CancellationToken ct = default)
    {
        if (salesReturnId == Guid.Empty) throw new ArgumentException("Return id required.",nameof(salesReturnId));
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Customer credit settlements require an ambient database transaction.");
        await db.Database.OpenConnectionAsync(ct);
        await LockRowAsync("tbl_SalesReturns",salesReturnId,ct);
        if (targetSalesInvoiceId.HasValue)
            await LockRowAsync("tbl_SalesInvoices",targetSalesInvoiceId.Value,ct);
    }
    public Task LockReceiptAsync(Guid receiptVoucherLineId,CancellationToken ct=default)
    {
        if(db.Database.CurrentTransaction is null)throw new InvalidOperationException("An active transaction is required.");
        return LockRowAsync("tbl_ReceiptVoucherLines",receiptVoucherLineId,ct);
    }
    private async Task LockRowAsync(string table,Guid id,CancellationToken ct)
    {
        await using var cmd=db.Database.GetDbConnection().CreateCommand();
        cmd.Transaction=db.Database.CurrentTransaction!.GetDbTransaction();
        cmd.CommandText=$"SELECT [Id] FROM [dbo].[{table}] WITH(UPDLOCK,HOLDLOCK) WHERE [Id]=@id";
        var p=cmd.CreateParameter(); p.ParameterName="@id";p.Value=id;cmd.Parameters.Add(p);
        if (await cmd.ExecuteScalarAsync(ct) is null)
            throw new InvalidOperationException($"Customer credit lock target does not exist in {table}.");
    }
}
