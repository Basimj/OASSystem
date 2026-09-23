using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Brands;

public sealed class UpdateBrandRequestValidator : AbstractValidator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("brand_code_required")
            .MaximumLength(32).WithErrorCode("brand_code_max_length");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("brand_name_required")
            .MaximumLength(100).WithErrorCode("brand_name_max_length");
    }
}
