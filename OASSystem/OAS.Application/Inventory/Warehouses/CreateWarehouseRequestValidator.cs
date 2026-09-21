using FluentValidation;
using OAS.Contracts.Inventory.Warehouses;

namespace OAS.Application.Inventory.Warehouses;

public sealed class CreateWarehouseRequestValidator : AbstractValidator<CreateWarehouseRequest>
{
    public CreateWarehouseRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("warehouse_code_required")
            .MaximumLength(32).WithErrorCode("warehouse_code_max_length");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode("warehouse_name_ar_required")
            .MaximumLength(100).WithErrorCode("warehouse_name_ar_max_length");

        RuleFor(x => x.NameEn)
            .MaximumLength(100).WithErrorCode("warehouse_name_en_max_length");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("warehouse_description_max_length");
    }
}
