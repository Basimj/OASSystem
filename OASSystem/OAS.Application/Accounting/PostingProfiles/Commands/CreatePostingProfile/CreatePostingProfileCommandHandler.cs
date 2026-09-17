using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;

public sealed class CreatePostingProfileCommandHandler(
    IRepository<PostingProfile, Guid> repository,
    PostingProfileMapper mapper)
    : IRequestHandler<CreatePostingProfileCommand, PostingProfile>
{
    public async Task<PostingProfile> Handle(
        CreatePostingProfileCommand request,
        CancellationToken cancellationToken)
    {
        var entity = mapper.Create(request.Data);
        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }
}
