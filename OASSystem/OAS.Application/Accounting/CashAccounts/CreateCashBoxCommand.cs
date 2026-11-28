using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashAccounts;
namespace OAS.Application.Accounting.CashAccounts;
public sealed record CreateCashBoxCommand(CreateCashBoxRequest Data) : ICommand<Guid>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.CashAccounts.Create];
}
public sealed class CreateCashBoxCommandHandler(ISender sender) : IRequestHandler<CreateCashBoxCommand, Guid>
{
    public async Task<Guid> Handle(CreateCashBoxCommand request, CancellationToken ct)
    {
        var data = request.Data;
        if (string.IsNullOrWhiteSpace(data.Name) || data.Name.Trim().Length > 150 || data.Currencies is null || data.Currencies.Count == 0 ||
            data.Currencies.Select(x => x.CurrencyId).Distinct().Count() != data.Currencies.Count ||
            (data.Currencies.Count > 1 && !data.Currencies.Any(x=>x.IsActive)))
            throw new ConflictException("cash_box_invalid", "أدخل اسم الصندوق وعملاته دون تكرار.");
        Guid? first = null;
        foreach (var currency in data.Currencies.OrderByDescending(x=>x.IsActive))
        {
            var entity = await sender.Send(new CreateCashAccountCommand(new(first is null ? data.Code : null, data.Name.Trim(), currency.CurrencyId,
                currency.IsDefault, currency.IsActive, data.RequiresOpenShift, currency.MovementType, first)), ct);
            first ??= entity.Id;
        }
        return first!.Value;
    }
}
