using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Accounting.PostingProfiles;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Commands.SetPostingProfileStatus;

public sealed class SetPostingProfileStatusCommandHandler(
    IRepository<PostingProfile, Guid> repository,
    IReadRepository<PostingProfileLine, Guid> lineRepository,
    IReadRepository<Account, Guid> accountRepository)
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

        if (request.Request.IsActive)
        {
            var lines = await lineRepository.ListAsync(new Specification<PostingProfileLine>()
                .Where(x => x.PostingProfileId == entity.Id), cancellationToken);
            if (lines.Count == 0 ||
                !PostingProfileRoleRequirements.HasRequiredRoles(entity.Module, entity.DocumentType,
                    lines.Select(x => x.AccountRole)))
                throw new ConflictException("posting_profile_required_roles_missing",
                    "ملف الترحيل غير مكتمل. أضف القواعد والأدوار المطلوبة قبل تفعيله.");
            if (lines.Select(x => x.AccountRole.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count() != lines.Count)
                throw new ConflictException("posting_profile_duplicate_role",
                    "يوجد دور محاسبي مكرر. راجع القواعد قبل التفعيل.");
            foreach (var accountId in lines.Select(x => x.AccountId).Distinct())
            {
                var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
                if (account is null || !account.CanReceivePosting())
                    throw new ConflictException("posting_profile_account_invalid",
                        "أحد الحسابات المرتبطة غير موجود أو غير مؤهل للترحيل. راجع قواعد الملف.");
            }
        }

        await PostingProfileActivationPolicy.DeactivateCompetingProfilesAsync(
            repository,
            entity.Id,
            entity.Module,
            entity.DocumentType,
            request.Request.IsActive,
            cancellationToken);

        entity.SetActive(request.Request.IsActive);
        repository.Update(entity);
    }
}
