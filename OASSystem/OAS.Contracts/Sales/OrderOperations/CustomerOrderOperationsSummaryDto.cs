namespace OAS.Contracts.Sales.OrderOperations;

public sealed record CustomerOrderOperationsSummaryDto(
    int TodayOrders,
    int AwaitingStock,
    int ReadyForProduction,
    int InProduction,
    int ReadyForDelivery,
    int Overdue,
    int Total);
