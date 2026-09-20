using FluentValidation;

namespace OAS.Application.Inventory.StockCounts.Commands.CreateStockCount;

public sealed class CreateStockCountCommandValidator : AbstractValidator<CreateStockCountCommand>
{
    public CreateStockCountCommandValidator()
    {
        RuleFor(x => x.Request.WarehouseId)
            .NotEmpty().WithErrorCode("warehouse_id_required");

        RuleFor(x => x.Request.CountDate)
            .NotEmpty().WithErrorCode("count_date_required");

        RuleFor(x => x.Request.Notes)
            .MaximumLength(500).WithErrorCode("stock_count_notes_max_length");
    }
}
