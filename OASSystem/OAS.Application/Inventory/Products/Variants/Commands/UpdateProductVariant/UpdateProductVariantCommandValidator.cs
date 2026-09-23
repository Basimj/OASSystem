using FluentValidation;

namespace OAS.Application.Inventory.Products.Variants.Commands.UpdateProductVariant;

public sealed class UpdateProductVariantCommandValidator : AbstractValidator<UpdateProductVariantCommand>
{
    public UpdateProductVariantCommandValidator()
    {
        RuleFor(x => x.VariantId).NotEmpty().WithErrorCode("product_variant_id_required");
        RuleFor(x => x.Request).SetValidator(new UpdateProductVariantRequestValidator());
    }
}
