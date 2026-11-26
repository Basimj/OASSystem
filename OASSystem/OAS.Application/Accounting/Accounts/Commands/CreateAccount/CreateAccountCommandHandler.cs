using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Accounts.Mapping;
using OAS.Application.Accounting.CoreHardening.Services;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandHandler(
    IRepository<Account, Guid> repository,
    AccountMapper mapper,
    IAccountIntegrityService integrity)
    : IRequestHandler<CreateAccountCommand, Account>
{
    public async Task<Account> Handle(
        CreateAccountCommand request,
        CancellationToken cancellationToken)
    {
        var data = request.Data;
        var accountClass = (OAS.Domain.Accounting.Enums.AccountClass)data.AccountClass;
        var category = (OAS.Domain.Accounting.Enums.FinancialStatementCategory?)data.FinancialStatementCategory;
        if (category.HasValue && !OAS.Domain.Accounting.Enums.FinancialStatementCategories.IsCompatible(accountClass, category.Value))
            throw new OAS.Application.Common.Exceptions.ConflictException("accounting_financial_category_invalid", "تصنيف القوائم المالية لا يتوافق مع تصنيف الحساب.");
        var entity = mapper.Create(data);
        await integrity.ValidateHierarchyAsync(
            entity.Id,
            entity.ParentAccountId,
            entity.Level,
            cancellationToken);

        if (entity.ParentAccountId is Guid parentId)
        {
            var parent = await repository.GetByIdAsync(parentId, cancellationToken);
            if (parent is not null && parent.AccountClass != entity.AccountClass)
                throw new OAS.Application.Common.Exceptions.ConflictException("accounting_financial_category_parent_class", "تصنيف الحساب يجب أن يطابق تصنيف الحساب الأب.");
        }
        var accounts = (await repository.ListAsync(cancellationToken: cancellationToken)).ToDictionary(x => x.Id);
        if (entity.ParentAccountId.HasValue)
        {
            if (category.HasValue)
                throw new OAS.Application.Common.Exceptions.ConflictException("accounting_financial_category_inherited", "الحساب الفرعي يرث التصنيف المالي من الحساب الأب.");
        }
        if (OAS.Domain.Accounting.Enums.FinancialStatementCategories.Resolve(accountClass, category, entity.ParentAccountId, accounts) is null)
            throw new OAS.Application.Common.Exceptions.ConflictException("accounting_financial_category_required", "حدد تصنيف القوائم المالية أو اختر حساباً أب مصنفاً.");
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
