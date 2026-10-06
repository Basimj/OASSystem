using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Lookups;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Checkout.Services;

public sealed class SalesCheckoutContextService(
    ICustomerOrderAggregateRepository orders,
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Prescription, Guid> prescriptions,
    IReadRepository<PrescriptionRevision, Guid> revisions,
    IReadRepository<PrescriptionEyeDetail, Guid> eyeDetails,
    IReadRepository<SalesInvoice, Guid> invoices,
    ICustomerOrderAvailabilityService availability,
    ISalesCreditExposureService creditExposure,
    ISalesSettlementService settlement,
    SalesDtoAssembler assembler) : ISalesCheckoutContextService
{
    public async Task<SalesCheckoutContextDto> GetAsync(Guid customerOrderId, CancellationToken cancellationToken = default)
    {
        var order = await orders.GetAggregateAsync(customerOrderId, false, cancellationToken)
            ?? throw new NotFoundException(nameof(CustomerOrder), customerOrderId);
        var customer = await customers.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), order.CustomerId);

        var orderDto = await assembler.OrderAsync(order, cancellationToken);
        var customerDto = new SalesCustomerLookupDto(
            customer.Id,
            customer.CustomerCode,
            customer.NameAr,
            customer.NameEn,
            customer.ContactInfo.Mobile,
            customer.ContactInfo.Phone,
            customer.IsCreditAllowed,
            customer.CreditLimit,
            customer.PaymentTermDays,
            customer.IsActive);
        var prescriptionContext = await GetPrescriptionContextAsync(customer, cancellationToken);
        var availabilityDto = await availability.AssessAsync(order, cancellationToken);
        var exposure = await creditExposure.CalculateExposureBeforeCurrentAsync(customer.Id, null, cancellationToken);
        var credit = new SalesCreditContextDto(
            customer.Id,
            customer.CustomerCode,
            customer.IsActive,
            customer.IsCreditAllowed,
            customer.CreditLimit,
            exposure,
            Math.Max(0m, customer.CreditLimit - exposure),
            customer.PaymentTermDays);
        var invoice = (await invoices.ListAsync(
            new Specification<SalesInvoice>().Where(x => x.CustomerOrderId == order.Id && x.Status != SalesInvoiceStatus.Cancelled),
            cancellationToken)).OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
        var payment = await settlement.GetPaymentSummaryAsync(order, invoice, 0m, cancellationToken);

        return new SalesCheckoutContextDto(orderDto, customerDto, prescriptionContext, availabilityDto, credit, payment, invoice?.Id, invoice?.InvoiceCode);
    }

    private async Task<CustomerPrescriptionContextDto> GetPrescriptionContextAsync(Customer customer, CancellationToken ct)
    {
        var rows = await prescriptions.ListAsync(
            new Specification<Prescription>().Where(x => x.CustomerId == customer.Id && x.IsActive), ct);
        var latest = rows.OrderByDescending(x => x.PrescriptionDate).ThenByDescending(x => x.CreatedAtUtc).FirstOrDefault();
        if (latest is null)
            return new CustomerPrescriptionContextDto(
                customer.Id, customer.CustomerCode, customer.NameAr, customer.ContactInfo.Mobile,
                false, null, null, null, null, null, null, null);

        var revisionRows = await revisions.ListAsync(
            new Specification<PrescriptionRevision>().Where(x => x.PrescriptionId == latest.Id && x.IsActive), ct);
        var revision = revisionRows.OrderByDescending(x => x.IsCurrent).ThenByDescending(x => x.RevisionNumber).FirstOrDefault();
        CustomerPrescriptionEyeContextDto? od = null;
        CustomerPrescriptionEyeContextDto? os = null;
        if (revision is not null)
        {
            var eyes = await eyeDetails.ListAsync(
                new Specification<PrescriptionEyeDetail>().Where(x => x.PrescriptionRevisionId == revision.Id && x.IsActive), ct);
            od = MapEye(eyes.FirstOrDefault(x => x.Eye == EyeSide.RightOD));
            os = MapEye(eyes.FirstOrDefault(x => x.Eye == EyeSide.LeftOS));
        }

        return new CustomerPrescriptionContextDto(
            customer.Id, customer.CustomerCode, customer.NameAr, customer.ContactInfo.Mobile,
            true, latest.Id, latest.PrescriptionCode, revision?.Id, revision?.RevisionNumber,
            latest.PrescriptionDate, od, os);
    }

    private static CustomerPrescriptionEyeContextDto? MapEye(PrescriptionEyeDetail? x) => x is null ? null : new(
        (OAS.Contracts.Sales.Enums.EyeSide)(byte)x.Eye,
        x.SPH, x.CYL, x.Axis, x.ADD, x.Prism,
        x.PrismBase.HasValue ? (OAS.Contracts.Sales.Enums.PrismBaseDirection?)(byte)x.PrismBase.Value : null,
        x.PD, x.MonocularPD, x.VA, x.FittingHeight);
}
