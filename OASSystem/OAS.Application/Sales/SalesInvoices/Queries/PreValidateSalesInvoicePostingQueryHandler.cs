using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Common;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.SalesInvoices.Queries;

public sealed class PreValidateSalesInvoicePostingQueryHandler(
    ISalesInvoiceAggregateRepository invoices,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Account, Guid> accounts,
    ISalesPostingPeriodService periods,
    ISalesStockReservationService stock,
    ISalesCreditExposureService credit)
    : IRequestHandler<PreValidateSalesInvoicePostingQuery, SalesPostingPreValidationDto>
{
    public async Task<SalesPostingPreValidationDto> Handle(PreValidateSalesInvoicePostingQuery request, CancellationToken ct)
    {
        var issues = new List<SalesValidationIssueDto>();
        var invoice = await invoices.GetAggregateAsync(request.InvoiceId, false, ct);
        if (invoice is null)
            return new(request.InvoiceId, false, [new(SalesErrorCodes.InvoiceNotFound, "الفاتورة غير موجودة.")]);

        if (invoice.Status != SalesInvoiceStatus.Confirmed)
            issues.Add(new(SalesErrorCodes.InvoiceInvalidStatus, "يجب أن تكون الفاتورة مؤكدة قبل الترحيل."));

        try
        {
            await periods.GetOpenPostingPeriodAsync(
                invoice.PostingDate,
                invoice.Lines.Any(x => x.IsActive && x.RequiresInventory),
                ct);
        }
        catch (ConflictException ex)
        {
            issues.Add(new(ex.Code, ex.Message, "PostingDate"));
        }

        var customer = await customers.GetByIdAsync(invoice.CustomerId, ct);
        if (customer is null || !customer.IsActive)
        {
            issues.Add(new(SalesErrorCodes.CustomerInactive, "العميل غير موجود أو غير فعال.", "CustomerId"));
        }
        else
        {
            var account = await accounts.GetByIdAsync(customer.AccountId, ct);
            if (account is null || !account.CanReceivePosting())
                issues.Add(new(SalesErrorCodes.CustomerAccountInvalid, "حساب العميل غير صالح للترحيل.", "CustomerId"));

            try
            {
                await credit.ValidateAsync(customer, invoice, ct);
            }
            catch (ConflictException ex)
            {
                issues.Add(new(ex.Code, ex.Message));
            }
        }

        try
        {
            await stock.ValidateInvoiceReservationsAsync(invoice, ct);
        }
        catch (ConflictException ex)
        {
            issues.Add(new(ex.Code, ex.Message));
        }

        return new(invoice.Id, issues.Count == 0, issues);
    }
}
