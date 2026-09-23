using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Categories;

public sealed class UpdateProductCategoryRequestValidator : AbstractValidator<UpdateProductCategoryRequest>
{
    public UpdateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("category_code_required")
            .MaximumLength(32).WithErrorCode("category_code_max_length");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode("category_name_ar_required")
            .MaximumLength(100).WithErrorCode("category_name_ar_max_length");

        RuleFor(x => x.NameEn)
            .MaximumLength(100).WithErrorCode("category_name_en_max_length");
    }
}
