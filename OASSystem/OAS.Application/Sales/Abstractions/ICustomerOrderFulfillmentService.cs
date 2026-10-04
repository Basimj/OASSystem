namespace OAS.Application.Sales.Abstractions;

public interface ICustomerOrderFulfillmentService
{
    Task ReconcileOrdersAsync(
        IReadOnlyCollection<Guid> customerOrderIds,
        CancellationToken cancellationToken = default);
}
