using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Rules;

public static class SubledgerAccountRules
{
    public static void EnsureSingleParty(Guid? customerId, Guid? supplierId, Guid? employeeId)
    {
        var count = (customerId.HasValue ? 1 : 0) +
                    (supplierId.HasValue ? 1 : 0) +
                    (employeeId.HasValue ? 1 : 0);

        if (count > 1)
            throw new DomainException("Only one customer, supplier or employee can be selected per journal line.");
    }

    public static void EnsurePartyOwnsAccount(Guid selectedAccountId, Guid expectedAccountId, string partyType)
    {
        if (selectedAccountId != expectedAccountId)
            throw new DomainException($"The selected {partyType} does not own the accounting account used by this journal line.");
    }
}
