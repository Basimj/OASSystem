using FluentValidation;
using OAS.Contracts.Enums.Inventory;
using OAS.Contracts.Inventory.Transactions;

namespace OAS.Application.Inventory.Transactions.Commands.CreateInventoryTransaction;

public sealed class CreateInventoryTransactionCommandValidator : AbstractValidator<CreateInventoryTransactionCommand>
{
    public CreateInventoryTransactionCommandValidator()
    {
        RuleFor(x => x.Request.TransactionNumber)
            .MaximumLength(32).WithErrorCode("transaction_number_too_long");

        RuleFor(x => x.Request.TransactionType)
            .IsInEnum().WithErrorCode("transaction_type_invalid");

        RuleFor(x => x.Request.TransactionDate)
            .NotEmpty().WithErrorCode("transaction_date_required");

        RuleFor(x => x.Request.ReferenceType)
            .MaximumLength(64).WithErrorCode("transaction_reference_type_too_long");

        RuleFor(x => x.Request.Reason)
            .MaximumLength(500).WithErrorCode("transaction_reason_too_long");

        RuleFor(x => x.Request.Notes)
            .MaximumLength(1000).WithErrorCode("transaction_notes_too_long");

        RuleFor(x => x.Request.Lines)
            .NotEmpty().WithErrorCode("transaction_lines_required")
            .Must(HaveUniqueVariants).WithErrorCode("transaction_duplicate_variant_lines");

        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductVariantId)
                .NotEmpty().WithErrorCode("product_variant_id_required");

            line.RuleFor(l => l.Quantity)
                .GreaterThan(0).WithErrorCode("line_quantity_must_be_positive")
                .Must(value => FitsPrecision(value, 18, 3))
                .WithErrorCode("line_quantity_precision_invalid");

            line.RuleFor(l => l.UnitCost)
                .GreaterThanOrEqualTo(0).WithErrorCode("line_unit_cost_invalid")
                .Must(value => FitsPrecision(value, 18, 2))
                .WithErrorCode("line_unit_cost_precision_invalid");

            line.RuleFor(l => l.Notes)
                .MaximumLength(500).WithErrorCode("line_notes_too_long");

            line.RuleFor(l => l)
                .Must(TotalCostFitsDatabase)
                .WithErrorCode("line_total_cost_precision_invalid");
        });

        When(x => x.Request.TransactionType is InventoryTransactionType.Receipt
                  or InventoryTransactionType.Opening
                  or InventoryTransactionType.SalesReturn
                  or InventoryTransactionType.AdjustmentIncrease, () =>
        {
            RuleFor(x => x.Request.DestinationWarehouseId)
                .NotEmpty().WithErrorCode("destination_warehouse_required");
        });

        When(x => x.Request.TransactionType is InventoryTransactionType.Issue
                  or InventoryTransactionType.AdjustmentDecrease
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

        When(x => x.Request.TransactionType is InventoryTransactionType.AdjustmentIncrease
                  or InventoryTransactionType.AdjustmentDecrease, () =>
        {
            RuleFor(x => x.Request.Reason)
                .NotEmpty().WithErrorCode("adjustment_reason_required");
        });
    }

    private static bool HaveUniqueVariants(IReadOnlyList<CreateInventoryTransactionLineRequest>? lines) =>
        lines is null || lines.Select(x => x.ProductVariantId).Distinct().Count() == lines.Count;

    private static bool FitsPrecision(decimal value, int precision, int scale)
    {
        if (value == decimal.MinValue)
            return false;

        var absolute = Math.Abs(value);
        var integerLimit = Pow10(precision - scale);
        if (absolute >= integerLimit)
            return false;

        var factor = Pow10(scale);
        return decimal.Truncate(absolute * factor) == absolute * factor;
    }

    private static bool TotalCostFitsDatabase(CreateInventoryTransactionLineRequest line)
    {
        if (line.Quantity <= 0 || line.UnitCost < 0)
            return true;

        const decimal maxTotal = 9999999999999999.99m;
        // Quantity precision is validated separately; costs <= 1 cannot overflow its total.
        if (line.UnitCost <= 1)
            return true;

        return line.Quantity <= maxTotal / line.UnitCost;
    }

    private static decimal Pow10(int exponent)
    {
        var value = 1m;
        for (var i = 0; i < exponent; i++)
            value *= 10m;
        return value;
    }
}
