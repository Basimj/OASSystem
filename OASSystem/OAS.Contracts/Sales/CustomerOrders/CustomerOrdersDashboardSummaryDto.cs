namespace OAS.Contracts.Sales.CustomerOrders;

/// <summary>Counts across all orders, not the current page. "Open" excludes completed and cancelled.</summary>
public sealed record CustomerOrdersDashboardSummaryDto(long OpenOrders, long AwaitingStock, long InProduction, long ReadyForDelivery);
