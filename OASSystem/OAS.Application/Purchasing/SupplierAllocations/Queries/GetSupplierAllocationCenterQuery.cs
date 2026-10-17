using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Application.Purchasing.Abstractions;
using OAS.Application.Purchasing.Authorization;
using OAS.Contracts.Purchasing.SupplierAllocations;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Purchasing.Enums;

namespace OAS.Application.Purchasing.SupplierAllocations.Queries;

public sealed record GetSupplierAllocationCenterQuery(Guid SupplierId)
    : IQuery<SupplierAllocationCenterDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } =
    [
        PurchasingPermissions.View,
        PurchasingPermissions.Invoices.View,
        AccountingPermissions.PaymentVouchers.View,
        AccountingPermissions.PaymentAllocations.View
    ];
}

public sealed class GetSupplierAllocationCenterQueryHandler(
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<PaymentVoucherLine, Guid> paymentLines,
    IReadRepository<PaymentVoucher, Guid> paymentVouchers,
    IReadRepository<PaymentAllocation, Guid> allocations,
    IReadRepository<PurchaseInvoice, Guid> invoices,
    IReadRepository<PurchaseReturn, Guid> returns,
    IReadRepository<Currency, Guid> currencies,
    IPurchaseInvoiceBalanceService invoiceBalances,
    ISupplierCreditSettlementService supplierCredits)
    : IRequestHandler<GetSupplierAllocationCenterQuery, SupplierAllocationCenterDto>
{
    public async Task<SupplierAllocationCenterDto> Handle(
        GetSupplierAllocationCenterQuery request,
        CancellationToken ct)
    {
        if (request.SupplierId == Guid.Empty)
            throw new ArgumentException("Supplier id is required.", nameof(request));

        var supplier = await suppliers.GetByIdAsync(request.SupplierId, ct)
            ?? throw new NotFoundException(nameof(Supplier), request.SupplierId);

        var unappliedPayments = await BuildUnappliedPaymentsAsync(request.SupplierId, ct);
        var openInvoices = await BuildOpenInvoicesAsync(request.SupplierId, ct);
        var availableCredits = await BuildAvailableCreditsAsync(request.SupplierId, ct);

        return new SupplierAllocationCenterDto(
            supplier.Id,
            supplier.SupplierCode,
            supplier.NameAr,
            Round(unappliedPayments.Sum(x => x.UnappliedBaseAmount)),
            Round(availableCredits.Sum(x => x.AvailableCreditBaseAmount)),
            unappliedPayments,
            openInvoices,
            availableCredits);
    }

    private async Task<IReadOnlyList<SupplierUnappliedPaymentDto>> BuildUnappliedPaymentsAsync(
        Guid supplierId,
        CancellationToken ct)
    {
        var lines = await paymentLines.ListAsync(
            new Specification<PaymentVoucherLine>()
                .Where(x =>
                    x.PartyType == SettlementPartyType.Supplier &&
                    x.SupplierId == supplierId)
                .AddSort(nameof(PaymentVoucherLine.PaymentVoucherId), OAS.Contracts.Common.Pagination.SortDirection.Descending),
            ct);

        var result = new List<SupplierUnappliedPaymentDto>();
        foreach (var line in lines)
        {
            if (line.CurrencyId is not Guid currencyId ||
                string.IsNullOrWhiteSpace(line.CurrencyCodeSnapshot) ||
                line.ExchangeRate is not > 0m)
            {
                continue;
            }

            var voucher = await paymentVouchers.GetByIdAsync(line.PaymentVoucherId, ct);
            if (voucher is null || voucher.Status != PaymentVoucherStatus.Posted)
                continue;

            var active = await allocations.ListAsync(
                new Specification<PaymentAllocation>().Where(x =>
                    x.PaymentVoucherLineId == line.Id &&
                    x.ReversedAtUtc == null),
                ct);

            var applied = Round(active.Sum(x => x.AllocatedAmount));
            var unapplied = Math.Max(0m, Round(line.Amount - applied));
            if (unapplied <= 0m)
                continue;

            var sourceBase = line.BaseAmount is > 0m
                ? line.BaseAmount.Value
                : Round(line.Amount * line.ExchangeRate.Value);
            var unappliedBase = line.Amount > 0m
                ? Round(sourceBase * (unapplied / line.Amount))
                : 0m;

            result.Add(new SupplierUnappliedPaymentDto(
                line.Id,
                voucher.Id,
                voucher.VoucherNumber,
                voucher.VoucherDate,
                currencyId,
                line.CurrencyCodeSnapshot,
                line.ExchangeRate.Value,
                line.Amount,
                applied,
                unapplied,
                sourceBase,
                unappliedBase));
        }

        return result
            .OrderByDescending(x => x.VoucherDate)
            .ThenBy(x => x.VoucherNumber, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private async Task<IReadOnlyList<SupplierOpenInvoiceDto>> BuildOpenInvoicesAsync(
        Guid supplierId,
        CancellationToken ct)
    {
        var posted = await invoices.ListAsync(
            new Specification<PurchaseInvoice>()
                .Where(x => x.SupplierId == supplierId && x.Status == PurchaseInvoiceStatus.Posted),
            ct);

        var result = new List<SupplierOpenInvoiceDto>();
        foreach (var invoice in posted.OrderBy(x => x.InvoiceDate))
        {
            var balance = await invoiceBalances.GetAsync(invoice, null, ct);
            if (balance.OutstandingAmount <= 0m && balance.OutstandingBaseAmount <= 0m)
                continue;

            var currencyCode = invoice.CurrencyCodeSnapshot;
            if (string.IsNullOrWhiteSpace(currencyCode))
            {
                var currency = await currencies.GetByIdAsync(invoice.CurrencyId, ct);
                currencyCode = currency?.Code ?? invoice.CurrencyId.ToString("D");
            }

            result.Add(new SupplierOpenInvoiceDto(
                invoice.Id,
                invoice.PurchaseInvoiceCode,
                invoice.SupplierInvoiceCode,
                invoice.InvoiceDate,
                invoice.CurrencyId,
                currencyCode,
                invoice.ExchangeRate,
                invoice.TotalAmount,
                balance.PaidAmount,
                balance.AppliedCreditInvoiceCurrencyAmount,
                balance.OutstandingAmount,
                invoice.BaseTotalAmount,
                balance.OutstandingBaseAmount));
        }

        return result;
    }

    private async Task<IReadOnlyList<SupplierAvailableCreditDto>> BuildAvailableCreditsAsync(
        Guid supplierId,
        CancellationToken ct)
    {
        var postedReturns = await returns.ListAsync(
            new Specification<PurchaseReturn>().Where(x =>
                x.SupplierId == supplierId &&
                x.Status == PurchaseReturnStatus.Posted &&
                x.PurchaseInvoiceId != null &&
                x.JournalEntryId != null &&
                x.SupplierGrossBaseAmount > 0m),
            ct);

        var result = new List<SupplierAvailableCreditDto>();
        foreach (var purchaseReturn in postedReturns.OrderBy(x => x.PostingDate))
        {
            var snapshot = await supplierCredits.GetSnapshotAsync(purchaseReturn.Id, ct);
            if (snapshot.AvailableCreditBaseAmount <= 0m)
                continue;

            result.Add(new SupplierAvailableCreditDto(
                purchaseReturn.Id,
                purchaseReturn.ReturnCode,
                purchaseReturn.PostingDate,
                purchaseReturn.PurchaseInvoiceId,
                snapshot.TotalCreditBaseAmount,
                snapshot.AppliedCreditBaseAmount,
                snapshot.AvailableCreditBaseAmount));
        }

        return result;
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
