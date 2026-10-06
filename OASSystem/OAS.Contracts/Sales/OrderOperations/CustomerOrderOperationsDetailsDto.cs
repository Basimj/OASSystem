using OAS.Contracts.Sales.Enums;

namespace OAS.Contracts.Sales.OrderOperations;

public sealed record CustomerOrderOperationsDetailsDto(
    Guid CustomerOrderId,
    string OrderCode,
    DateOnly OrderDate,
    DateOnly? RequiredDate,
    CustomerOrderStatus Status,
    Guid CustomerId,
    string CustomerCode,
    string CustomerName,
    string? Mobile,
    string? Notes,
    CustomerOrderSupplySummaryDto SupplySummary,
    Guid? OpticalJobId,
    string? OpticalJobCode,
    IReadOnlyList<CustomerOrderLineOperationsDto> Lines,
    IReadOnlyList<CustomerOrderOperationalTimelineItemDto> Timeline);
