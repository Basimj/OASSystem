using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Expenses.ExpenseTypes.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Commands.CreateExpenseType;

public sealed class CreateExpenseTypeCommandHandler(
    IRepository<ExpenseType, Guid> repository,
    IReadRepository<Account, Guid> accountRepository,
    ExpenseTypeMapper mapper)
    : IRequestHandler<CreateExpenseTypeCommand, ExpenseType>
{
    public async Task<ExpenseType> Handle(
        CreateExpenseTypeCommand request,
        CancellationToken cancellationToken)
    {
        await ValidateDefaultAccountAsync(request.Data.DefaultExpenseAccountId, cancellationToken);

        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
    private async Task ValidateDefaultAccountAsync(Guid? accountId, CancellationToken cancellationToken)
    {
        if (!accountId.HasValue) return;

        var account = await accountRepository.GetByIdAsync(accountId.Value, cancellationToken);
        if (account is null)
            throw new NotFoundException(nameof(Account), accountId.Value);
        if (!account.CanReceivePosting())
            throw new ConflictException(
                "default_expense_account_invalid",
                $"الحساب {account.Code} - {account.NameAr} غير صالح كحساب مصروف افتراضي.");
    }

}
