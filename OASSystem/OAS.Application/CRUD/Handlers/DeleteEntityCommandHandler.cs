using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.CRUD.Commands;
using OAS.Domain.Common.Entities;
using OAS.Domain.Common.Interfaces;

namespace OAS.Application.CRUD.Handlers;

public sealed class DeleteEntityCommandHandler<TEntity, TKey>(IRepository<TEntity, TKey> repository, TimeProvider timeProvider, ICurrentUser currentUser)
    : IRequestHandler<DeleteEntityCommand<TEntity, TKey>>
    where TEntity : Entity<TKey> where TKey : notnull
{
    public async Task Handle(DeleteEntityCommand<TEntity, TKey> request, CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException(typeof(TEntity).Name, request.Id);

        if (entity is ISoftDeletable softDeletable)
        {
            softDeletable.MarkDeleted(timeProvider.GetUtcNow(), currentUser.UserId);
            repository.Update(entity);
        }
        else
        {
            repository.Delete(entity);
        }
    }
}
