using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.CustomerOrders;

public sealed record SetCustomerOrderHeaderDiscountRequest(
    SalesDiscountType DiscountType,
    decimal? DiscountValue,
    string? Reason,
    string RowVersion);

public sealed record ApproveCustomerOrderHeaderDiscountRequest(string RowVersion);
