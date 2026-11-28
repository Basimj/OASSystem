using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.CashAccounts.Commands.CreateCashAccount;
using OAS.Application.Accounting.CashAccounts.Commands.UpdateCashAccount;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.CashAccounts;
using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.CashAccounts;
public sealed record UpdateCashBoxCommand(Guid Id, UpdateCashBoxRequest Data) : ICommand, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => Data.Currencies?.Any(x=>!x.Id.HasValue)==true
        ? [AccountingPermissions.CashAccounts.Edit, AccountingPermissions.CashAccounts.Create] : [AccountingPermissions.CashAccounts.Edit];
}
public sealed class UpdateCashBoxCommandHandler(ISender sender, IReadRepository<CashAccount,Guid> repository) : IRequestHandler<UpdateCashBoxCommand>
{
    public async Task Handle(UpdateCashBoxCommand request, CancellationToken ct)
    {
        var d=request.Data;
        var stored=await repository.ListAsync(new Specification<CashAccount>().Where(x=>x.CashBoxId==request.Id),ct);
        if(stored.Count==0||string.IsNullOrWhiteSpace(d.Name)||d.Name.Trim().Length>150||d.Currencies is null||d.Currencies.Count==0||d.Currencies.Select(x=>x.CurrencyId).Distinct().Count()!=d.Currencies.Count||
           d.Currencies.Where(x=>x.Id.HasValue).Select(x=>x.Id!.Value).Distinct().Count()!=stored.Count||
           !stored.Select(x=>x.Id).ToHashSet().SetEquals(d.Currencies.Where(x=>x.Id.HasValue).Select(x=>x.Id!.Value))||
           d.Currencies.Count(x=>x.Id.HasValue)!=stored.Count)
            throw new ConflictException("cash_box_invalid", "بيانات الصندوق تغيرت أو توجد عملة مكررة. حدّث البيانات.");
        foreach(var line in d.Currencies.Where(x=>x.Id.HasValue).OrderBy(x=>x.Id))
        {
            var a=stored.Single(x=>x.Id==line.Id);
            if(a.CurrencyId!=line.CurrencyId||string.IsNullOrWhiteSpace(line.RowVersion)||!a.RowVersion.SequenceEqual(Convert.FromBase64String(line.RowVersion)))
                throw new ConflictException("cash_box_changed", "تغير حساب العملة، حدّث بيانات الصندوق.");
            var name = a.Id == request.Id ? d.Name.Trim() : a.Name;
            if(a.Name==name&&a.RequiresOpenShift==d.RequiresOpenShift&&a.IsActive==line.IsActive&&a.IsDefault==line.IsDefault&&(byte)a.MovementType==(byte)line.MovementType)continue;
            await sender.Send(new UpdateCashAccountCommand(a.Id,new(name,line.CurrencyId,line.IsDefault,line.IsActive,line.RowVersion,d.RequiresOpenShift,line.MovementType)) { IsGroupUpdate = true },ct);
        }
        Guid? anchor=d.Currencies.FirstOrDefault(x=>x.Id.HasValue&&x.IsActive)?.Id;
        foreach(var line in d.Currencies.Where(x=>!x.Id.HasValue))
        {
            if(anchor is null)throw new ConflictException("cash_box_inactive", "يجب وجود عملة نشطة عند إضافة عملة جديدة.");
            await sender.Send(new CreateCashAccountCommand(new(null,d.Name.Trim(),line.CurrencyId,line.IsDefault,line.IsActive,d.RequiresOpenShift,line.MovementType,anchor)),ct);
        }
    }
}
