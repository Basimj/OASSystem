using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Accounting.MultiCurrency;

public sealed class CounterpartyAccountResolver(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<Employee, Guid> employees,
    IReadRepository<EmployeeAccount, Guid> employeeAccounts,
    IReadRepository<Account, Guid> accounts) : ICounterpartyAccountResolver
{
    public async Task<CounterpartyResolution> ResolveAsync(SettlementPartyType partyType, Guid? customerId, Guid? supplierId, Guid? employeeId, string? partyName, Guid? otherAccountId, CancellationToken cancellationToken = default)
    {
        return partyType switch
        {
            SettlementPartyType.Customer => await ResolveCustomerAsync(customerId, supplierId, employeeId, cancellationToken),
            SettlementPartyType.Supplier => await ResolveSupplierAsync(customerId, supplierId, employeeId, cancellationToken),
            SettlementPartyType.Employee => await ResolveEmployeeAsync(customerId, supplierId, employeeId, cancellationToken),
            SettlementPartyType.Other => await ResolveOtherAsync(customerId, supplierId, employeeId, partyName, otherAccountId, cancellationToken),
            _ => throw new ConflictException("settlement_party_type_invalid", "Settlement party type is invalid.")
        };
    }

    private async Task<CounterpartyResolution> ResolveCustomerAsync(Guid? customerId, Guid? supplierId, Guid? employeeId, CancellationToken ct)
    {
        EnsureOnly(customerId, supplierId, employeeId, 1);
        var id = customerId ?? throw new ConflictException("customer_required", "Customer is required.");
        var party = await customers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Customer), id);
        if (!party.IsActive) throw new ConflictException("customer_inactive", "Selected customer is inactive.");
        await EnsureAccountAsync(party.AccountId, ct);
        return new(party.AccountId, party.NameAr, id, null, null);
    }

    private async Task<CounterpartyResolution> ResolveSupplierAsync(Guid? customerId, Guid? supplierId, Guid? employeeId, CancellationToken ct)
    {
        EnsureOnly(customerId, supplierId, employeeId, 2);
        var id = supplierId ?? throw new ConflictException("supplier_required", "Supplier is required.");
        var party = await suppliers.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Supplier), id);
        if (!party.IsActive) throw new ConflictException("supplier_inactive", "Selected supplier is inactive.");
        await EnsureAccountAsync(party.AccountId, ct);
        return new(party.AccountId, party.NameAr, null, id, null);
    }

    private async Task<CounterpartyResolution> ResolveEmployeeAsync(Guid? customerId, Guid? supplierId, Guid? employeeId, CancellationToken ct)
    {
        EnsureOnly(customerId, supplierId, employeeId, 3);
        var id = employeeId ?? throw new ConflictException("employee_required", "Employee is required.");
        var employee = await employees.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Employee), id);
        if (!employee.IsActive) throw new ConflictException("employee_inactive", "Selected employee is inactive.");
        var mappings = await employeeAccounts.ListAsync(new Specification<EmployeeAccount>().Where(x => x.EmployeeId == id && x.IsActive).ApplyPaging(0, 2), ct);
        var mapping = mappings.SingleOrDefault() ?? throw new ConflictException("employee_account_mapping_required", "Selected employee does not have an active accounting mapping.");
        await EnsureAccountAsync(mapping.AccountId, ct);
        return new(mapping.AccountId, employee.DisplayName, null, null, id);
    }

    private async Task<CounterpartyResolution> ResolveOtherAsync(Guid? customerId, Guid? supplierId, Guid? employeeId, string? partyName, Guid? otherAccountId, CancellationToken ct)
    {
        if (customerId.HasValue || supplierId.HasValue || employeeId.HasValue)
            throw new ConflictException("other_party_fk_conflict", "Other party cannot contain customer, supplier or employee id.");
        if (string.IsNullOrWhiteSpace(partyName)) throw new ConflictException("other_party_name_required", "Party name is required for Other.");
        var accountId = otherAccountId ?? throw new ConflictException("other_party_account_required", "Counterparty account is required for Other.");
        var account = await EnsureAccountAsync(accountId, ct, requireManual: true);
        return new(account.Id, partyName.Trim(), null, null, null);
    }

    private async Task<Account> EnsureAccountAsync(Guid id, CancellationToken ct, bool requireManual = false)
    {
        var account = await accounts.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Account), id);
        var valid = requireManual ? account.CanReceiveManualPosting() : account.CanReceivePosting();
        if (!valid) throw new ConflictException("counterparty_account_invalid", "Counterparty account is inactive or cannot receive posting.");
        return account;
    }

    private static void EnsureOnly(Guid? customerId, Guid? supplierId, Guid? employeeId, int expected)
    {
        var count = (customerId.HasValue ? 1 : 0) + (supplierId.HasValue ? 1 : 0) + (employeeId.HasValue ? 1 : 0);
        if (count != 1 || (expected == 1 && !customerId.HasValue) || (expected == 2 && !supplierId.HasValue) || (expected == 3 && !employeeId.HasValue))
            throw new ConflictException("typed_party_fk_invalid", "Exactly one party foreign key must match PartyType.");
    }
}
