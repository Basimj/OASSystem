using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using DomainRateType=OAS.Domain.Accounting.Enums.ExchangeRateType;
namespace OAS.Application.Accounting.ExchangeRates.Commands.CreateExchangeRate;
public sealed class CreateExchangeRateCommandHandler(IRepository<ExchangeRate,Guid> repository,IReadRepository<Currency,Guid> currencies,IReadRepository<AccountingSettings,Guid> settings):IRequestHandler<CreateExchangeRateCommand,Guid>
{
 public async Task<Guid> Handle(CreateExchangeRateCommand r,CancellationToken ct)
 {
  var c=await currencies.GetByIdAsync(r.Request.CurrencyId,ct)??throw new NotFoundException(nameof(Currency),r.Request.CurrencyId);
  if(!c.IsActive)throw new ConflictException("exchange_rate_currency_inactive","The selected currency is inactive.");
  var st=await settings.GetByIdAsync(AccountingSettings.SingletonId,ct);
  if(st?.BaseCurrencyId==c.Id && r.Request.Rate!=1m)throw new ConflictException("base_currency_rate_must_be_one","Base currency exchange rate must equal 1.");
  var type=(DomainRateType)(byte)r.Request.RateType;
  if(await repository.CountAsync(new Specification<ExchangeRate>().Where(x=>x.CurrencyId==c.Id&&x.RateDate==r.Request.RateDate&&x.RateType==type),ct)>0)
   throw new ConflictException("exchange_rate_duplicate","An exchange rate already exists for the same currency, date and rate type.");
  var e=ExchangeRate.Create(Guid.NewGuid(),c.Id,r.Request.RateDate,r.Request.Rate,type,r.Request.IsActive);await repository.AddAsync(e,ct);return e.Id;
 }
}
