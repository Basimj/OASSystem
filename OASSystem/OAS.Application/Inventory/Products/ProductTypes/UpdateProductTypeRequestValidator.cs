using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.ProductTypes;

public sealed class UpdateProductTypeRequestValidator : AbstractValidator<UpdateProductTypeRequest>
{
    public UpdateProductTypeRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode("product_type_code_required")
            .MaximumLength(32).WithErrorCode("product_type_code_max_length");

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode("product_type_name_required")
            .MaximumLength(100).WithErrorCode("product_type_name_max_length");

    }
}
