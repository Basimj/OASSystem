using FluentValidation;
using OAS.Application.Sales.CustomerOrders.Commands;

namespace OAS.Application.Sales.CustomerOrders.Validation;

public sealed class CreateCustomerOrderCommandValidator : AbstractValidator<CreateCustomerOrderCommand>
{
    public CreateCustomerOrderCommandValidator(CreateCustomerOrderRequestValidator validator) =>
        RuleFor(x => x.Data).SetValidator(validator);
}

public sealed class UpdateCustomerOrderCommandValidator : AbstractValidator<UpdateCustomerOrderCommand>
{
    public UpdateCustomerOrderCommandValidator(UpdateCustomerOrderRequestValidator validator)
    {
        RuleFor(x => x.Id).NotEmpty().WithErrorCode("sales_order_id_required");
        RuleFor(x => x.Data).SetValidator(validator);
    }
}

public sealed class ConfirmCustomerOrderCommandValidator : AbstractValidator<ConfirmCustomerOrderCommand>
{
    public ConfirmCustomerOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithErrorCode("sales_order_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
    }
}

public sealed class CancelCustomerOrderCommandValidator : AbstractValidator<CancelCustomerOrderCommand>
{
    public CancelCustomerOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithErrorCode("sales_order_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
    }
}
