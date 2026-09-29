using FluentValidation;
using OAS.Application.Purchasing.Common;
using OAS.Application.Purchasing.SupplierCatalog.Commands;

namespace OAS.Application.Purchasing.SupplierCatalog.Validators;

public sealed class CreateSupplierCatalogItemCommandValidator : AbstractValidator<CreateSupplierCatalogItemCommand>
{
    public CreateSupplierCatalogItemCommandValidator()
    {
        RuleFor(x => x.Request.SupplierId).NotEmpty();
        RuleFor(x => x.Request.ProductVariantId).NotEmpty();
        RuleFor(x => x.Request.PurchaseUnitId).NotEmpty();
        RuleFor(x => x.Request.UnitConversionFactor).GreaterThan(0);
        RuleFor(x => x.Request.MinimumOrderQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.LeadTimeDays).GreaterThanOrEqualTo(0).When(x => x.Request.LeadTimeDays.HasValue);
        RuleFor(x => x.Request.SupplierProductCode).MaximumLength(64);
        RuleFor(x => x.Request.SupplierProductName).MaximumLength(200);
    }
}

public sealed class UpdateSupplierCatalogItemCommandValidator : AbstractValidator<UpdateSupplierCatalogItemCommand>
{
    public UpdateSupplierCatalogItemCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.PurchaseUnitId).NotEmpty();
        RuleFor(x => x.Request.UnitConversionFactor).GreaterThan(0);
        RuleFor(x => x.Request.MinimumOrderQuantity).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request.LeadTimeDays).GreaterThanOrEqualTo(0).When(x => x.Request.LeadTimeDays.HasValue);
        RuleFor(x => x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");
    }
}

public sealed class CreateSupplierPriceCommandValidator : AbstractValidator<CreateSupplierPriceCommand>
{
    public CreateSupplierPriceCommandValidator()
    {
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.Request.CurrencyId).NotEmpty();
        RuleFor(x => x.Request.UnitPrice).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Request).Must(x => !x.EffectiveTo.HasValue || x.EffectiveTo.Value >= x.EffectiveFrom)
            .WithErrorCode("effective_to_before_effective_from");
        RuleFor(x => x.Request.Notes).MaximumLength(500);
    }
}

public sealed class CloseSupplierPriceCommandValidator : AbstractValidator<CloseSupplierPriceCommand>
{
    public CloseSupplierPriceCommandValidator()
    {
        RuleFor(x => x.CatalogItemId).NotEmpty();
        RuleFor(x => x.PriceId).NotEmpty();
        RuleFor(x => x.Request.RowVersion).Must(PurchasingRowVersion.IsValid).WithErrorCode("row_version_invalid");
    }
}
