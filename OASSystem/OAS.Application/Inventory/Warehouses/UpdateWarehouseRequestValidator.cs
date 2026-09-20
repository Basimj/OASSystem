using FluentValidation;
using OAS.Contracts.Inventory.Warehouses;

namespace OAS.Application.Inventory.Warehouses;

public sealed class UpdateWarehouseRequestValidator : AbstractValidator<UpdateWarehouseRequest>
{
    public UpdateWarehouseRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode("warehouse_name_ar_required")
            .MaximumLength(150).WithErrorCode("warehouse_name_ar_max_length");

        RuleFor(x => x.NameEn)
            .MaximumLength(150).WithErrorCode("warehouse_name_en_max_length");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("warehouse_description_max_length");
    }
}
