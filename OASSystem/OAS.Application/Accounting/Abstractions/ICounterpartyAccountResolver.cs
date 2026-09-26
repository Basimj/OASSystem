using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Abstractions;

public sealed record CounterpartyResolution(
    Guid AccountId,
    string PartyNameSnapshot,
    Guid? CustomerId,
    Guid? SupplierId,
    Guid? EmployeeId);

public interface ICounterpartyAccountResolver
{
    Task<CounterpartyResolution> ResolveAsync(
        SettlementPartyType partyType,
        Guid? customerId,
        Guid? supplierId,
        Guid? employeeId,
        string? partyName,
        Guid? otherAccountId,
        CancellationToken cancellationToken = default);
}
