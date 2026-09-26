using OAS.Application.Abstractions.Messaging;using OAS.Application.Abstractions.Security;using OAS.Application.Accounting.Authorization;using OAS.Contracts.Accounting.ExchangeRates;
namespace OAS.Application.Accounting.ExchangeRates.Commands.UpdateExchangeRate;
public sealed record UpdateExchangeRateCommand(Guid Id,UpdateExchangeRateRequest Request):ICommand,IAuthorizedRequest{public IReadOnlyCollection<string> RequiredPermissions{get;}=[AccountingPermissions.ExchangeRates.Manage];}
