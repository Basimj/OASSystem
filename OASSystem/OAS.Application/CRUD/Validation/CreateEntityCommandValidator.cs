using FluentValidation;
using OAS.Application.CRUD.Commands;
using OAS.Domain.Common.Entities;

namespace OAS.Application.CRUD.Validation;

public sealed class CreateEntityCommandValidator<TEntity, TKey, TCreateDto> : AbstractValidator<CreateEntityCommand<TEntity, TKey, TCreateDto>>
    where TEntity : Entity<TKey> where TKey : notnull
{
    public CreateEntityCommandValidator(IEnumerable<IValidator<TCreateDto>> validators)
    {
        foreach (var validator in validators) RuleFor(x => x.Data).SetValidator(validator);
    }
}
