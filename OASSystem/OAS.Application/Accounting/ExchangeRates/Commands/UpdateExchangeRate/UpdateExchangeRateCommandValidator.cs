using FluentValidation;namespace OAS.Application.Accounting.ExchangeRates.Commands.UpdateExchangeRate;
public sealed class UpdateExchangeRateCommandValidator:AbstractValidator<UpdateExchangeRateCommand>{public UpdateExchangeRateCommandValidator(){RuleFor(x=>x.Id).NotEmpty();RuleFor(x=>x.Request.Rate).GreaterThan(0);RuleFor(x=>x.Request.RowVersion).NotEmpty();}}
