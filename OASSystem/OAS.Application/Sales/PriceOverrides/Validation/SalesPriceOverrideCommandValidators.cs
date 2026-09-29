using FluentValidation;
using OAS.Application.Sales.PriceOverrides.Commands;

namespace OAS.Application.Sales.PriceOverrides.Validation;

public sealed class RequestSalesPriceOverrideCommandValidator : AbstractValidator<RequestSalesPriceOverrideCommand>
{
    public RequestSalesPriceOverrideCommandValidator(RequestSalesPriceOverrideRequestValidator validator)
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithErrorCode("sales_invoice_id_required");
        RuleFor(x => x.Request).SetValidator(validator);
    }
}

public sealed class ApproveSalesPriceOverrideCommandValidator : AbstractValidator<ApproveSalesPriceOverrideCommand>
{
    public ApproveSalesPriceOverrideCommandValidator()
    {
        RuleFor(x => x.OverrideId).NotEmpty().WithErrorCode("sales_price_override_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
    }
}

public sealed class RejectSalesPriceOverrideCommandValidator : AbstractValidator<RejectSalesPriceOverrideCommand>
{
    public RejectSalesPriceOverrideCommandValidator()
    {
        RuleFor(x => x.OverrideId).NotEmpty().WithErrorCode("sales_price_override_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
    }
}

public sealed class CancelSalesPriceOverrideCommandValidator : AbstractValidator<CancelSalesPriceOverrideCommand>
{
    public CancelSalesPriceOverrideCommandValidator()
    {
        RuleFor(x => x.OverrideId).NotEmpty().WithErrorCode("sales_price_override_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
    }
}
