using Microsoft.Extensions.DependencyInjection;
using OAS.Application.CRUD.Abstractions;
using OAS.Application.CRUD.Mapping;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.CustomerOrders;
using OAS.Application.Sales.CustomerOrders.Mapping;
using OAS.Application.Sales.CustomerOrders.Specifications;
using OAS.Application.Sales.Posting;
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
        services.AddScoped<ISalesPrescriptionValidator, SalesPrescriptionValidator>();
        services.AddScoped<ISalesPostingPeriodService, SalesPostingPeriodService>();
        services.AddScoped<ISalesStockReservationService, SalesStockReservationService>();
        services.AddScoped<ISalesCreditExposureService, SalesCreditExposureService>();
        services.AddScoped<ISalesPaymentAllocationTargetValidator, SalesPaymentAllocationTargetValidator>();
        services.AddScoped<ISalesInventoryPostingService, SalesInventoryPostingService>();
        services.AddScoped<ISalesInvoiceAccountingPostingService, SalesInvoiceAccountingPostingService>();
        services.AddScoped<ISalesImmediateSettlementService, SalesImmediateSettlementService>();

        return services;
    }
}
