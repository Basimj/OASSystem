using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.FiscalYears;
using OAS.Domain.Accounting.Entities;
using ContractFiscalYearStatus =
    OAS.Contracts.Accounting.Enums.FiscalYearStatus;
using DomainFiscalYearStatus =
    OAS.Domain.Accounting.Enums.FiscalYearStatus;

namespace OAS.Application.Accounting.FiscalYears.Mapping;

public sealed class FiscalYearMapper
    : ICrudMapper<
        FiscalYear,
        Guid,
        FiscalYearDto,
        CreateFiscalYearRequest,
        UpdateFiscalYearRequest>
{
    public FiscalYear Create(CreateFiscalYearRequest source)
    {
        return FiscalYear.Create(
            Guid.NewGuid(),
            source.Code,
            source.Name,
            source.StartDate,
            source.EndDate,
            DomainFiscalYearStatus.Future);
    }

    public void Update(
        UpdateFiscalYearRequest source,
        FiscalYear destination)
    {
        destination.UpdateDetails(
            source.Code,
            source.Name,
            source.StartDate,
            source.EndDate);
    }

    public FiscalYearDto ToRead(FiscalYear source)
    {
        return new FiscalYearDto(
            source.Id,
            source.Code,
            source.Name,
            source.StartDate,
            source.EndDate,
            (ContractFiscalYearStatus)(byte)source.Status,
            source.ClosedAtUtc.HasValue
                ? new DateTimeOffset(
                    source.ClosedAtUtc.Value,
                    TimeSpan.Zero)
                : null,
            source.ClosedBy,
            Convert.ToBase64String(source.RowVersion));
    }
}