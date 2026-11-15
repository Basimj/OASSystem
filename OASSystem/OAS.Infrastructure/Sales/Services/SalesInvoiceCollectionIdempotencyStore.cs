using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Sales.Services;

/// <summary>
/// The MediatR TransactionBehavior starts a single transaction for the entire command.
/// The SQL application lock serializes concurrent requests for the same OperationId,
/// including retries across app instances. Receipt allocation and the stored result
/// commit together or roll back together.
/// </summary>
public sealed class SalesInvoiceCollectionIdempotencyStore(OasDbContext db)
    : ISalesInvoiceCollectionIdempotencyStore
{
    public async Task<CollectSalesInvoicePaymentResultDto?> GetExistingAsync(
        Guid invoiceId, Guid operationId, string fingerprint, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null)
            throw new InvalidOperationException("Invoice collection idempotency requires an ambient database transaction.");

        var resource = $"oas.sales.collection.{operationId:N}";
        var lockResult = new SqlParameter("@lockResult", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resourceParameter = new SqlParameter("@resource", SqlDbType.NVarChar, 255) { Value = resource };
        await db.Database.ExecuteSqlRawAsync(
            "EXEC @lockResult = sys.sp_getapplock @Resource=@resource, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;",
            new object[] { lockResult, resourceParameter }, ct);
        if (lockResult.Value is not int value || value < 0)
            throw new ConflictException("sales_invoice_collection_lock_timeout",
                "لا يمكن تأكيد نتيجة التحصيل الآن؛ أعد المحاولة بنفس العملية بعد تحديث الفاتورة.");

        var prior = await db.Set<SalesInvoiceCollectionAttempt>().AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == operationId, ct);
        if (prior is null) return null;
        if (prior.InvoiceId != invoiceId || !string.Equals(prior.Fingerprint, fingerprint, StringComparison.Ordinal))
            throw new ConflictException("sales_invoice_collection_key_changed",
                "استخدم رقم عملية جديدًا عند تغيير مبالغ أو حسابات التحصيل. لا يمكن إعادة استخدام رقم العملية مع بيانات مختلفة.");
        return JsonSerializer.Deserialize<CollectSalesInvoicePaymentResultDto>(prior.ResultJson)
            ?? throw new InvalidOperationException("Could not deserialize stored invoice collection result.");
    }

    public Task StoreAsync(Guid invoiceId, Guid operationId, string fingerprint,
        CollectSalesInvoicePaymentResultDto result, CancellationToken ct)
    {
        db.Set<SalesInvoiceCollectionAttempt>().Add(new SalesInvoiceCollectionAttempt
        {
            Id = operationId,
            InvoiceId = invoiceId,
            Fingerprint = fingerprint,
            ResultJson = JsonSerializer.Serialize(result),
            CreatedAtUtc = DateTime.UtcNow
        });
        // The outer TransactionBehavior is responsible for SaveChanges + Commit.
        return Task.CompletedTask;
    }
}
