using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OrderOperations;

public sealed record CustomerOrderOperationsItemDto(
    Guid CustomerOrderId,
    string OrderCode,
    DateOnly OrderDate,
    DateOnly? RequiredDate,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string? Mobile,
    string ItemsSummary,
    int TotalLines,
    int AvailableLines,
    int ShortageLines,
    CustomerOrderSupplySummaryDto SupplySummary,
    bool RequiresProduction,
    CustomerOrderStatus OperationalStatus,
    Guid? OpticalJobId,
    string? OpticalJobCode,
    Guid? TechnicianId,
    string? Technician,
    DateTimeOffset LastUpdatedAt);
