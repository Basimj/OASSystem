using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Abstractions;

public interface IExpenseCurrencyService
{
    Task ApplyAsync(Expense expense, Guid? currencyId, decimal? expectedRate, CancellationToken ct);
}
