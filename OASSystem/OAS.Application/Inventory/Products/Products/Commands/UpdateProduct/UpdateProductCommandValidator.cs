using FluentValidation;

namespace OAS.Application.Inventory.Products.Products.Commands.UpdateProduct;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.ProductId).NotEmpty().WithErrorCode("product_id_required");
        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new UpdateProductRequestValidator());
    }
}
