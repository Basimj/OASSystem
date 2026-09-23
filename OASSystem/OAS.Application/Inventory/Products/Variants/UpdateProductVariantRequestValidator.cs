using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Variants;

public sealed class UpdateProductVariantRequestValidator : AbstractValidator<UpdateProductVariantRequest>
{
    public UpdateProductVariantRequestValidator()
    {
        RuleFor(x => x.SKU)
            .NotEmpty().WithErrorCode("sku_required")
            .MaximumLength(64).WithErrorCode("sku_max_length");

        RuleFor(x => x.Barcode)
            .MaximumLength(64).WithErrorCode("barcode_max_length");

        RuleFor(x => x.VariantName)
            .MaximumLength(100).WithErrorCode("variant_name_max_length");

        RuleFor(x => x.Color)
            .MaximumLength(50).WithErrorCode("color_max_length");

        RuleFor(x => x.Size)
            .MaximumLength(50).WithErrorCode("size_max_length");

        RuleFor(x => x.PurchasePrice)
            .GreaterThanOrEqualTo(0).WithErrorCode("purchase_price_invalid");

        RuleFor(x => x.SellingPrice)
            .GreaterThanOrEqualTo(0).WithErrorCode("selling_price_invalid");
    }
}
