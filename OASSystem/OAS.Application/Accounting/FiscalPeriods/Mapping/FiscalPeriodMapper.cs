using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.Enums;
using OAS.Contracts.Accounting.FiscalPeriods;
using OAS.Domain.Accounting.Entities;
using DomainFiscalPeriodStatus = OAS.Domain.Accounting.Enums.FiscalPeriodStatus;

namespace OAS.Application.Accounting.FiscalPeriods.Mapping;

public sealed class FiscalPeriodMapper
    : ICrudMapper<
        FiscalPeriod,
        Guid,
        FiscalPeriodDto,
        CreateFiscalPeriodRequest,
        UpdateFiscalPeriodRequest>
{
    public FiscalPeriod Create(
        CreateFiscalPeriodRequest source)
    {
        return FiscalPeriod.Create(
            Guid.NewGuid(),
            source.FiscalYearId,
            source.PeriodNumber,
            source.Name,
            source.StartDate,
            source.EndDate,
            DomainFiscalPeriodStatus.Open,
            salesLocked: false,
            inventoryLocked: false,
            accountingLocked: false);
    }

    public void Update(
        UpdateFiscalPeriodRequest source,
        FiscalPeriod destination)
    {
        destination.UpdateDetails(
            source.Name,
            source.StartDate,
            source.EndDate);
    }

    public FiscalPeriodDto ToRead(
        FiscalPeriod source)
    {
        return new FiscalPeriodDto(
            source.Id,
            source.FiscalYearId,
            source.PeriodNumber,
            source.Name,
            source.StartDate,
            source.EndDate,
            (FiscalPeriodStatus)(byte)source.Status,
            source.SalesLocked,
            source.InventoryLocked,
            source.AccountingLocked,
            source.ClosedAtUtc.HasValue
                ? new DateTimeOffset(
                    source.ClosedAtUtc.Value,
                    TimeSpan.Zero)
                : null,
            source.ClosedBy,
            string.Empty);
    }
}