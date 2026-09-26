using OAS.Application.Abstractions.Messaging;using OAS.Application.Abstractions.Security;using OAS.Application.Accounting.Authorization;using OAS.Contracts.Accounting.Currencies;using OAS.Contracts.Common.Pagination;
namespace OAS.Application.Accounting.Currencies.Queries.GetCurrencies;
public sealed record GetCurrenciesQuery(PageRequest Request):IQuery<PagedResult<CurrencyDto>>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[AccountingPermissions.Currencies.View];}
