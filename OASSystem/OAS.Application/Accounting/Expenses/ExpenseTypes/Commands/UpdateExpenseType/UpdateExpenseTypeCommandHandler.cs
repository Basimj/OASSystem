using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.UpdateExpenseType;

public sealed class UpdateExpenseTypeCommandHandler(
    IRepository<ExpenseType, Guid> repository,
    IReadRepository<Account, Guid> accountRepository,
    ExpenseTypeMapper mapper)
    : IRequestHandler<UpdateExpenseTypeCommand, ExpenseType>
{
    public async Task<ExpenseType> Handle(
        UpdateExpenseTypeCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(ExpenseType), request.Id);
        }

        if (!string.Equals(entity.Code, request.Data.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "accounting_expense_type_code_immutable",
                "لا يمكن تغيير كود نوع المصروف بعد الإنشاء.");
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("تم تعديل نوع المصروف بواسطة مستخدم آخر. أعد تحميله ثم حاول مرة أخرى.");
        }

        if (request.Data.DefaultExpenseAccountId.HasValue)
        {
            var account = await accountRepository.GetByIdAsync(request.Data.DefaultExpenseAccountId.Value, cancellationToken);
            if (account is null)
                throw new NotFoundException(nameof(Account), request.Data.DefaultExpenseAccountId.Value);
            if (!account.CanReceivePosting())
                throw new ConflictException(
                    "default_expense_account_invalid",
                    $"الحساب {account.Code} - {account.NameAr} غير صالح كحساب مصروف افتراضي.");
        }

        mapper.Update(request.Data, entity);
        repository.Update(entity);
        return entity;
    }
}
