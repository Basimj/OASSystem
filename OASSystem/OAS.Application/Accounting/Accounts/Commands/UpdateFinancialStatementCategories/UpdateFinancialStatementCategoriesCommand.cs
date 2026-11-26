using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.Accounts;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Accounts.Commands.UpdateFinancialStatementCategories;

public sealed record UpdateFinancialStatementCategoriesCommand(UpdateFinancialStatementCategoriesRequest Data)
    : ICommand<UpdateFinancialStatementCategoriesResult>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [AccountingPermissions.Accounts.Edit];
}

public sealed class UpdateFinancialStatementCategoriesCommandHandler(IRepository<Account, Guid> accounts)
    : IRequestHandler<UpdateFinancialStatementCategoriesCommand, UpdateFinancialStatementCategoriesResult>
{
    public async Task<UpdateFinancialStatementCategoriesResult> Handle(UpdateFinancialStatementCategoriesCommand request, CancellationToken cancellationToken)
    {
        var changes = request.Data.Changes;
        if (changes is null || changes.Count == 0 || changes.Count > 5000 || changes.Select(c => c.AccountId).Distinct().Count() != changes.Count)
            throw new ConflictException("accounting_financial_category_batch_invalid", "حدد من 1 إلى 5000 حساب دون تكرار.");
        var pending = new List<(Account Account, FinancialStatementCategory? Category)>();
        foreach (var change in changes)
        {
            var account = await accounts.GetForUpdateAsync(change.AccountId, cancellationToken) ?? throw new NotFoundException(nameof(Account), change.AccountId);
            try
            {
                if (string.IsNullOrWhiteSpace(change.RowVersion) || !Convert.FromBase64String(change.RowVersion).SequenceEqual(account.RowVersion))
                    throw new ConcurrencyException("تم تعديل الحساب؛ حدّث البيانات ثم أعد المحاولة.");
            }
            catch (FormatException ex) { throw new ConcurrencyException("بيانات التزامن غير صالحة.", ex); }
            var category = (FinancialStatementCategory?)change.Category;
            if (category.HasValue && !FinancialStatementCategories.IsCompatible(account.AccountClass, category.Value))
                throw new ConflictException("accounting_financial_category_invalid", "تصنيف القوائم المالية لا يتوافق مع تصنيف الحساب: " + account.Code);
            if (account.ParentAccountId.HasValue && category != account.FinancialStatementCategory)
                throw new ConflictException("accounting_financial_category_inherited", "لا يمكن تغيير التصنيف المالي لحساب فرعي من التصنيف الجماعي.");
            if (!account.ParentAccountId.HasValue && category is null && account.AccountClass != AccountClass.Equity)
                throw new ConflictException("accounting_financial_category_required", "التصنيف المالي للحساب المستقل مطلوب.");
            pending.Add((account, category));
        }
        // Presentation metadata only. Do not call UpdateDetails or bypass any structural edit guard.
        foreach (var item in pending)
        {
            item.Account.SetFinancialStatementCategory(item.Category);
            accounts.Update(item.Account);
        }
        return new UpdateFinancialStatementCategoriesResult(pending.Count);
    }
}
