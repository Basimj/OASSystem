using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfileById;

public sealed class GetPostingProfileByIdQueryHandler(
    IReadRepository<PostingProfile, Guid> repository,
    PostingProfileMapper mapper)
    : IRequestHandler<GetPostingProfileByIdQuery, PostingProfileDto>
{
    public async Task<PostingProfileDto> Handle(
        GetPostingProfileByIdQuery request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetByIdAsync(request.Id, cancellationToken);
        if (entity is null)
        {
            throw new NotFoundException(nameof(PostingProfile), request.Id);
        }

        return mapper.ToRead(entity);
    }
}
