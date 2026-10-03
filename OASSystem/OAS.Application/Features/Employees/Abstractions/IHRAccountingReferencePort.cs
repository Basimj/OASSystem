namespace OAS.Application.Features.Employees.Abstractions;

public sealed record HRCurrencyReference(Guid Id, string Code, string? Symbol, byte DecimalPlaces, bool IsActive);

public interface IHRAccountingReferencePort
{
    Task<HRCurrencyReference?> GetCurrencyAsync(Guid currencyId, CancellationToken cancellationToken = default);
}
