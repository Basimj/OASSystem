using OAS.Domain.Sales.Entities;
namespace OAS.Application.Sales.Abstractions;
public interface IOpticalProductionInventoryService{Task<Guid> IssueMaterialsAsync(OpticalProductionJob job,string user,DateTimeOffset at,CancellationToken ct=default);}
