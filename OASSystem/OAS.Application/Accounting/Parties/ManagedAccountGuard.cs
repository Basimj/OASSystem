using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Parties;

/// <summary>
/// Protects GL accounts whose identity/lifecycle is owned by another accounting master record.
/// These accounts must be maintained from the owning customer, supplier, employee, cash or bank screen
/// so the master record and chart of accounts cannot drift apart.
/// </summary>
public sealed class ManagedAccountGuard(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<EmployeeAccount, Guid> employeeAccounts,
    IReadRepository<CashAccount, Guid> cashAccounts,
    IReadRepository<BankAccount, Guid> bankAccounts) : IManagedAccountGuard
{
    public async Task<bool> IsManagedAsync(Guid accountId, CancellationToken ct = default)
    {
        if (accountId == Guid.Empty)
            return false;

        return await customers.CountAsync(
                   new Specification<Customer>().Where(x => x.AccountId == accountId), ct) > 0
               || await suppliers.CountAsync(
                   new Specification<Supplier>().Where(x => x.AccountId == accountId), ct) > 0
               || await employeeAccounts.CountAsync(
                   new Specification<EmployeeAccount>().Where(x => x.AccountId == accountId), ct) > 0
               || await cashAccounts.CountAsync(
                   new Specification<CashAccount>().Where(x => x.AccountId == accountId), ct) > 0
               || await bankAccounts.CountAsync(
                   new Specification<BankAccount>().Where(x => x.AccountId == accountId), ct) > 0;
    }
}
