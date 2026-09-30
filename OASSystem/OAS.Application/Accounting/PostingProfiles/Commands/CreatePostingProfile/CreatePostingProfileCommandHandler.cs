using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;

public sealed class CreatePostingProfileCommandHandler(
    IRepository<PostingProfile, Guid> repository,
    IReadRepository<Account, Guid> accountRepository,
    PostingProfileMapper mapper)
    : IRequestHandler<CreatePostingProfileCommand, PostingProfile>
{
    public async Task<PostingProfile> Handle(
        CreatePostingProfileCommand request,
        CancellationToken cancellationToken)
    {
        // Preserve the existing accounting rule: every account referenced by the
        // posting profile must exist and be eligible to receive postings.
        await ValidateAccountsAsync(
            request.Data.Lines.Select(x => x.AccountId),
            cancellationToken);

        var entity = mapper.Create(request.Data);

        // Root invariant: there may be only one active profile for the same
        // Module + DocumentType scope. Historical profiles are kept inactive.
        await PostingProfileActivationPolicy.DeactivateCompetingProfilesAsync(
            repository,
            currentProfileId: null,
            entity.Module,
            entity.DocumentType,
            entity.IsActive,
            cancellationToken);

        await repository.AddAsync(entity, cancellationToken);
        return entity;
    }

    private async Task ValidateAccountsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        foreach (var accountId in accountIds.Distinct())
        {
            var account = await accountRepository.GetByIdAsync(
                accountId,
                cancellationToken);

            if (account is null)
                throw new NotFoundException(nameof(Account), accountId);

            if (!account.CanReceivePosting())
            {
                throw new ConflictException(
                    "posting_profile_account_invalid",
                    $"الحساب {account.Code} - {account.NameAr} غير صالح للاستخدام في ملف الترحيل.");
            }
        }
    }
}
