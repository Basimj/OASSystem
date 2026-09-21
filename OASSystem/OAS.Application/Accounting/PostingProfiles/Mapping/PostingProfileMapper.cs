using OAS.Application.CRUD.Mapping;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Mapping;

public sealed class PostingProfileMapper
    : ICrudMapper<
        PostingProfile,
        Guid,
        PostingProfileDto,
        CreatePostingProfileRequest,
        UpdatePostingProfileRequest>
{
    public PostingProfile Create(CreatePostingProfileRequest source)
    {
        var profile = PostingProfile.Create(
            Guid.NewGuid(),
            source.Code,
            source.Name,
            source.Module,
            source.DocumentType,
            source.IsActive);

        if (source.Lines is not null)
        {
            foreach (var lineReq in source.Lines)
            {
                var line = PostingProfileLine.Create(
                    Guid.NewGuid(),
                    profile.Id,
                    lineReq.AccountRole,
                    lineReq.AccountId,
                    lineReq.IsRequired);
                profile.AddLine(line);
            }
        }

        return profile;
    }

    public void Update(UpdatePostingProfileRequest source, PostingProfile destination)
    {
        destination.UpdateDetails(
            source.Code,
            source.Name,
            source.Module,
            source.DocumentType);

        destination.SetActive(source.IsActive);
    }

    public PostingProfileDto ToRead(PostingProfile source)
        => ToRead(source, source.Lines);

    public PostingProfileDto ToRead(
        PostingProfile source,
        IEnumerable<PostingProfileLine> sourceLines)
    {
        var lines = sourceLines
            .Select(l => new PostingProfileLineDto(
                l.Id,
                l.PostingProfileId,
                l.AccountRole,
                l.AccountId,
                l.IsRequired))
            .ToList();

        return new PostingProfileDto(
            source.Id,
            source.Code,
            source.Name,
            source.Module,
            source.DocumentType,
            source.IsActive,
            Convert.ToBase64String(source.RowVersion),
            lines);
    }
}
