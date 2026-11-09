using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Accounting.ExchangeRates;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using ContractRateType = OAS.Contracts.Accounting.Enums.ExchangeRateType;

namespace OAS.Application.Accounting.ExchangeRates.Queries.GetExchangeRates;

public sealed class GetExchangeRatesQueryHandler(
    IReadRepository<ExchangeRate, Guid> repository,
    IReadRepository<Currency, Guid> currencies)
    : IRequestHandler<GetExchangeRatesQuery, PagedResult<ExchangeRateDto>>
{
    public async Task<PagedResult<ExchangeRateDto>> Handle(GetExchangeRatesQuery request, CancellationToken ct)
    {
        var p = request.Request.Normalize();
        Guid[] searchCurrencyIds = [];
        if (!string.IsNullOrWhiteSpace(p.Search))
        {
            var term = p.Search.Trim();
            var matching = await currencies.ListAsync(new Specification<Currency>()
                .Where(x => x.Code.Contains(term) || x.NameAr.Contains(term) ||
                    (x.NameEn != null && x.NameEn.Contains(term))), ct);
            searchCurrencyIds = matching.Select(x => x.Id).ToArray();
        }

        // Specification.Where replaces the previous predicate: combine filters in one expression.
        var spec = new Specification<ExchangeRate>().Where(x =>
            (p.ExchangeRateCurrencyId == null || x.CurrencyId == p.ExchangeRateCurrencyId.Value) &&
            (string.IsNullOrWhiteSpace(p.Search) || searchCurrencyIds.Contains(x.CurrencyId)) &&
            (p.DateFrom == null || x.RateDate >= p.DateFrom.Value) &&
            (p.DateTo == null || x.RateDate <= p.DateTo.Value) &&
            (p.Status != "active" || x.IsActive) &&
            (p.Status != "inactive" || !x.IsActive));
        spec.AddSort(nameof(ExchangeRate.RateDate), SortDirection.Descending)
            .ApplyPaging((p.PageNumber - 1) * p.PageSize, p.PageSize);
        var page = await repository.GetPageAsync(spec, ct);
        var ids = page.Items.Select(x => x.CurrencyId).Distinct().ToArray();
        var names = ids.Length == 0
            ? new Dictionary<Guid, Currency>()
            : (await currencies.ListAsync(new Specification<Currency>()
                .Where(x => ids.Contains(x.Id)), ct)).ToDictionary(x => x.Id);
        return new PagedResult<ExchangeRateDto>
        {
            Items = page.Items.Select(x => new ExchangeRateDto(
                x.Id, x.CurrencyId,
                names.TryGetValue(x.CurrencyId, out var currency) ? currency.Code : string.Empty,
                x.RateDate, x.Rate, (ContractRateType)(byte)x.RateType, x.IsActive,
                Convert.ToBase64String(x.RowVersion))).ToArray(),
            PageNumber = p.PageNumber,
            PageSize = p.PageSize,
            TotalCount = page.TotalCount
        };
    }
}
