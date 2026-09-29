using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Lookups;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

using InventoryUnit = OAS.Domain.Entities.Inventory.Unit;

namespace OAS.Application.Sales.Lookups;

internal static class SalesLookupLimit
{
    public static int Normalize(int take)
        => Math.Clamp(take, 1, 50);
}

public sealed class SearchSalesCustomersQueryHandler(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Account, Guid> accounts)
    : IRequestHandler<
        SearchSalesCustomersQuery,
        IReadOnlyList<SalesCustomerLookupDto>>
{
    public async Task<IReadOnlyList<SalesCustomerLookupDto>> Handle(
        SearchSalesCustomersQuery request,
        CancellationToken ct)
    {
        var spec = new Specification<Customer>();
        var search = request.Search?.Trim();

        if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                (
                    x.CustomerCode.Contains(search) ||
                    x.NameAr.Contains(search) ||
                    (
                        x.ContactInfo.Mobile != null &&
                        x.ContactInfo.Mobile.Contains(search)
                    )
                ));
        }
        else
        {
            spec.Where(x => x.IsActive);
        }

        spec
            .AddSort(
                nameof(Customer.NameAr),
                OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .ApplyPaging(
                0,
                SalesLookupLimit.Normalize(request.Take));

        var rows = await customers.ListAsync(spec, ct);

        var result =
            new List<SalesCustomerLookupDto>(rows.Count);

        foreach (var customer in rows)
        {
            var account =
                await accounts.GetByIdAsync(
                    customer.AccountId,
                    ct);

            result.Add(
                new SalesCustomerLookupDto(
                    customer.Id,
                    customer.CustomerCode,
                    customer.AccountId,
                    account?.Code ?? string.Empty,
                    customer.NameAr,
                    customer.ContactInfo.Mobile,
                    customer.IsCreditAllowed,
                    customer.CreditLimit,
                    customer.PaymentTermDays,
                    customer.IsActive));
        }

        return result;
    }
}

public sealed class SearchSalesPrescriptionsQueryHandler(
    IPrescriptionAggregateRepository prescriptions)
    : IRequestHandler<
        SearchSalesPrescriptionsQuery,
        IReadOnlyList<SalesPrescriptionLookupDto>>
{
    public async Task<IReadOnlyList<SalesPrescriptionLookupDto>> Handle(
        SearchSalesPrescriptionsQuery request,
        CancellationToken ct)
    {
        var search = request.Search?.Trim();

        var spec = new Specification<Prescription>();

        if (request.CustomerId.HasValue &&
            !string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.CustomerId == request.CustomerId.Value &&
                x.IsActive &&
                x.PrescriptionCode.Contains(search));
        }
        else if (request.CustomerId.HasValue)
        {
            spec.Where(x =>
                x.CustomerId == request.CustomerId.Value &&
                x.IsActive);
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                x.PrescriptionCode.Contains(search));
        }
        else
        {
            spec.Where(x => x.IsActive);
        }

        spec
            .AddSort(
                nameof(Prescription.PrescriptionDate),
                OAS.Contracts.Common.Pagination.SortDirection.Descending)
            .ApplyPaging(
                0,
                SalesLookupLimit.Normalize(request.Take));

        var rows =
            await prescriptions.ListAsync(spec, ct);

        var result =
            new List<SalesPrescriptionLookupDto>(rows.Count);

        foreach (var row in rows)
        {
            var full =
                await prescriptions.GetAggregateAsync(
                    row.Id,
                    false,
                    ct)
                ?? row;

            var current =
                full.Revisions.SingleOrDefault(
                    x => x.IsCurrent && x.IsActive);

            result.Add(
                new SalesPrescriptionLookupDto(
                    full.Id,
                    full.PrescriptionCode,
                    full.CustomerId,
                    full.PrescriptionDate,
                    (OAS.Contracts.Sales.Enums.PrescriptionStatus)
                    (byte)full.Status,
                    current?.Id,
                    current?.RevisionNumber,
                    full.IsActive));
        }

        return result;
    }
}

public sealed class GetSalesPrescriptionRevisionsQueryHandler(
    IPrescriptionAggregateRepository prescriptions,
    IReadRepository<PrescriptionRevision, Guid> revisions)
    : IRequestHandler<
        GetSalesPrescriptionRevisionsQuery,
        IReadOnlyList<SalesPrescriptionRevisionLookupDto>>
{
    public async Task<IReadOnlyList<SalesPrescriptionRevisionLookupDto>> Handle(
        GetSalesPrescriptionRevisionsQuery request,
        CancellationToken ct)
    {
        var prescription =
            await prescriptions.GetAggregateAsync(
                request.PrescriptionId,
                false,
                ct);

        if (prescription is null)
            return [];

        var spec =
            new Specification<PrescriptionRevision>()
                .Where(x =>
                    x.PrescriptionId == request.PrescriptionId &&
                    x.IsActive)
                .AddSort(
                    nameof(PrescriptionRevision.RevisionNumber),
                    OAS.Contracts.Common.Pagination.SortDirection.Descending);

        var rows =
            await revisions.ListAsync(spec, ct);

        return rows
            .Select(x =>
                new SalesPrescriptionRevisionLookupDto(
                    x.Id,
                    x.PrescriptionId,
                    prescription.PrescriptionCode,
                    x.RevisionNumber,
                    x.EffectiveDate,
                    x.IsCurrent,
                    x.IsActive))
            .ToArray();
    }
}

public sealed class SearchSalesProductVariantsQueryHandler(
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<Product, Guid> products,
    IReadRepository<InventoryUnit, Guid> units,
    IReadRepository<LensDetails, Guid> lensDetails)
    : IRequestHandler<
        SearchSalesProductVariantsQuery,
        IReadOnlyList<SalesProductVariantLookupDto>>
{
    public async Task<IReadOnlyList<SalesProductVariantLookupDto>> Handle(
        SearchSalesProductVariantsQuery request,
        CancellationToken ct)
    {
        var search = request.Search?.Trim();

        var productIds = new HashSet<Guid>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var productSpec =
                new Specification<Product>()
                    .Where(x =>
                        x.IsActive &&
                        (
                            x.ProductCode.Contains(search) ||
                            x.NameAr.Contains(search)
                        ))
                    .ApplyPaging(0, 100);

            foreach (var product
                     in await products.ListAsync(productSpec, ct))
            {
                productIds.Add(product.Id);
            }
        }

        var spec =
            new Specification<ProductVariant>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                (
                    x.SKU.Contains(search) ||
                    (
                        x.Barcode != null &&
                        x.Barcode.Contains(search)
                    ) ||
                    (
                        x.VariantName != null &&
                        x.VariantName.Contains(search)
                    ) ||
                    productIds.Contains(x.ProductId)
                ));
        }
        else
        {
            spec.Where(x => x.IsActive);
        }

        spec
            .AddSort(
                nameof(ProductVariant.SKU),
                OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .ApplyPaging(
                0,
                SalesLookupLimit.Normalize(request.Take));

        var rows =
            await variants.ListAsync(spec, ct);

        var result =
            new List<SalesProductVariantLookupDto>(rows.Count);

        foreach (var variant in rows)
        {
            var product =
                await products.GetByIdAsync(
                    variant.ProductId,
                    ct);

            if (product is null ||
                !product.IsActive)
            {
                continue;
            }

            string? unitName = null;

            if (variant.UnitId.HasValue)
            {
                var unit =
                    await units.GetByIdAsync(
                        variant.UnitId.Value,
                        ct);

                unitName = unit?.NameAr;
            }

            var lens =
                (
                    await lensDetails.ListAsync(
                        new Specification<LensDetails>()
                            .Where(x =>
                                x.ProductId == product.Id),
                        ct)
                ).FirstOrDefault();

            result.Add(
                new SalesProductVariantLookupDto(
                    variant.Id,
                    product.Id,
                    product.ProductCode,
                    product.NameAr,
                    variant.SKU,
                    variant.Barcode,
                    variant.VariantName,
                    variant.Color,
                    variant.Size,
                    variant.UnitId,
                    unitName,
                    variant.SellingPrice,
                    product.IsStockItem,
                    lens?.IsPrescriptionLens ?? false,
                    lens?.SphereMin,
                    lens?.SphereMax,
                    lens?.CylinderMin,
                    lens?.CylinderMax,
                    lens?.AddMin,
                    lens?.AddMax,
                    variant.IsActive));
        }

        return result;
    }
}

public sealed class SearchSalesWarehousesQueryHandler(
    IReadRepository<Warehouse, Guid> warehouses,
    IInventoryBalanceRepository balances)
    : IRequestHandler<
        SearchSalesWarehousesQuery,
        IReadOnlyList<SalesWarehouseLookupDto>>
{
    public async Task<IReadOnlyList<SalesWarehouseLookupDto>> Handle(
        SearchSalesWarehousesQuery request,
        CancellationToken ct)
    {
        var search = request.Search?.Trim();

        var spec =
            new Specification<Warehouse>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                (
                    x.Code.Contains(search) ||
                    x.NameAr.Contains(search)
                ));
        }
        else
        {
            spec.Where(x => x.IsActive);
        }

        spec
            .AddSort(
                nameof(Warehouse.IsDefault),
                OAS.Contracts.Common.Pagination.SortDirection.Descending)
            .AddSort(
                nameof(Warehouse.NameAr),
                OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .ApplyPaging(
                0,
                SalesLookupLimit.Normalize(request.Take));

        var rows =
            await warehouses.ListAsync(spec, ct);

        var result =
            new List<SalesWarehouseLookupDto>(rows.Count);

        foreach (var warehouse in rows)
        {
            InventoryBalance? balance = null;

            if (request.ProductVariantId.HasValue)
            {
                balance =
                    await balances.GetByWarehouseAndVariantAsync(
                        warehouse.Id,
                        request.ProductVariantId.Value,
                        ct);
            }

            result.Add(
                new SalesWarehouseLookupDto(
                    warehouse.Id,
                    warehouse.Code,
                    warehouse.NameAr,
                    warehouse.IsDefault,
                    warehouse.IsActive,
                    balance?.OnHandQuantity,
                    balance?.ReservedQuantity,
                    balance?.AvailableQuantity));
        }

        return result;
    }
}

public sealed class SearchSalesCurrenciesQueryHandler(
    IReadRepository<Currency, Guid> currencies,
    IExchangeRateResolver rates)
    : IRequestHandler<
        SearchSalesCurrenciesQuery,
        IReadOnlyList<SalesCurrencyLookupDto>>
{
    public async Task<IReadOnlyList<SalesCurrencyLookupDto>> Handle(
        SearchSalesCurrenciesQuery request,
        CancellationToken ct)
    {
        var search = request.Search?.Trim();

        var spec =
            new Specification<Currency>();

        /*
         * ‰⁄—÷ «·⁄„·«  «·‰‘ÿ… ﬂ·Â«.
         *
         * ÊÃÊœ ”⁄— ’—› ·Ì” ‘—ÿ« ·≈ŸÂ«— «·⁄„·… ›Ì «·ﬁ«∆„….
         * ≈–« ·„ ÌÊÃœ «·”⁄—°  —Ã⁄ «·⁄„·… Ê·ﬂ‰ IsSelectable/HasRate
         * ÌﬂÊ‰ False ⁄·Ï „” ÊÏ DTO.
         */
        if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                (
                    x.Code.Contains(search) ||
                    x.NameAr.Contains(search)
                ));
        }
        else
        {
            spec.Where(x => x.IsActive);
        }

        spec
            .AddSort(
                nameof(Currency.Code),
                OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .ApplyPaging(
                0,
                SalesLookupLimit.Normalize(request.Take));

        var rows =
            await currencies.ListAsync(spec, ct);

        var result =
            new List<SalesCurrencyLookupDto>(rows.Count);

        foreach (var currency in rows)
        {
            try
            {
                /*
                 * «·⁄„·… «·√”«”Ì… ” ⁄Êœ „‰ ExchangeRateResolver
                 * »”⁄— 1 »œÊ‰ «·Õ«Ã… ≈·Ï ”Ã· ExchangeRate.
                 *
                 * «·⁄„·… «·√Ã‰»Ì… –«  «·”⁄— «·›⁄«· ” ⁄Êœ »«·”⁄—
                 * «·„ÊÃÊœ ›Ì √Ê ﬁ»· DocumentDate.
                 */
                var rate =
                    await rates.ResolveAsync(
                        currency.Id,
                        request.DocumentDate,
                        ExchangeRateType.Accounting,
                        cancellationToken: ct);

                result.Add(
                    new SalesCurrencyLookupDto(
                        currency.Id,
                        currency.Code,
                        currency.NameAr,
                        currency.Symbol,
                        currency.DecimalPlaces,
                        rate.Rate,
                        rate.RateDate,
                        rate.IsBaseCurrency,
                        currency.IsActive,
                        HasEffectiveExchangeRate: true,
                        AvailabilityMessage: null));
            }
            catch (ConflictException ex)
                when (ex.Code == "exchange_rate_not_found")
            {
                /*
                 * „Â„:
                 *
                 * ⁄œ„ ÊÃÊœ ”⁄— ’—› ·Ì” Œÿ√ Lookup.
                 * ·« ‰Êﬁ› «·ﬁ«∆„… Ê·« ‰—„Ì Conflict ··„” Œœ„.
                 *
                 * ‰⁄Ìœ «·⁄„·… ‰›”Â« ··‹Client Ê·ﬂ‰ »Õ«·…
                 * HasEffectiveExchangeRate = false.
                 *
                 * «·‹Client ”Ì⁄—÷Â« Disabled.
                 */
                result.Add(
                    new SalesCurrencyLookupDto(
                        currency.Id,
                        currency.Code,
                        currency.NameAr,
                        currency.Symbol,
                        currency.DecimalPlaces,

                        // ·« Ì” Œœ„ ·√‰ «·⁄‰’— ”ÌﬂÊ‰ Disabled.
                        EffectiveExchangeRate: 0m,

                        // ›ﬁÿ ﬁÌ„… „—Ã⁄Ì… ··‹DTO ⁄‰œ ⁄œ„ ÊÃÊœ ”⁄—.
                        EffectiveRateDate: request.DocumentDate,

                        IsBaseCurrency: false,
                        IsActive: currency.IsActive,
                        HasEffectiveExchangeRate: false,
                        AvailabilityMessage:
                            $"·« ÌÊÃœ ”⁄— ’—› ›⁄«· Õ Ï {request.DocumentDate:yyyy-MM-dd}"));
            }
        }

        return result;
    }
}

public sealed class SearchCustomerOrdersLookupQueryHandler(
    IReadRepository<CustomerOrder, Guid> orders)
    : IRequestHandler<
        SearchCustomerOrdersLookupQuery,
        IReadOnlyList<CustomerOrderLookupDto>>
{
    public async Task<IReadOnlyList<CustomerOrderLookupDto>> Handle(
        SearchCustomerOrdersLookupQuery request,
        CancellationToken ct)
    {
        var search = request.Search?.Trim();

        var spec =
            new Specification<CustomerOrder>();

        if (request.CustomerId.HasValue &&
            !string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.CustomerId == request.CustomerId.Value &&
                x.IsActive &&
                x.OrderCode.Contains(search));
        }
        else if (request.CustomerId.HasValue)
        {
            spec.Where(x =>
                x.CustomerId == request.CustomerId.Value &&
                x.IsActive);
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                x.OrderCode.Contains(search));
        }
        else
        {
            spec.Where(x => x.IsActive);
        }

        spec
            .AddSort(
                nameof(CustomerOrder.OrderDate),
                OAS.Contracts.Common.Pagination.SortDirection.Descending)
            .ApplyPaging(
                0,
                SalesLookupLimit.Normalize(request.Take));

        var rows =
            await orders.ListAsync(spec, ct);

        return rows
            .Select(x =>
                new CustomerOrderLookupDto(
                    x.Id,
                    x.OrderCode,
                    x.CustomerId,
                    x.OrderDate,
                    (OAS.Contracts.Sales.Enums.CustomerOrderStatus)
                    (byte)x.Status,
                    x.TotalAmount,
                    x.CurrencyCodeSnapshot,
                    Convert.ToBase64String(x.RowVersion)))
            .ToArray();
    }
}