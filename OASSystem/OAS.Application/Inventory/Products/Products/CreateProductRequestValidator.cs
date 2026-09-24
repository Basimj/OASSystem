using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Products;

public sealed class CreateProductRequestValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductRequestValidator()
    {
        RuleFor(x => x.ProductCode)
            .NotEmpty().WithErrorCode("product_code_required")
            .MaximumLength(32).WithErrorCode("product_code_max_length");

        RuleFor(x => x.NameAr)
            .NotEmpty().WithErrorCode("product_name_ar_required")
            .MaximumLength(150).WithErrorCode("product_name_ar_max_length");

        RuleFor(x => x.NameEn)
            .MaximumLength(150).WithErrorCode("product_name_en_max_length");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithErrorCode("category_id_required");

        RuleFor(x => x.ProductTypeId)
            .NotEmpty().WithErrorCode("product_type_id_required");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithErrorCode("product_description_max_length");
    }
}
