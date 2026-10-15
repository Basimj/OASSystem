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
        RuleFor(x => x.Request.CashAccountId)
            .NotEmpty()
            .When(x => x.Request.ImmediatePaymentMethod == OAS.Contracts.Sales.Enums.SalesImmediatePaymentMethod.Cash)
            .WithErrorCode("sales_immediate_cash_account_required");
        RuleFor(x => x.Request.BankAccountId)
            .NotEmpty()
            .When(x => x.Request.ImmediatePaymentMethod == OAS.Contracts.Sales.Enums.SalesImmediatePaymentMethod.Bank)
            .WithErrorCode("sales_immediate_bank_account_required");
    }
}

public sealed class CollectSalesInvoicePaymentCommandValidator : AbstractValidator<CollectSalesInvoicePaymentCommand>
{
    public CollectSalesInvoicePaymentCommandValidator()
    {
        RuleFor(x => x.InvoiceId).NotEmpty().WithErrorCode("sales_invoice_id_required");
        RuleFor(x => x.Request.PaymentLines)
            .NotEmpty()
            .WithErrorCode("sales_invoice_collection_payment_required");
        RuleForEach(x => x.Request.PaymentLines).ChildRules(line =>
        {
            line.RuleFor(x => x.CurrencyId).NotEmpty().WithErrorCode("sales_payment_currency_required");
            line.RuleFor(x => x.Amount).GreaterThan(0m).WithErrorCode("sales_payment_amount_invalid");
        });
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
