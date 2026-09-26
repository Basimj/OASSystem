using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Security;
using OAS.Application.Accounting.Authorization;
using OAS.Contracts.Accounting.ExchangeRates;
namespace OAS.Application.Accounting.ExchangeRates.Commands.CreateExchangeRate;
public sealed record CreateExchangeRateCommand(CreateExchangeRateRequest Request) : ICommand<Guid>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.ExchangeRates.Manage]; }
