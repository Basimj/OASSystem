using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.Rules;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Application.Accounting.CoreHardening.Services;

public sealed class JournalSubledgerIntegrityService(
    IReadRepository<Customer, Guid> customers,
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<Employee, Guid> employees,
    IReadRepository<EmployeeAccount, Guid> employeeAccounts) : IJournalSubledgerIntegrityService
{
    public async Task<string?> ValidateAndResolvePartyNameAsync(
        Account account,
        Guid? customerId,
        Guid? supplierId,
        Guid? employeeId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(account);

        try
        {
            SubledgerAccountRules.EnsureSingleParty(customerId, supplierId, employeeId);
        }
        catch (Exception ex)
        {
            throw new ConflictException("journal_party_invalid", ex.Message);
        }

        if (account.AccountType == AccountType.Subledger &&
            !customerId.HasValue && !supplierId.HasValue && !employeeId.HasValue)
        {
            throw new ConflictException(
                "journal_subledger_party_required",
                $"Subledger account '{account.Code}' requires a customer, supplier or employee reference.");
        }

        if (customerId is Guid customerKey)
        {
            var customer = await customers.GetByIdAsync(customerKey, cancellationToken)
                ?? throw new NotFoundException(nameof(Customer), customerKey);
            if (!customer.IsActive)
                throw new ConflictException("journal_customer_inactive", "The selected customer is inactive.");

            if (account.AccountType == AccountType.Subledger && customer.AccountId != account.Id)
            {
                throw new ConflictException(
                    "journal_customer_account_mismatch",
                    "The selected customer does not own the subledger account used by this journal line.");
            }

            return customer.NameAr;
        }

        if (supplierId is Guid supplierKey)
        {
            var supplier = await suppliers.GetByIdAsync(supplierKey, cancellationToken)
                ?? throw new NotFoundException(nameof(Supplier), supplierKey);
            if (!supplier.IsActive)
                throw new ConflictException("journal_supplier_inactive", "The selected supplier is inactive.");

            if (account.AccountType == AccountType.Subledger && supplier.AccountId != account.Id)
            {
                throw new ConflictException(
                    "journal_supplier_account_mismatch",
                    "The selected supplier does not own the subledger account used by this journal line.");
            }

            return supplier.NameAr;
        }

        if (employeeId is Guid employeeKey)
        {
            var employee = await employees.GetByIdAsync(employeeKey, cancellationToken)
                ?? throw new NotFoundException(nameof(Employee), employeeKey);
            if (!employee.IsActive)
                throw new ConflictException("journal_employee_inactive", "The selected employee is inactive.");

            if (account.AccountType == AccountType.Subledger)
            {
                var mappings = await employeeAccounts.ListAsync(
                    new Specification<EmployeeAccount>()
                        .Where(x => x.EmployeeId == employeeKey && x.IsActive)
                        .ApplyPaging(0, 2),
                    cancellationToken);

                if (mappings.Count != 1 || mappings[0].AccountId != account.Id)
                {
                    throw new ConflictException(
                        "journal_employee_account_mismatch",
                        "The selected employee does not own the subledger account used by this journal line.");
                }
            }

            return employee.DisplayName;
        }

        return null;
    }
}
