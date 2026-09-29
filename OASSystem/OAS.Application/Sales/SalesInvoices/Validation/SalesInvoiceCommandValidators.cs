using FluentValidation;
using OAS.Application.Sales.SalesInvoices.Commands;

namespace OAS.Application.Sales.SalesInvoices.Validation;

public sealed class CreateSalesInvoiceCommandValidator : AbstractValidator<CreateSalesInvoiceCommand>
{
    public CreateSalesInvoiceCommandValidator(CreateSalesInvoiceRequestValidator validator) =>
        RuleFor(x => x.Data).SetValidator(validator);
}

public sealed class UpdateSalesInvoiceCommandValidator : AbstractValidator<UpdateSalesInvoiceCommand>
{
    public UpdateSalesInvoiceCommandValidator(UpdateSalesInvoiceRequestValidator validator)
    {
        RuleFor(x => x.Id).NotEmpty().WithErrorCode("sales_invoice_id_required");
        RuleFor(x => x.Data).SetValidator(validator);
    }
}

public sealed class CreateSalesInvoiceFromOrderCommandValidator : AbstractValidator<CreateSalesInvoiceFromOrderCommand>
{
    public CreateSalesInvoiceFromOrderCommandValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithErrorCode("sales_order_id_required");
        RuleFor(x => x.Request.InvoiceCode).MaximumLength(40);
        RuleFor(x => x.Request.OrderRowVersion).NotEmpty().WithErrorCode("row_version_required");
        RuleFor(x => x.Request.Description).MaximumLength(1000);
    }
}

public sealed class ConfirmSalesInvoiceCommandValidator : AbstractValidator<ConfirmSalesInvoiceCommand>
{
    public ConfirmSalesInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithErrorCode("sales_invoice_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
    }
}

public sealed class PostSalesInvoiceCommandValidator : AbstractValidator<PostSalesInvoiceCommand>
{
    public PostSalesInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithErrorCode("sales_invoice_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
    }
}

public sealed class CancelSalesInvoiceCommandValidator : AbstractValidator<CancelSalesInvoiceCommand>
{
    public CancelSalesInvoiceCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithErrorCode("sales_invoice_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
    }
}
