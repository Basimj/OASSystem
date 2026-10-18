using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.SupplierOpenings;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.SupplierOpenings;

public sealed record GetSupplierOpeningBatchQuery(Guid BatchId)
    : IQuery<SupplierOpeningBatchDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.Journals.View, AccountingPermissions.Suppliers.View];
}

public sealed class GetSupplierOpeningBatchQueryHandler(
    ISupplierOpeningRepository openings,
    IReadRepository<Supplier, Guid> suppliers,
    IReadRepository<Currency, Guid> currencies)
    : IRequestHandler<GetSupplierOpeningBatchQuery, SupplierOpeningBatchDto>
{
    public async Task<SupplierOpeningBatchDto> Handle(GetSupplierOpeningBatchQuery query, CancellationToken cancellationToken)
    {
        var batch = await openings.GetBatchAsync(query.BatchId, false, cancellationToken)
            ?? throw new NotFoundException(nameof(SupplierOpeningBatch), query.BatchId);
        var baseCurrency = await currencies.GetByIdAsync(batch.BaseCurrencyId, cancellationToken);
        var rows = new List<SupplierOpeningItemDto>(batch.Items.Count);
        foreach (var item in batch.Items.OrderBy(x => x.SupplierId).ThenBy(x => x.DocumentDate).ThenBy(x => x.Id))
        {
            var supplier = await suppliers.GetByIdAsync(item.SupplierId, cancellationToken);
            var applications = await openings.GetApplicationsForItemAsync(item.Id, cancellationToken);
            var applied = applications.Where(x => x.Status == SupplierOpeningApplicationStatus.Active)
                .Sum(x => x.AppliedTargetBaseAmount);
            rows.Add(new SupplierOpeningItemDto(
                item.Id, item.SupplierId, supplier?.SupplierCode, supplier?.NameAr,
                item.LegacyReference, item.DocumentDate, item.DueDate, (byte)item.DueDateProvenance,
                item.CurrencyId, item.CurrencyCodeSnapshot, item.CurrencyDecimalPlacesSnapshot,
                item.OriginalOpenAmount, item.ExchangeRateSnapshot, item.BaseOpenAmount,
                Math.Round(applied, 4), Math.Max(0m, Math.Round(item.BaseOpenAmount - applied, 4)),
                SupplierOpeningRowVersion.Encode(item.RowVersion)));
        }
        var total = Math.Round(rows.Sum(x => x.BaseOpenAmount), 4);
        var outstanding = Math.Round(rows.Sum(x => x.OutstandingBaseAmount), 4);
        return new SupplierOpeningBatchDto(
            batch.Id, batch.BatchCode, batch.AsOfDate, batch.BaseCurrencyId, baseCurrency?.Code,
            (byte)batch.Status, batch.JournalEntryId, batch.Notes, batch.PostedBy, batch.PostedAtUtc,
            rows, total, outstanding, SupplierOpeningRowVersion.Encode(batch.RowVersion));
    }
}
