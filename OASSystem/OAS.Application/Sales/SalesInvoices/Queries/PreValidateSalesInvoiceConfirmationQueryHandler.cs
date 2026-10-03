using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Queries;

public sealed class PreValidateSalesInvoiceConfirmationQueryHandler(
    ISalesInvoiceAggregateRepository invoices,
    IReadRepository<Customer, Guid> customers,
    ISalesCreditExposureService credit,
    ISalesStockReservationService stock)
    : IRequestHandler<PreValidateSalesInvoiceConfirmationQuery, SalesConfirmationPreValidationDto>
{
    public async Task<SalesConfirmationPreValidationDto> Handle(
        PreValidateSalesInvoiceConfirmationQuery request,
        CancellationToken ct)
    {
        var issues = new List<SalesValidationIssueDto>();
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, false, ct);

        if (invoice is null)
        {
            return new SalesConfirmationPreValidationDto(
                request.InvoiceId,
                false,
                [new SalesValidationIssueDto(SalesErrorCodes.InvoiceNotFound, "الفاتورة غير موجودة.")]);
        }

        if (invoice.Status != SalesInvoiceStatus.Draft)
        {
            issues.Add(new SalesValidationIssueDto(
                SalesErrorCodes.InvoiceInvalidStatus,
                "يمكن تأكيد الفاتورة من حالة المسودة فقط."));
        }

        var customer = await customers.GetByIdAsync(invoice.CustomerId, ct);
        if (customer is null || !customer.IsActive)
        {
            issues.Add(new SalesValidationIssueDto(
                SalesErrorCodes.CustomerInactive,
                "العميل غير موجود أو غير فعال.",
                "CustomerId"));

            return new SalesConfirmationPreValidationDto(invoice.Id, false, issues);
        }

        var assessment = await credit.EvaluateAsync(customer, invoice, ct);
        if (!assessment.IsAllowed)
        {
            issues.Add(new SalesValidationIssueDto(
                assessment.ErrorCode ?? SalesErrorCodes.CreditLimitExceeded,
                assessment.Message ?? "تعذر اعتماد البيع الآجل للعميل.",
                "PaymentTermType"));
        }

        if (!await stock.HasSufficientStockForInvoiceAsync(invoice, ct))
        {
            issues.Add(new SalesValidationIssueDto(
                SalesErrorCodes.InsufficientStock,
                "الكمية المتاحة غير كافية لتأكيد الفاتورة.",
                "Lines"));
        }

        return new SalesConfirmationPreValidationDto(
            invoice.Id,
            issues.Count == 0,
            issues,
            assessment.CreditLimit,
            assessment.ExposureBeforeCurrent,
            assessment.InvoiceBaseAmount,
            assessment.NewExposure);
    }
}
