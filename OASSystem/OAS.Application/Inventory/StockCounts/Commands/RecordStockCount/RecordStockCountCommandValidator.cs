using FluentValidation;

namespace OAS.Application.Inventory.StockCounts.Commands.RecordStockCount;

public sealed class RecordStockCountCommandValidator : AbstractValidator<RecordStockCountCommand>
{
    public RecordStockCountCommandValidator()
    {
        RuleFor(x => x.StockCountId)
            .NotEmpty().WithErrorCode("stock_count_id_required");

        RuleFor(x => x.LineId)
            .NotEmpty().WithErrorCode("stock_count_line_id_required");

        RuleFor(x => x.Request.CountedQuantity)
            .GreaterThanOrEqualTo(0).WithErrorCode("counted_quantity_invalid");
    }
}
