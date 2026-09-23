using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.Parties;
public sealed class ManagedAccountGuard(IReadRepository<Customer,Guid> customers,IReadRepository<Supplier,Guid> suppliers):IManagedAccountGuard
{
    public async Task<bool> IsManagedAsync(Guid accountId,CancellationToken ct=default) =>
        await customers.CountAsync(new Specification<Customer>().Where(x=>x.AccountId==accountId),ct)>0 ||
        await suppliers.CountAsync(new Specification<Supplier>().Where(x=>x.AccountId==accountId),ct)>0;
}
