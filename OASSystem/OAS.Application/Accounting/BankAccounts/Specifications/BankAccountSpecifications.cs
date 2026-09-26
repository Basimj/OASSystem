using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.BankAccounts.Specifications;

public static class BankAccountSpecifications
{
    public static Specification<BankAccount> ByCode(string code) =>
        new Specification<BankAccount>().Where(x => x.Code == code);
}
