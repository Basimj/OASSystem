using OAS.Application.Abstractions.Messaging; using OAS.Application.Abstractions.Security; using OAS.Application.Accounting.Authorization; using OAS.Contracts.Accounting.Currencies;
namespace OAS.Application.Accounting.Currencies.Commands.CreateCurrency;
public sealed record CreateCurrencyCommand(CreateCurrencyRequest Request):ICommand<Guid>,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[AccountingPermissions.Currencies.Manage];}
