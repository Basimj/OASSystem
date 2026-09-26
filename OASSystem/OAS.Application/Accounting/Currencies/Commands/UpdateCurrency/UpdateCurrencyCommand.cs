using OAS.Application.Abstractions.Messaging;using OAS.Application.Abstractions.Security;using OAS.Application.Accounting.Authorization;using OAS.Contracts.Accounting.Currencies;
namespace OAS.Application.Accounting.Currencies.Commands.UpdateCurrency;
public sealed record UpdateCurrencyCommand(Guid Id,UpdateCurrencyRequest Request):ICommand,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[AccountingPermissions.Currencies.Manage];}
