using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Accounting.Abstractions;

public sealed record CustomerAdvanceApplicationPostingResult(
    Guid JournalEntryId,
    decimal TargetBaseAmount);

public interface ICustomerAdvanceAccountingPostingService
{
    Task<CustomerAdvanceApplicationPostingResult> PostApplicationAsync(
        Guid applicationId,
        CustomerAdvance advance,
        SalesInvoice invoice,
        decimal amount,
        decimal sourceBaseAmount,
        decimal targetBaseAmount,
        string? appliedBy,
        DateTime appliedAtUtc,
        CancellationToken cancellationToken = default);
}
