using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Lookups;

namespace OAS.Application.Sales.Lookups;

public sealed record SearchSalesCustomersQuery(string? Search, int Take = 20)
    : IQuery<IReadOnlyList<SalesCustomerLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}

public sealed record SearchSalesPrescriptionsQuery(Guid? CustomerId, string? Search, int Take = 20)
    : IQuery<IReadOnlyList<SalesPrescriptionLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Prescriptions.View];
}

public sealed record GetSalesPrescriptionRevisionsQuery(Guid PrescriptionId)
    : IQuery<IReadOnlyList<SalesPrescriptionRevisionLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Prescriptions.View];
}

public sealed record SearchSalesProductCategoriesQuery(string? Search, int Take = 20)
    : IQuery<IReadOnlyList<SalesProductCategoryLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}

public sealed record SearchSalesProductVariantsQuery(Guid? CategoryId, string? Search, int Take = 20)
    : IQuery<IReadOnlyList<SalesProductVariantLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}

public sealed record SearchSalesWarehousesQuery(Guid? ProductVariantId, string? Search, int Take = 20)
    : IQuery<IReadOnlyList<SalesWarehouseLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}

public sealed record SearchSalesCurrenciesQuery(DateOnly DocumentDate, string? Search, int Take = 20)
    : IQuery<IReadOnlyList<SalesCurrencyLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}


public sealed record SearchSalesCashAccountsQuery(Guid CurrencyId, string? Search, int Take = 20)
    : IQuery<IReadOnlyList<SalesCashAccountLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}

public sealed record SearchSalesBankAccountsQuery(Guid CurrencyId, string? Search, int Take = 20)
    : IQuery<IReadOnlyList<SalesBankAccountLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}

public sealed record SearchCustomerOrdersLookupQuery(Guid? CustomerId, string? Search, int Take = 20)
    : IQuery<IReadOnlyList<CustomerOrderLookupDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Orders.View];
}
