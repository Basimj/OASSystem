using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Categories;

public sealed class CreateProductCategoryRequestValidator : AbstractValidator<CreateProductCategoryRequest>
{
    public CreateProductCategoryRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("category_code_required")
            .MaximumLength(32).WithErrorCode("category_code_max_length");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("category_name_required")
            .MaximumLength(100).WithErrorCode("category_name_max_length");


        RuleFor(x => x.ProductTypeId)
            .NotEmpty().WithErrorCode("product_type_id_required");
    }
}
