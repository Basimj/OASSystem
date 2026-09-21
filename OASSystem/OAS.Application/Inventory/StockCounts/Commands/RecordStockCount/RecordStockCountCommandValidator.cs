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
            .GreaterThanOrEqualTo(0).WithErrorCode("counted_quantity_invalid")
            .Must(value => FitsPrecision(value, 18, 3))
            .WithErrorCode("counted_quantity_precision_invalid");

        RuleFor(x => x.Request.CountedBy)
            .MaximumLength(64).WithErrorCode("counted_by_too_long");
    }

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

    private static decimal Pow10(int exponent)
    {
        var value = 1m;
        for (var i = 0; i < exponent; i++)
            value *= 10m;
        return value;
    }
}
