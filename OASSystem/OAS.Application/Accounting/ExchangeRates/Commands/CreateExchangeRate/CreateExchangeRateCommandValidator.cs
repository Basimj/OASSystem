using FluentValidation;
namespace OAS.Application.Accounting.ExchangeRates.Commands.CreateExchangeRate;
public sealed class CreateExchangeRateCommandValidator : AbstractValidator<CreateExchangeRateCommand>
{ public CreateExchangeRateCommandValidator(){RuleFor(x=>x.Request.CurrencyId).NotEmpty();RuleFor(x=>x.Request.Rate).GreaterThan(0);} }
