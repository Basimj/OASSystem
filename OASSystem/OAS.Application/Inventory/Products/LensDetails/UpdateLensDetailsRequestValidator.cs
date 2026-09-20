using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.LensDetails;

public sealed class UpdateLensDetailsRequestValidator : AbstractValidator<UpdateLensDetailsRequest>
{
    public UpdateLensDetailsRequestValidator()
    {
        RuleFor(x => x.LensType)
            .NotEmpty().WithErrorCode("lens_type_required")
            .MaximumLength(50).WithErrorCode("lens_type_max_length");

        RuleFor(x => x.Material)
            .MaximumLength(50).WithErrorCode("lens_material_max_length");

        RuleFor(x => x.Coating)
            .MaximumLength(50).WithErrorCode("lens_coating_max_length");
    }
}
