using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Services;
using OAS.Contracts.Purchasing.CustomerDemand;
using OAS.Contracts.Sales.Checkout;
using OAS.Contracts.Sales.Common;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;
using ContractPaymentPlan = OAS.Contracts.Sales.Enums.SalesPaymentPlan;
using ContractOrderStatus = OAS.Contracts.Sales.Enums.CustomerOrderStatus;
using ContractLineType = OAS.Contracts.Sales.Enums.SalesLineType;
using ContractEyeSide = OAS.Contracts.Sales.Enums.EyeSide;

namespace OAS.Application.Sales.Checkout.Services;

public sealed class SalesCheckoutOrchestrator(
    ICustomerOrderAggregateRepository orders,
    IRepository<SalesInvoice, Guid> invoices,
    IRepository<OpticalJob, Guid> opticalJobs,
    IReadRepository<Customer, Guid> customers,
    ICustomerOrderConfirmationService orderConfirmation,
    ICustomerOrderAvailabilityService availability,
    ICustomerDemandProcurementPort customerDemand,
    ISalesInvoiceFromOrderService invoiceFromOrder,
    ISalesInvoiceConfirmationService invoiceConfirmation,
    ISalesInvoicePostingWorkflow invoicePosting,
    ISalesSettlementService settlement,
    IOpticalProductionPort opticalProduction,
    ISalesCreditExposureService creditExposure,
    IPermissionChecker permissions,
    SalesDtoAssembler assembler,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider) : ISalesCheckoutOrchestrator
{
    private const decimal MoneyTolerance = 0.0001m;

    public async Task<CheckoutCustomerOrderResultDto> CheckoutAsync(
        CustomerOrder order,
        CheckoutCustomerOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);

        var requestedPlan = (SalesPaymentPlan)(byte)request.PaymentPlan;
        if (order.Status != CustomerOrderStatus.Draft)
        {
            if (order.PaymentPlan != requestedPlan)
                throw new ConflictException("sales_checkout_payment_plan_locked", "لا يمكن تغيير خطة السداد بعد تأكيد طلب العميل.");
            if (order.Status == CustomerOrderStatus.Cancelled)
                throw new ConflictException("sales_checkout_order_cancelled", "طلب العميل ملغى ولا يمكن إتمام البيع عليه.");
            return await BuildExistingResultAsync(order, cancellationToken);
        }

        var customer = await customers.GetByIdAsync(order.CustomerId, cancellationToken)
            ?? throw new NotFoundException(nameof(Customer), order.CustomerId);
        if (!customer.IsActive)
            throw new ConflictException("sales_checkout_customer_inactive", "العميل غير مفعل.");

        var paymentTermDays = requestedPlan == SalesPaymentPlan.AccountCredit ? customer.PaymentTermDays : 0;
        order.ChangePaymentPlan(requestedPlan, paymentTermDays);

        if (requestedPlan == SalesPaymentPlan.AccountCredit)
            await ValidateAccountCreditAsync(customer, order, cancellationToken);

        var paymentBaseAmount = await settlement.CalculatePaymentBaseAmountAsync(
            request.PaymentLines, order.OrderDate, cancellationToken);
        var preCheckoutPayment = await settlement.GetPaymentSummaryAsync(order, null, 0m, cancellationToken);
        var remainingBeforeCheckout = Math.Max(0m, order.TotalAmount - preCheckoutPayment.ExistingAdvanceBalance);
        var requiredBaseAmount = Math.Round(remainingBeforeCheckout * order.ExchangeRate, 4, MidpointRounding.AwayFromZero);
        ValidateCheckoutPaymentPlan(order, request.PaymentLines.Count, paymentBaseAmount, requiredBaseAmount);

        var confirmation = await orderConfirmation.ConfirmAsync(
            order, request.SupplierSchedulingDecisions, cancellationToken);
        orders.Update(order);

        // Reservation and CustomerDemand entities must be visible to the invoice/credit/posting
        // queries that run later in the same outer transaction.
        await unitOfWork.SaveChangesAsync(cancellationToken);

        var hasShortage = confirmation.Availability.Lines.Any(x => x.ShortageQuantity > 0m);
        if (hasShortage)
        {
            SalesPaymentCollectionResult? advanceCollection = null;
            if (request.PaymentLines.Count > 0)
                advanceCollection = await settlement.CreateAdvanceForOrderAsync(order, request.PaymentLines, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
            var paidNow = ToOrderCurrency(advanceCollection?.BaseAmount ?? 0m, order);
            var paymentSummary = await settlement.GetPaymentSummaryAsync(order, null, paidNow, cancellationToken);
            return await BuildResultAsync(
                order,
                null,
                confirmation.Availability,
                confirmation.DemandLines,
                advanceCollection is null ? [] : [advanceCollection.ReceiptVoucherId],
                advanceCollection?.CustomerAdvanceIds ?? [],
                null,
                paymentSummary,
                cancellationToken);
        }

        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var invoice = await invoiceFromOrder.CreateAsync(order, today, today, null, $"فاتورة طلب العميل {order.OrderCode}", cancellationToken);
        if (invoice.Status == SalesInvoiceStatus.Draft)
        {
            await invoiceConfirmation.ConfirmAsync(invoice, cancellationToken);
            invoices.Update(invoice);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        if (invoice.Status == SalesInvoiceStatus.Confirmed)
        {
            await invoicePosting.PostAsync(invoice, cancellationToken);
            invoices.Update(invoice);
        }
        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new ConflictException("sales_checkout_invoice_not_posted", "تعذر ترحيل فاتورة المبيعات لإكمال عملية البيع.");

        await settlement.ApplyAdvancesToInvoiceAsync(order, invoice, cancellationToken);

        SalesPaymentCollectionResult? receiptCollection = null;
        if (request.PaymentLines.Count > 0)
            receiptCollection = await settlement.CreateReceiptForInvoiceAsync(invoice, request.PaymentLines, cancellationToken);

        Guid? opticalJobId = null;
        if (order.RequiresProduction)
        {
            opticalJobId = await opticalProduction.EnsureJobAsync(order, invoice.Id, cancellationToken);
        }
        else if (order.Status == CustomerOrderStatus.Confirmed)
        {
            order.MarkReadyForDelivery();
            orders.Update(order);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        var paidNowAmount = ToOrderCurrency(receiptCollection?.BaseAmount ?? 0m, order);
        var finalPayment = await settlement.GetPaymentSummaryAsync(order, invoice, paidNowAmount, cancellationToken);
        if (order.PaymentPlan == SalesPaymentPlan.FullNow && finalPayment.OutstandingAmount > MoneyTolerance)
            throw new ConflictException("sales_checkout_full_payment_incomplete", "الدفع الكامل لم يغطِ كامل المبلغ المستحق.");

        return await BuildResultAsync(
            order,
            invoice,
            confirmation.Availability,
            confirmation.DemandLines,
            receiptCollection is null ? [] : [receiptCollection.ReceiptVoucherId],
            receiptCollection?.CustomerAdvanceIds ?? [],
            opticalJobId,
            finalPayment,
            cancellationToken);
    }

    public async Task<CheckoutCustomerOrderResultDto> DeliverAsync(
        CustomerOrder order,
        DeliverCustomerOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (order.Status == CustomerOrderStatus.Completed)
            return await BuildExistingResultAsync(order, cancellationToken);
        if (order.Status != CustomerOrderStatus.ReadyForDelivery)
            throw new ConflictException("sales_delivery_order_not_ready", "طلب العميل ليس جاهزًا للتسليم.");

        var invoice = await FindInvoiceAsync(order.Id, cancellationToken)
            ?? throw new ConflictException("sales_delivery_invoice_required", "لا توجد فاتورة مبيعات مرتبطة بطلب العميل.");
        if (invoice.Status != SalesInvoiceStatus.Posted)
            throw new ConflictException("sales_delivery_invoice_not_posted", "يجب ترحيل الفاتورة قبل تسليم الطلب للعميل.");

        SalesPaymentCollectionResult? collection = null;
        var before = await settlement.GetPaymentSummaryAsync(order, invoice, 0m, cancellationToken);
        if (request.PaymentLines.Count > 0)
            collection = await settlement.CreateReceiptForInvoiceAsync(invoice, request.PaymentLines, cancellationToken);

        var after = await settlement.GetPaymentSummaryAsync(
            order, invoice, ToOrderCurrency(collection?.BaseAmount ?? 0m, order), cancellationToken);
        if (order.PaymentPlan != SalesPaymentPlan.AccountCredit && after.OutstandingAmount > MoneyTolerance)
        {
            var code = request.PaymentLines.Count == 0 && before.OutstandingAmount > MoneyTolerance
                ? "sales_delivery_payment_required"
                : "sales_delivery_payment_incomplete";
            throw new ConflictException(code, "يجب تسوية المبلغ المتبقي قبل تسليم الطلب للعميل.");
        }

        var job = await FindOpticalJobForUpdateAsync(order.Id, cancellationToken);
        if (order.RequiresProduction)
        {
            if (job is null)
                throw new ConflictException("sales_delivery_optical_job_required", "لا يوجد أمر معمل مرتبط بالطلب الجاهز للتسليم.");
            if (job.Status != OpticalJobStatus.Delivered)
            {
                if (job.Status != OpticalJobStatus.ReadyForDelivery)
                    throw new ConflictException("sales_delivery_optical_job_not_ready", "أمر المعمل ليس جاهزًا للتسليم.");
                job.MarkDelivered();
                opticalJobs.Update(job);
            }
        }

        order.Complete();
        orders.Update(order);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await BuildResultAsync(
            order,
            invoice,
            await availability.AssessAsync(order, cancellationToken),
            await customerDemand.GetOpenDemandForOrderAsync(order.Id, cancellationToken),
            collection is null ? [] : [collection.ReceiptVoucherId],
            [],
            job?.Id,
            after,
            cancellationToken);
    }

    private async Task ValidateAccountCreditAsync(Customer customer, CustomerOrder order, CancellationToken cancellationToken)
    {
        if (!await permissions.HasPermissionAsync(SalesPermissions.Credit.Use, cancellationToken))
            throw new ForbiddenException("ليس لديك صلاحية استخدام البيع الآجل.");

        var exposure = await creditExposure.CalculateExposureBeforeCurrentAsync(customer.Id, null, cancellationToken);
        var newBaseAmount = Math.Round(order.TotalAmount * order.ExchangeRate, 4, MidpointRounding.AwayFromZero);
        try
        {
            customer.EnsureCanUseAccountCredit(exposure, newBaseAmount);
        }
        catch (DomainException ex)
        {
            throw new ConflictException("sales_checkout_credit_not_allowed", ex.Message);
        }
    }

    private static void ValidateCheckoutPaymentPlan(
        CustomerOrder order,
        int paymentLineCount,
        decimal paymentBaseAmount,
        decimal requiredBaseAmount)
    {
        switch (order.PaymentPlan)
        {
            case SalesPaymentPlan.FullNow:
                if (requiredBaseAmount <= MoneyTolerance)
                {
                    if (paymentLineCount != 0 || paymentBaseAmount > MoneyTolerance)
                        throw new ConflictException("sales_checkout_payment_already_covered", "الرصيد المستحق مغطى مسبقًا بعربون العميل ولا يقبل تحصيلًا إضافيًا.");
                    break;
                }
                if (paymentLineCount == 0 || Math.Abs(paymentBaseAmount - requiredBaseAmount) > MoneyTolerance)
                    throw new ConflictException("sales_checkout_full_payment_required", "خطة الدفع الكامل تتطلب دفعات تغطي كامل الرصيد المستحق.");
                break;
            case SalesPaymentPlan.PartialNow:
                if (paymentLineCount == 0 || paymentBaseAmount <= MoneyTolerance || paymentBaseAmount >= requiredBaseAmount - MoneyTolerance)
                    throw new ConflictException("sales_checkout_partial_payment_invalid", "الدفع الجزئي يجب أن يكون أكبر من صفر وأقل من كامل قيمة الطلب.");
                break;
            case SalesPaymentPlan.PayOnPickup:
                if (paymentLineCount != 0)
                    throw new ConflictException("sales_checkout_pickup_payment_not_allowed", "الدفع عند الاستلام لا يقبل دفعات أثناء إتمام البيع الأولي.");
                break;
            case SalesPaymentPlan.AccountCredit:
                if (paymentLineCount != 0)
                    throw new ConflictException("sales_checkout_credit_payment_not_allowed", "البيع الآجل لا يقبل دفعات أثناء الإتمام الأولي في الإصدار الحالي.");
                break;
            default:
                throw new ConflictException("sales_checkout_payment_plan_invalid", "خطة السداد غير صالحة.");
        }
    }

    private async Task<CheckoutCustomerOrderResultDto> BuildExistingResultAsync(
        CustomerOrder order,
        CancellationToken cancellationToken)
    {
        var invoice = await FindInvoiceAsync(order.Id, cancellationToken);
        var currentAvailability = await availability.AssessAsync(order, cancellationToken);
        var demands = await customerDemand.GetOpenDemandForOrderAsync(order.Id, cancellationToken);
        var job = (await opticalJobs.ListAsync(
            new Specification<OpticalJob>().Where(x => x.CustomerOrderId == order.Id && x.IsActive),
            cancellationToken)).SingleOrDefault();
        var payment = await settlement.GetPaymentSummaryAsync(order, invoice, 0m, cancellationToken);
        var references = await settlement.GetReferencesAsync(order, invoice, cancellationToken);
        return await BuildResultAsync(
            order, invoice, currentAvailability, demands,
            references.ReceiptVoucherIds, references.CustomerAdvanceIds,
            job?.Id, payment, cancellationToken);
    }

    private async Task<CheckoutCustomerOrderResultDto> BuildResultAsync(
        CustomerOrder order,
        SalesInvoice? invoice,
        CustomerOrderAvailabilityDto currentAvailability,
        IReadOnlyList<CustomerDemandLine> demands,
        IReadOnlyList<Guid> receiptVoucherIds,
        IReadOnlyList<Guid> advanceIds,
        Guid? opticalJobId,
        SalesPaymentSummaryDto paymentSummary,
        CancellationToken cancellationToken)
    {
        var dto = await assembler.OrderAsync(order, cancellationToken);
        var shortages = currentAvailability.Lines
            .Where(x => x.ShortageQuantity > 0m)
            .Select(a =>
            {
                var line = dto.Lines.Single(x => x.Id == a.CustomerOrderLineId);
                var demand = demands.FirstOrDefault(x => x.CustomerOrderLineId == line.Id && x.ProductVariantId == a.ProductVariantId);
                return new CustomerDemandQueueItemDto(
                    line.Id,
                    a.ProductVariantId,
                    line.ProductCode,
                    line.ProductName ?? line.DescriptionSnapshot,
                    (ContractLineType)(byte)line.LineType,
                    line.OpticalSnapshot?.Eye ?? line.PrescriptionEye,
                    a.RequestedQuantity,
                    a.ShortageQuantity,
                    a.WarehouseId,
                    demand?.PreferredSupplierId,
                    demand?.ScheduledOrderAtUtc,
                    order.RequiredDate,
                    FormatOpticalSummary(line.OpticalSnapshot));
            })
            .ToArray();

        return new CheckoutCustomerOrderResultDto(
            order.Id,
            order.OrderCode,
            (ContractOrderStatus)(byte)order.Status,
            invoice?.Id,
            invoice?.InvoiceCode,
            receiptVoucherIds,
            advanceIds,
            demands.Select(x => (Guid?)x.PurchaseRequestId).FirstOrDefault(),
            opticalJobId,
            paymentSummary,
            shortages);
    }

    private async Task<SalesInvoice?> FindInvoiceAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var rows = await invoices.ListAsync(
            new Specification<SalesInvoice>().Where(x => x.CustomerOrderId == orderId && x.Status != SalesInvoiceStatus.Cancelled),
            cancellationToken);
        return rows.OrderByDescending(x => x.CreatedAtUtc).FirstOrDefault();
    }

    private async Task<OpticalJob?> FindOpticalJobForUpdateAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var rows = await opticalJobs.ListAsync(
            new Specification<OpticalJob>().Where(x => x.CustomerOrderId == orderId && x.IsActive),
            cancellationToken);
        var job = rows.SingleOrDefault();
        return job is null ? null : await opticalJobs.GetForUpdateAsync(job.Id, cancellationToken);
    }

    private static decimal ToOrderCurrency(decimal baseAmount, CustomerOrder order) =>
        order.ExchangeRate <= 0m
            ? 0m
            : Math.Round(baseAmount / order.ExchangeRate, order.CurrencyDecimalPlacesSnapshot, MidpointRounding.AwayFromZero);

    private static string? FormatOpticalSummary(CustomerOrderLineOpticalSnapshotDto? snapshot)
    {
        if (snapshot is null)
            return null;
        var parts = new List<string>();
        if (snapshot.SPH.HasValue) parts.Add($"SPH {snapshot.SPH:0.##}");
        if (snapshot.CYL.HasValue) parts.Add($"CYL {snapshot.CYL:0.##}");
        if (snapshot.Axis.HasValue) parts.Add($"Axis {snapshot.Axis}");
        if (snapshot.ADD.HasValue) parts.Add($"ADD {snapshot.ADD:0.##}");
        if (!string.IsNullOrWhiteSpace(snapshot.MaterialSnapshot)) parts.Add(snapshot.MaterialSnapshot!);
        if (!string.IsNullOrWhiteSpace(snapshot.CoatingSnapshot)) parts.Add(snapshot.CoatingSnapshot!);
        return parts.Count == 0 ? null : string.Join(" / ", parts);
    }
}
