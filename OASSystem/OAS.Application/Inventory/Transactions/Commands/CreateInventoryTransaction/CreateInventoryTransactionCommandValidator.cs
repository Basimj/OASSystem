using FluentValidation;
using OAS.Contracts.Enums.Inventory;

namespace OAS.Application.Inventory.Transactions.Commands.CreateInventoryTransaction;

public sealed class CreateInventoryTransactionCommandValidator : AbstractValidator<CreateInventoryTransactionCommand>
{
    public CreateInventoryTransactionCommandValidator()
    {
        RuleFor(x => x.Request.TransactionType)
            .IsInEnum().WithErrorCode("transaction_type_invalid");

        RuleFor(x => x.Request.Lines)
            .NotEmpty().WithErrorCode("transaction_lines_required");

        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductVariantId)
                .NotEmpty().WithErrorCode("product_variant_id_required");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithErrorCode("line_quantity_must_be_positive");

            line.RuleFor(l => l.UnitCost)
                .GreaterThanOrEqualTo(0).WithErrorCode("line_unit_cost_invalid");
        });

        When(x => x.Request.TransactionType is InventoryTransactionType.Receipt
                  or InventoryTransactionType.Opening
                  or InventoryTransactionType.SalesReturn, () =>
        {
            RuleFor(x => x.Request.DestinationWarehouseId)
                .NotEmpty().WithErrorCode("destination_warehouse_required");
        });

        When(x => x.Request.TransactionType is InventoryTransactionType.Issue
                  or InventoryTransactionType.PurchaseReturn
                  or InventoryTransactionType.ProductionIssue
                  or InventoryTransactionType.Scrap, () =>
        {
            RuleFor(x => x.Request.SourceWarehouseId)
                .NotEmpty().WithErrorCode("source_warehouse_required");
        });

        When(x => x.Request.TransactionType == InventoryTransactionType.Transfer, () =>
        {
            RuleFor(x => x.Request.SourceWarehouseId)
                .NotEmpty().WithErrorCode("source_warehouse_required");

            RuleFor(x => x.Request.DestinationWarehouseId)
                .NotEmpty().WithErrorCode("destination_warehouse_required");

            RuleFor(x => x.Request)
                .Must(r => r.SourceWarehouseId != r.DestinationWarehouseId)
                .WithErrorCode("transfer_warehouses_must_differ");
        });
    }
}
