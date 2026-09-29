using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Purchasing.Rules;

namespace OAS.Domain.Purchasing.Entities;

public sealed class SupplierPriceHistory : AuditableEntity<Guid>
{
    private SupplierPriceHistory() { }
    private SupplierPriceHistory(Guid id, Guid supplierCatalogItemId, Guid currencyId, decimal unitPrice, DateOnly effectiveFrom,
        DateOnly? effectiveTo, bool isCurrent, string? notes)
    {
        Id = PurchasingDomainGuard.Required(id, "Supplier price history id");
        SupplierCatalogItemId = PurchasingDomainGuard.Required(supplierCatalogItemId, "Supplier catalog item id");
        CurrencyId = PurchasingDomainGuard.Required(currencyId, "Currency id");
        SetPrice(unitPrice, effectiveFrom, effectiveTo, isCurrent, notes);
    }
    public Guid SupplierCatalogItemId { get; private set; }
    public Guid CurrencyId { get; private set; }
    public decimal UnitPrice { get; private set; }
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public bool IsCurrent { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static SupplierPriceHistory Create(Guid id, Guid supplierCatalogItemId, Guid currencyId, decimal unitPrice, DateOnly effectiveFrom,
        DateOnly? effectiveTo, bool isCurrent, string? notes) => new(id, supplierCatalogItemId, currencyId, unitPrice, effectiveFrom, effectiveTo, isCurrent, notes);

    public void Close(DateOnly effectiveTo)
    {
        if (effectiveTo < EffectiveFrom) throw new DomainException("Effective to date cannot be before effective from date.");
        EffectiveTo = effectiveTo;
        IsCurrent = false;
    }

    private void SetPrice(decimal unitPrice, DateOnly effectiveFrom, DateOnly? effectiveTo, bool isCurrent, string? notes)
    {
        PurchasingDomainGuard.NonNegative(unitPrice, "Unit price");
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom) throw new DomainException("Effective to date cannot be before effective from date.");
        UnitPrice = unitPrice;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        IsCurrent = isCurrent;
        Notes = PurchasingDomainGuard.Optional(notes, 500, "Price notes");
    }
}
