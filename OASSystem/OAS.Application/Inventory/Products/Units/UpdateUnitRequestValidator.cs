using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Units;

public sealed class UpdateUnitRequestValidator : AbstractValidator<UpdateUnitRequest>
{
    public UpdateUnitRequestValidator()
    {
        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode("unit_name_ar_required")
            .MaximumLength(100).WithErrorCode("unit_name_ar_max_length");

        RuleFor(x => x.NameEn)
            .MaximumLength(100).WithErrorCode("unit_name_en_max_length");
    }
}
