using FluentValidation;
using OAS.Application.CRUD.Commands;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Validation;

public sealed class UpdateEntityCommandValidator<TEntity, TKey, TUpdateDto> : AbstractValidator<UpdateEntityCommand<TEntity, TKey, TUpdateDto>>
    where TEntity : Entity<TKey> where TKey : notnull
{
    public UpdateEntityCommandValidator(IEnumerable<IValidator<TUpdateDto>> validators)
    {
        foreach (var validator in validators) RuleFor(x => x.Data).SetValidator(validator);
    }
}
