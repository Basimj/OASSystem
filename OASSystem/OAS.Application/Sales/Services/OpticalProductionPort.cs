using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Services;

public sealed class OpticalProductionPort(
    IReadRepository<OpticalJob, Guid> jobs,
    IOpticalJobService opticalJobs) : IOpticalProductionPort
{
    public async Task<Guid?> EnsureJobAsync(
        CustomerOrder order,
        Guid? salesInvoiceId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        if (!order.Lines.Any(x => x.IsActive && x.RequiresProduction))
            return null;

        var existing = (await jobs.ListAsync(
            new Specification<OpticalJob>().Where(x => x.CustomerOrderId == order.Id && x.IsActive),
            cancellationToken)).SingleOrDefault();
        if (existing is not null)
            return existing.Id;

        return await opticalJobs.CreateAsync(
            new CreateOpticalJobRequest(order.Id, salesInvoiceId, order.RequiredDate, order.Notes, []),
            cancellationToken);
    }
}
