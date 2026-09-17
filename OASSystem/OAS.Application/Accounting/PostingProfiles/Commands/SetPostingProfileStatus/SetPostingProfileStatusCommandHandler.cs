using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Commands.SetPostingProfileStatus;

public sealed class SetPostingProfileStatusCommandHandler(
    IRepository<PostingProfile, Guid> repository)
    : IRequestHandler<SetPostingProfileStatusCommand>
{
    public async Task Handle(
        SetPostingProfileStatusCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(PostingProfile), request.Id);
        }

        var requestedRowVersion = Convert.FromBase64String(request.Request.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ConcurrencyException("The posting profile has been modified by another user.");
        }

        entity.SetActive(request.Request.IsActive);
        repository.Update(entity);
    }
}
