using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Repositories;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Common;
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

public sealed class SearchSalesProductTypesQueryHandler(
    IReadRepository<ProductType, Guid> productTypes)
    : IRequestHandler<
        SearchSalesProductTypesQuery,
        IReadOnlyList<SalesProductTypeLookupDto>>
{
    public async Task<IReadOnlyList<SalesProductTypeLookupDto>> Handle(
        SearchSalesProductTypesQuery request,
        CancellationToken ct)
    {
        var search = request.Search?.Trim();
        var spec = new Specification<ProductType>();

        if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                (x.Code.Contains(search) || x.NameAr.Contains(search)));
        }
        else
        {
            spec.Where(x => x.IsActive);
        }

        spec
            .AddSort(
                nameof(ProductType.NameAr),
                OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .ApplyPaging(0, SalesLookupLimit.Normalize(request.Take));

        var rows = await productTypes.ListAsync(spec, ct);

        return rows
            .Select(x => new SalesProductTypeLookupDto(
                x.Id,
                x.Code,
                x.NameAr,
                x.SystemKey,
                (OAS.Contracts.Sales.Enums.SalesLineType)(byte)SalesProductTypeMapping.ToLineType(x.SystemKey),
                x.IsActive))
            .ToArray();
    }
}

public sealed class SearchSalesProductCategoriesQueryHandler(
    IReadRepository<ProductCategory, Guid> categories,
    IReadRepository<Product, Guid> products)
    : IRequestHandler<
        SearchSalesProductCategoriesQuery,
        IReadOnlyList<SalesProductCategoryLookupDto>>
{
    public async Task<IReadOnlyList<SalesProductCategoryLookupDto>> Handle(
        SearchSalesProductCategoriesQuery request,
        CancellationToken ct)
    {
        HashSet<Guid>? allowedCategoryIds = null;
        if (request.ProductTypeId.HasValue)
        {
            var matchingProducts = await products.ListAsync(
                new Specification<Product>()
                    .Where(x => x.IsActive && x.ProductTypeId == request.ProductTypeId.Value),
                ct);

            allowedCategoryIds = matchingProducts
                .Select(x => x.CategoryId)
                .ToHashSet();

            if (allowedCategoryIds.Count == 0)
                return [];
        }

        var search = request.Search?.Trim();
        var spec = new Specification<ProductCategory>();

        if (allowedCategoryIds is not null && !string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                allowedCategoryIds.Contains(x.Id) &&
                (x.Code.Contains(search) || x.NameAr.Contains(search)));
        }
        else if (allowedCategoryIds is not null)
        {
            spec.Where(x => x.IsActive && allowedCategoryIds.Contains(x.Id));
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                (x.Code.Contains(search) || x.NameAr.Contains(search)));
        }
        else
        {
            spec.Where(x => x.IsActive);
        }

        spec
            .AddSort(
                nameof(ProductCategory.NameAr),
                OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .ApplyPaging(0, SalesLookupLimit.Normalize(request.Take));

        var rows = await categories.ListAsync(spec, ct);

        return rows
            .Select(x => new SalesProductCategoryLookupDto(
                x.Id, x.Code, x.NameAr, x.ParentCategoryId, x.IsActive))
            .ToArray();
    }
}

public sealed class SearchSalesProductVariantsQueryHandler(
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<Product, Guid> products,
    IReadRepository<ProductType, Guid> productTypes,
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

        HashSet<Guid>? filteredProductIds = null;
        if (request.ProductTypeId.HasValue || request.CategoryId.HasValue)
        {
            var productFilter = new Specification<Product>();

            if (request.ProductTypeId.HasValue && request.CategoryId.HasValue)
            {
                productFilter.Where(x =>
                    x.IsActive &&
                    x.ProductTypeId == request.ProductTypeId.Value &&
                    x.CategoryId == request.CategoryId.Value);
            }
            else if (request.ProductTypeId.HasValue)
            {
                productFilter.Where(x =>
                    x.IsActive &&
                    x.ProductTypeId == request.ProductTypeId.Value);
            }
            else
            {
                productFilter.Where(x =>
                    x.IsActive &&
                    x.CategoryId == request.CategoryId!.Value);
            }

            var filteredProducts = await products.ListAsync(productFilter, ct);
            filteredProductIds = filteredProducts.Select(x => x.Id).ToHashSet();

            if (filteredProductIds.Count == 0)
                return [];
        }

        var matchingProductIds = new HashSet<Guid>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var productSpec = new Specification<Product>();

            if (request.ProductTypeId.HasValue && request.CategoryId.HasValue)
            {
                productSpec.Where(x =>
                    x.IsActive &&
                    x.ProductTypeId == request.ProductTypeId.Value &&
                    x.CategoryId == request.CategoryId.Value &&
                    (x.ProductCode.Contains(search) || x.NameAr.Contains(search)));
            }
            else if (request.ProductTypeId.HasValue)
            {
                productSpec.Where(x =>
                    x.IsActive &&
                    x.ProductTypeId == request.ProductTypeId.Value &&
                    (x.ProductCode.Contains(search) || x.NameAr.Contains(search)));
            }
            else if (request.CategoryId.HasValue)
            {
                productSpec.Where(x =>
                    x.IsActive &&
                    x.CategoryId == request.CategoryId.Value &&
                    (x.ProductCode.Contains(search) || x.NameAr.Contains(search)));
            }
            else
            {
                productSpec.Where(x =>
                    x.IsActive &&
                    (x.ProductCode.Contains(search) || x.NameAr.Contains(search)));
            }

            productSpec.ApplyPaging(0, 100);
            foreach (var product in await products.ListAsync(productSpec, ct))
                matchingProductIds.Add(product.Id);
        }

        var spec = new Specification<ProductVariant>();

        if (filteredProductIds is not null && !string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                filteredProductIds.Contains(x.ProductId) &&
                (
                    x.SKU.Contains(search) ||
                    (x.Barcode != null && x.Barcode.Contains(search)) ||
                    (x.VariantName != null && x.VariantName.Contains(search)) ||
                    matchingProductIds.Contains(x.ProductId)
                ));
        }
        else if (filteredProductIds is not null)
        {
            spec.Where(x => x.IsActive && filteredProductIds.Contains(x.ProductId));
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            spec.Where(x =>
                x.IsActive &&
                (
                    x.SKU.Contains(search) ||
                    (x.Barcode != null && x.Barcode.Contains(search)) ||
                    (x.VariantName != null && x.VariantName.Contains(search)) ||
                    matchingProductIds.Contains(x.ProductId)
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
            .ApplyPaging(0, SalesLookupLimit.Normalize(request.Take));

        var rows = await variants.ListAsync(spec, ct);
        var result = new List<SalesProductVariantLookupDto>(rows.Count);

        foreach (var variant in rows)
        {
            var product = await products.GetByIdAsync(variant.ProductId, ct);

            if (product is null || !product.IsActive)
                continue;

            if (request.ProductTypeId.HasValue && product.ProductTypeId != request.ProductTypeId.Value)
                continue;

            if (request.CategoryId.HasValue && product.CategoryId != request.CategoryId.Value)
                continue;

            var productType = await productTypes.GetByIdAsync(product.ProductTypeId, ct);
            if (productType is null || !productType.IsActive)
                continue;

            string? unitName = null;
            if (variant.UnitId.HasValue)
            {
                var unit = await units.GetByIdAsync(variant.UnitId.Value, ct);
                unitName = unit?.NameAr;
            }

            var lens = (await lensDetails.ListAsync(
                new Specification<LensDetails>()
                    .Where(x => x.ProductId == product.Id),
                ct)).FirstOrDefault();

            result.Add(new SalesProductVariantLookupDto(
                variant.Id,
                product.Id,
                productType.Id,
                productType.Code,
                productType.NameAr,
                productType.SystemKey,
                (OAS.Contracts.Sales.Enums.SalesLineType)(byte)SalesProductTypeMapping.ToLineType(productType.SystemKey),
                product.CategoryId,
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

public sealed class SearchSalesCashAccountsQueryHandler(
    IReadRepository<CashAccount, Guid> cashAccounts)
    : IRequestHandler<SearchSalesCashAccountsQuery, IReadOnlyList<SalesCashAccountLookupDto>>
{
    public async Task<IReadOnlyList<SalesCashAccountLookupDto>> Handle(SearchSalesCashAccountsQuery request, CancellationToken ct)
    {
        var search = request.Search?.Trim();
        var spec = new Specification<CashAccount>();
        if (!string.IsNullOrWhiteSpace(search))
            spec.Where(x => x.IsActive && x.CurrencyId == request.CurrencyId && (x.Code.Contains(search) || x.Name.Contains(search)));
        else
            spec.Where(x => x.IsActive && x.CurrencyId == request.CurrencyId);

        spec.AddSort(nameof(CashAccount.IsDefault), OAS.Contracts.Common.Pagination.SortDirection.Descending)
            .AddSort(nameof(CashAccount.Name), OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .ApplyPaging(0, SalesLookupLimit.Normalize(request.Take));
        var rows = await cashAccounts.ListAsync(spec, ct);
        return rows.Select(x => new SalesCashAccountLookupDto(x.Id, x.Code, x.Name, x.CurrencyId!.Value, x.IsDefault, x.IsActive)).ToArray();
    }
}

public sealed class SearchSalesBankAccountsQueryHandler(
    IReadRepository<BankAccount, Guid> bankAccounts)
    : IRequestHandler<SearchSalesBankAccountsQuery, IReadOnlyList<SalesBankAccountLookupDto>>
{
    public async Task<IReadOnlyList<SalesBankAccountLookupDto>> Handle(SearchSalesBankAccountsQuery request, CancellationToken ct)
    {
        var search = request.Search?.Trim();
        var spec = new Specification<BankAccount>();
        if (!string.IsNullOrWhiteSpace(search))
            spec.Where(x => x.IsActive && x.CurrencyId == request.CurrencyId && (x.Code.Contains(search) || x.BankName.Contains(search) || x.AccountName.Contains(search) || x.AccountNumber.Contains(search)));
        else
            spec.Where(x => x.IsActive && x.CurrencyId == request.CurrencyId);

        spec.AddSort(nameof(BankAccount.BankName), OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .AddSort(nameof(BankAccount.AccountName), OAS.Contracts.Common.Pagination.SortDirection.Ascending)
            .ApplyPaging(0, SalesLookupLimit.Normalize(request.Take));
        var rows = await bankAccounts.ListAsync(spec, ct);
        return rows.Select(x => new SalesBankAccountLookupDto(x.Id, x.Code, x.BankName, x.AccountName, x.AccountNumber, x.CurrencyId!.Value, x.IsActive)).ToArray();
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
         * نعرض العملات النشطة كلها.
         *
         * وجود سعر صرف ليس شرطًا لإظهار العملة في القائمة.
         * إذا لم يوجد السعر، ترجع العملة ولكن IsSelectable/HasRate
         * يكون False على مستوى DTO.
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
                 * العملة الأساسية ستعود من ExchangeRateResolver
                 * بسعر 1 بدون الحاجة إلى سجل ExchangeRate.
                 *
                 * العملة الأجنبية ذات السعر الفعال ستعود بالسعر
                 * الموجود في أو قبل DocumentDate.
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
                 * مهم:
                 *
                 * عدم وجود سعر صرف ليس خطأ Lookup.
                 * لا نوقف القائمة ولا نرمي Conflict للمستخدم.
                 *
                 * نعيد العملة نفسها للـClient ولكن بحالة
                 * HasEffectiveExchangeRate = false.
                 *
                 * الـClient سيعرضها Disabled.
                 */
                result.Add(
                    new SalesCurrencyLookupDto(
                        currency.Id,
                        currency.Code,
                        currency.NameAr,
                        currency.Symbol,
                        currency.DecimalPlaces,

                        // لا يستخدم لأن العنصر سيكون Disabled.
                        EffectiveExchangeRate: 0m,

                        // فقط قيمة مرجعية للـDTO عند عدم وجود سعر.
                        EffectiveRateDate: request.DocumentDate,

                        IsBaseCurrency: false,
                        IsActive: currency.IsActive,
                        HasEffectiveExchangeRate: false,
                        AvailabilityMessage:
                            $"لا يوجد سعر صرف فعال حتى {request.DocumentDate:yyyy-MM-dd}"));
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