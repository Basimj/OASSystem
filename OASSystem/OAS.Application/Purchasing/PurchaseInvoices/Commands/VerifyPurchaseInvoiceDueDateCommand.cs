using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Application.Purchasing.Common;
using OAS.Contracts.Purchasing.PurchaseInvoices;
using OAS.Domain.Purchasing.Entities;

namespace OAS.Application.Purchasing.PurchaseInvoices.Commands;

public sealed record VerifyPurchaseInvoiceDueDateCommand(
    Guid PurchaseInvoiceId,
    VerifyPurchaseInvoiceDueDateRequest Request)
    : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [PurchasingPermissions.Invoices.Edit];
}

public sealed class VerifyPurchaseInvoiceDueDateCommandHandler(
    IPurchaseInvoiceRepository invoices,
    IRepository<PurchaseInvoiceDueDateAudit, Guid> audits,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<VerifyPurchaseInvoiceDueDateCommand>
{
    public async Task Handle(VerifyPurchaseInvoiceDueDateCommand command, CancellationToken cancellationToken)
    {
        var invoice = await invoices.GetForUpdateAsync(command.PurchaseInvoiceId, cancellationToken)
            ?? throw new NotFoundException(nameof(PurchaseInvoice), command.PurchaseInvoiceId);
        PurchasingRowVersion.EnsureMatches(invoice.RowVersion, command.Request.RowVersion, "Purchase invoice");
        if (string.IsNullOrWhiteSpace(command.Request.Reason))
            throw new ConflictException("purchasing_due_date_reason_required", "سبب تصحيح تاريخ الاستحقاق مطلوب.");
        if (command.Request.PaymentTermDaysSnapshot < 0)
            throw new ConflictException("purchasing_payment_terms_invalid", "عدد أيام السداد لا يمكن أن يكون سالبًا.");

        var oldDueDate = invoice.DueDate;
        var oldTerms = invoice.PaymentTermDaysSnapshot;
        var oldProvenance = invoice.DueDateProvenance;
        var userId = string.IsNullOrWhiteSpace(currentUser.UserId) ? "system" : currentUser.UserId!;
        var at = timeProvider.GetUtcNow().UtcDateTime;

        invoice.VerifyHistoricalDueDate(command.Request.DueDate, command.Request.PaymentTermDaysSnapshot);
        invoices.Update(invoice);

        await audits.AddAsync(PurchaseInvoiceDueDateAudit.Create(
            Guid.NewGuid(),
            invoice.Id,
            oldDueDate,
            oldTerms,
            oldProvenance,
            command.Request.DueDate,
            command.Request.PaymentTermDaysSnapshot,
            command.Request.Reason,
            userId,
            at), cancellationToken);
    }
}
