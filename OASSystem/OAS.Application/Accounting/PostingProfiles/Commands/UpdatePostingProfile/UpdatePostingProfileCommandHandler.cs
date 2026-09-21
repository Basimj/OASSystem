using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Commands.UpdatePostingProfile;

public sealed class UpdatePostingProfileCommandHandler(
    IRepository<PostingProfile, Guid> repository,
    IRepository<PostingProfileLine, Guid> lineRepository,
    PostingProfileMapper mapper)
    : IRequestHandler<UpdatePostingProfileCommand, PostingProfile>
{
    public async Task<PostingProfile> Handle(
        UpdatePostingProfileCommand request,
        CancellationToken cancellationToken)
    {
        var entity = await repository.GetForUpdateAsync(request.Id, cancellationToken);
        if (entity is null)
            throw new NotFoundException(nameof(PostingProfile), request.Id);

        if (!string.Equals(entity.Code, request.Data.Code.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException(
                "accounting_posting_profile_code_immutable",
                "The posting profile code cannot be changed after creation.");
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("The posting profile has been modified by another user.");

        mapper.Update(request.Data, entity);

        var existingLines = await lineRepository.ListAsync(
            new Specification<PostingProfileLine>()
                .Where(x => x.PostingProfileId == entity.Id)
                .Tracking(),
            cancellationToken);

        if (existingLines.Count > 0)
            lineRepository.DeleteRange(existingLines);

        var newLines = request.Data.Lines
            .Select(line => PostingProfileLine.Create(
                Guid.NewGuid(), entity.Id, line.AccountRole, line.AccountId, line.IsRequired))
            .ToList();

        entity.ReplaceLines(newLines);
        if (newLines.Count > 0)
            await lineRepository.AddRangeAsync(newLines, cancellationToken);

        repository.Update(entity);
        return entity;
    }
}
