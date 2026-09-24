using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.Products.Commands.CreateStockProduct;

public sealed class CreateStockProductCommandValidator : AbstractValidator<CreateStockProductCommand>
{
    public CreateStockProductCommandValidator()
    {
        RuleFor(x => x.Request.Product).NotNull();

        When(x => x.Request.Product is not null, () =>
        {
            RuleFor(x => x.Request.Product.ProductCode)
                .NotEmpty().WithErrorCode("product_code_required")
                .MaximumLength(32).WithErrorCode("product_code_max_length");
            RuleFor(x => x.Request.Product.NameAr)
                .NotEmpty().WithErrorCode("product_name_ar_required")
                .MaximumLength(150).WithErrorCode("product_name_ar_max_length");
            RuleFor(x => x.Request.Product.NameEn)
                .MaximumLength(150).WithErrorCode("product_name_en_max_length");
            RuleFor(x => x.Request.Product.CategoryId)
                .NotEmpty().WithErrorCode("category_id_required");
            RuleFor(x => x.Request.Product.ProductTypeId)
                .NotEmpty().WithErrorCode("product_type_id_required");
            RuleFor(x => x.Request.Product.Description)
                .MaximumLength(500).WithErrorCode("product_description_max_length");

            When(x => x.Request.Product.IsStockItem, () =>
            {
                RuleFor(x => x.Request.Variant)
                    .NotNull().WithErrorCode("stock_product_variant_required");
            });
        });

        When(x => x.Request.Variant is not null, () =>
        {
            RuleFor(x => x.Request.Variant!.SKU)
                .NotEmpty().WithErrorCode("sku_required")
                .MaximumLength(64).WithErrorCode("sku_max_length");
            RuleFor(x => x.Request.Variant!.Barcode)
                .MaximumLength(64).WithErrorCode("barcode_max_length");
            RuleFor(x => x.Request.Variant!.VariantName)
                .MaximumLength(100).WithErrorCode("variant_name_max_length");
            RuleFor(x => x.Request.Variant!.Color)
                .MaximumLength(50).WithErrorCode("color_max_length");
            RuleFor(x => x.Request.Variant!.Size)
                .MaximumLength(50).WithErrorCode("size_max_length");
            RuleFor(x => x.Request.Variant!.PurchasePrice)
                .GreaterThanOrEqualTo(0).WithErrorCode("purchase_price_invalid");
            RuleFor(x => x.Request.Variant!.SellingPrice)
                .GreaterThanOrEqualTo(0).WithErrorCode("selling_price_invalid");
        });

        When(x => x.Request.FrameDetails is not null, () =>
        {
            RuleFor(x => x.Request.FrameDetails!.Model)
                .NotEmpty().WithErrorCode("frame_model_required")
                .MaximumLength(100).WithErrorCode("frame_model_max_length");
            RuleFor(x => x.Request.FrameDetails!.Material).MaximumLength(50).WithErrorCode("frame_material_max_length");
            RuleFor(x => x.Request.FrameDetails!.RimType).MaximumLength(50).WithErrorCode("frame_rim_type_max_length");
            RuleFor(x => x.Request.FrameDetails!.Gender).MaximumLength(20).WithErrorCode("frame_gender_max_length");
            RuleFor(x => x.Request.FrameDetails!.Shape).MaximumLength(50).WithErrorCode("frame_shape_max_length");
            RuleFor(x => x.Request.FrameDetails!.TempleLength).Must(FitsNullableDecimal6x2).WithErrorCode("frame_temple_length_invalid");
            RuleFor(x => x.Request.FrameDetails!.BridgeSize).Must(FitsNullableDecimal6x2).WithErrorCode("frame_bridge_size_invalid");
            RuleFor(x => x.Request.FrameDetails!.LensWidth).Must(FitsNullableDecimal6x2).WithErrorCode("frame_lens_width_invalid");
        });

        When(x => x.Request.LensDetails is not null, () =>
        {
            RuleFor(x => x.Request.LensDetails!.LensType)
                .NotEmpty().WithErrorCode("lens_type_required")
                .MaximumLength(50).WithErrorCode("lens_type_max_length");
            RuleFor(x => x.Request.LensDetails!.Material).MaximumLength(50).WithErrorCode("lens_material_max_length");
            RuleFor(x => x.Request.LensDetails!.Coating).MaximumLength(50).WithErrorCode("lens_coating_max_length");
            RuleFor(x => x.Request.LensDetails!.RefractiveIndex).Must(x => FitsNullableDecimal(x, 5, 3)).WithErrorCode("lens_refractive_index_invalid");
            RuleFor(x => x.Request.LensDetails!.SphereMin).Must(FitsNullableDecimal6x2).WithErrorCode("lens_sphere_min_invalid");
            RuleFor(x => x.Request.LensDetails!.SphereMax).Must(FitsNullableDecimal6x2).WithErrorCode("lens_sphere_max_invalid");
            RuleFor(x => x.Request.LensDetails!.CylinderMin).Must(FitsNullableDecimal6x2).WithErrorCode("lens_cylinder_min_invalid");
            RuleFor(x => x.Request.LensDetails!.CylinderMax).Must(FitsNullableDecimal6x2).WithErrorCode("lens_cylinder_max_invalid");
            RuleFor(x => x.Request.LensDetails!.AddMin).Must(FitsNullableDecimal6x2).WithErrorCode("lens_add_min_invalid");
            RuleFor(x => x.Request.LensDetails!.AddMax).Must(FitsNullableDecimal6x2).WithErrorCode("lens_add_max_invalid");
        });

        When(x => x.Request.OpeningInventory is not null && x.Request.Product is not null, () =>
        {
            RuleFor(x => x.Request.Product.IsStockItem)
                .Equal(true).WithErrorCode("opening_inventory_requires_stock_product");
            RuleFor(x => x.Request.Variant)
                .NotNull().WithErrorCode("opening_inventory_variant_required");
            RuleFor(x => x.Request.OpeningInventory!.WarehouseId)
                .NotEmpty().WithErrorCode("opening_warehouse_required");
            RuleFor(x => x.Request.OpeningInventory!.Quantity)
                .GreaterThan(0).WithErrorCode("opening_quantity_must_be_positive");
            RuleFor(x => x.Request.OpeningInventory!.UnitCost)
                .GreaterThanOrEqualTo(0).WithErrorCode("opening_unit_cost_invalid");
        });
    }
    private static bool FitsNullableDecimal6x2(decimal? value) => FitsNullableDecimal(value, 6, 2);

    private static bool FitsNullableDecimal(decimal? value, int precision, int scale)
    {
        if (!value.HasValue) return true;
        var absolute = Math.Abs(value.Value);
        var max = Pow10(precision - scale);
        if (absolute >= max) return false;
        var factor = Pow10(scale);
        return decimal.Truncate(absolute * factor) == absolute * factor;
    }

    private static decimal Pow10(int exponent)
    {
        var value = 1m;
        for (var i = 0; i < exponent; i++) value *= 10m;
        return value;
    }

}
