using FluentValidation;

namespace OAS.Application.Inventory.Transactions.Commands.PostInventoryTransaction;

public sealed class PostInventoryTransactionCommandValidator : AbstractValidator<PostInventoryTransactionCommand>
{
    public PostInventoryTransactionCommandValidator()
    {
        RuleFor(x => x.TransactionId)
            .NotEmpty().WithErrorCode("transaction_id_required");
    }
}
