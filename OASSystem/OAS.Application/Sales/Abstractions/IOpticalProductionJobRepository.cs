using OAS.Application.Abstractions.Persistence;using OAS.Domain.Sales.Entities;
namespace OAS.Application.Sales.Abstractions;
public interface IOpticalProductionJobRepository:IRepository<OpticalProductionJob,Guid>{Task<OpticalProductionJob?> GetAggregateAsync(Guid id,bool tracking,CancellationToken ct=default);}
