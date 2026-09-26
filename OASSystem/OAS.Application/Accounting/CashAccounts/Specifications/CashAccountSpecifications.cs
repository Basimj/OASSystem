using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.CashAccounts.Specifications;

public static class CashAccountSpecifications
{
    public static Specification<CashAccount> ByCode(string code) =>
        new Specification<CashAccount>().Where(x => x.Code == code);
}
