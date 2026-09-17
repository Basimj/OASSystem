using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.Expenses;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Expenses.ExpenseTypes.Mapping;

public sealed class ExpenseTypeMapper
    : ICrudMapper<
        ExpenseType,
        Guid,
        ExpenseTypeDto,
        CreateExpenseTypeRequest,
        UpdateExpenseTypeRequest>
{
    public ExpenseType Create(CreateExpenseTypeRequest source)
    {
        return ExpenseType.Create(
            Guid.NewGuid(),
            source.Code,
            source.NameAr,
            source.NameEn,
            source.DefaultExpenseAccountId,
            source.IsActive);
    }

    public void Update(UpdateExpenseTypeRequest source, ExpenseType destination)
    {
        destination.UpdateDetails(
            source.Code,
            source.NameAr,
            source.NameEn,
            source.DefaultExpenseAccountId);

        destination.SetActive(source.IsActive);
    }

    public ExpenseTypeDto ToRead(ExpenseType source)
    {
        return new ExpenseTypeDto(
            source.Id,
            source.Code,
            source.NameAr,
            source.NameEn,
            source.DefaultExpenseAccountId,
            source.IsActive,
            source.RowVersion is not null ? Convert.ToBase64String(source.RowVersion) : string.Empty);
    }
}
