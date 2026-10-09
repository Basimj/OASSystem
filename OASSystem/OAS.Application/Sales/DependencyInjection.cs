using Microsoft.Extensions.DependencyInjection;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.CRUD.Mapping;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.CustomerOrders;
using OAS.Application.Sales.CustomerOrders.Mapping;
using OAS.Application.Sales.CustomerOrders.Specifications;
using OAS.Application.Sales.Posting;
using OAS.Application.Sales.OpticalJobs.Services;
using OAS.Application.Sales.Returns.Services;
using OAS.Application.Sales.Production.Services;
using OAS.Application.Sales.Prescriptions;
using OAS.Application.Sales.Prescriptions.Mapping;
using OAS.Application.Sales.Prescriptions.Specifications;
using OAS.Application.Sales.SalesInvoices;
using OAS.Application.Sales.SalesInvoices.Mapping;
using OAS.Application.Sales.SalesInvoices.Specifications;
using OAS.Application.Sales.Services;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Contracts.Sales.SalesInvoices;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales;

public static class DependencyInjection
{
    public static IServiceCollection AddSalesApplication(this IServiceCollection services)
    {
        services.AddCrudFeature<Prescription, Guid, PrescriptionDto, CreatePrescriptionRequest, UpdatePrescriptionRequest>();
        services.AddScoped<ICrudMapper<Prescription, Guid, PrescriptionDto, CreatePrescriptionRequest, UpdatePrescriptionRequest>, PrescriptionMapper>();
        services.AddScoped<ICrudSpecificationFactory<Prescription>, PrescriptionSpecificationFactory>();
        services.AddScoped<PrescriptionApplicationService>();
        services.AddScoped<ICrudApplicationService<Guid, PrescriptionDto, CreatePrescriptionRequest, UpdatePrescriptionRequest>>(
            sp => sp.GetRequiredService<PrescriptionApplicationService>());

        services.AddCrudFeature<CustomerOrder, Guid, CustomerOrderDto, CreateCustomerOrderRequest, UpdateCustomerOrderRequest>();
        services.AddScoped<ICrudMapper<CustomerOrder, Guid, CustomerOrderDto, CreateCustomerOrderRequest, UpdateCustomerOrderRequest>, CustomerOrderMapper>();
        services.AddScoped<ICrudSpecificationFactory<CustomerOrder>, CustomerOrderSpecificationFactory>();
        services.AddScoped<CustomerOrderApplicationService>();
        services.AddScoped<ICrudApplicationService<Guid, CustomerOrderDto, CreateCustomerOrderRequest, UpdateCustomerOrderRequest>>(
            sp => sp.GetRequiredService<CustomerOrderApplicationService>());

        services.AddCrudFeature<SalesInvoice, Guid, SalesInvoiceDto, CreateSalesInvoiceRequest, UpdateSalesInvoiceRequest>();
        services.AddScoped<ICrudMapper<SalesInvoice, Guid, SalesInvoiceDto, CreateSalesInvoiceRequest, UpdateSalesInvoiceRequest>, SalesInvoiceMapper>();
        services.AddScoped<ICrudSpecificationFactory<SalesInvoice>, SalesInvoiceSpecificationFactory>();
        services.AddScoped<SalesInvoiceApplicationService>();
        services.AddScoped<ICrudApplicationService<Guid, SalesInvoiceDto, CreateSalesInvoiceRequest, UpdateSalesInvoiceRequest>>(
            sp => sp.GetRequiredService<SalesInvoiceApplicationService>());

        services.AddScoped<SalesDtoAssembler>();
        services.AddScoped<ISalesLineResolver, SalesLineResolver>();
        services.AddScoped<ILensVariantResolver, LensVariantResolver>();
        services.AddScoped<ICustomerOrderOpticalService, CustomerOrderOpticalService>();
        services.AddScoped<ICustomerOrderAvailabilityService, CustomerOrderAvailabilityService>();
        services.AddScoped<ICustomerOrderConfirmationService, CustomerOrderConfirmationService>();
        services.AddScoped<ISalesInvoiceFromOrderService, SalesInvoiceFromOrderService>();
        services.AddScoped<ISalesInvoiceConfirmationService, SalesInvoiceConfirmationService>();
        services.AddScoped<ISalesInvoicePostingWorkflow, SalesInvoicePostingWorkflow>();
        services.AddScoped<ISalesInvoiceBalanceService, SalesInvoiceBalanceService>();
        services.AddScoped<ISalesSettlementService, SalesSettlementService>();
        services.AddScoped<IOpticalProductionPort, OpticalProductionPort>();
        services.AddScoped<ISalesCheckoutOrchestrator, OAS.Application.Sales.Checkout.Services.SalesCheckoutOrchestrator>();
        services.AddScoped<ISalesCheckoutContextService, OAS.Application.Sales.Checkout.Services.SalesCheckoutContextService>();
        services.AddScoped<ICustomerOrderFulfillmentService, CustomerOrderFulfillmentService>();
        services.AddScoped<IOpticalJobService, OpticalJobService>();
        services.AddScoped<IOpticalJobInventoryPort, OpticalJobInventoryPort>();
        services.AddScoped<IOpticalJobAccountingPort, OpticalJobAccountingPort>();
        services.AddScoped<IOpticalJobPurchasingPort, OpticalJobPurchasingPort>();
        services.AddScoped<IOpticalJobSalesPort, OpticalJobSalesPort>();
        services.AddScoped<IOpticalReplacementReceiptService, OpticalReplacementReceiptService>();
        services.AddScoped<ISalesPrescriptionValidator, SalesPrescriptionValidator>();
        services.AddScoped<ISalesPostingPeriodService, SalesPostingPeriodService>();
        services.AddScoped<ISalesStockReservationService, SalesStockReservationService>();
        services.AddScoped<ISalesCreditExposureService, SalesCreditExposureService>();
        services.AddScoped<ISalesPaymentAllocationTargetValidator, SalesPaymentAllocationTargetValidator>();
        services.AddScoped<OAS.Application.Accounting.Abstractions.IPaymentAllocationTargetValidator>(sp => sp.GetRequiredService<ISalesPaymentAllocationTargetValidator>());
        services.AddScoped<ISalesInventoryPostingService, SalesInventoryPostingService>();
        services.AddScoped<ISalesInvoiceAccountingPostingService, SalesInvoiceAccountingPostingService>();
        services.AddScoped<ISalesReturnInventoryPostingService, SalesReturnInventoryPostingService>();
        services.AddScoped<ISalesReturnAccountingPostingService, SalesReturnAccountingPostingService>();
        services.AddScoped<IOpticalProductionInventoryService, OpticalProductionInventoryService>();
        services.AddScoped<ISalesImmediateSettlementService, SalesImmediateSettlementService>();

        return services;
    }
}
