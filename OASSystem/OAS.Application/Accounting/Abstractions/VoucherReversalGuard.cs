using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Abstractions;

public static class VoucherReversalGuard
{
    public static async Task EnsureNotReversedAsync(IReadRepository<JournalEntry, Guid>? journals, Guid? journalId, CancellationToken ct)
    {
        if (journals is null || journalId is not Guid id) return;
        var journal = await journals.GetByIdAsync(id, ct);
        if (journal?.ReversedJournalId.HasValue == true)
            throw new ConflictException("voucher_already_reversed", "لا يمكن تخصيص مبلغ أو إنشاء تسوية من سند معكوس.");
    }
}
