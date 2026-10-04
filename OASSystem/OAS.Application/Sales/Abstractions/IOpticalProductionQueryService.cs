using OAS.Contracts.Sales.Production;
namespace OAS.Application.Sales.Abstractions;
public interface IOpticalProductionQueryService{Task<OpticalProductionJobDto?> GetAsync(Guid id,CancellationToken ct=default);Task<IReadOnlyList<OpticalProductionJobDto>> ListAsync(OpticalProductionStatus? status,Guid? salesInvoiceId,CancellationToken ct=default);}
