using FluentValidation;using OAS.Contracts.Sales.PriceOverrides;
namespace OAS.Application.Sales.PriceOverrides.Validation;
public sealed class RequestSalesPriceOverrideRequestValidator:AbstractValidator<RequestSalesPriceOverrideRequest>{public RequestSalesPriceOverrideRequestValidator(){RuleFor(x=>x.SalesInvoiceLineId).NotEmpty();RuleFor(x=>x.OverridePrice).GreaterThanOrEqualTo(0);RuleFor(x=>x.Reason).NotEmpty().MaximumLength(1000);RuleFor(x=>x.InvoiceRowVersion).NotEmpty();}}
