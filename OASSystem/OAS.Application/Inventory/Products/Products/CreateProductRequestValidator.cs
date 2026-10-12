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

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("product_name_required")
            .MaximumLength(200).WithErrorCode("product_name_max_length");


        RuleFor(x => x.CategoryId)
            .NotEmpty().WithErrorCode("category_id_required");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithErrorCode("product_description_max_length");
    }
}
