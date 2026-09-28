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
    IReadRepository<Account, Guid> accountRepository,
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
                "لا يمكن تغيير كود ملف الترحيل بعد الإنشاء.");
        }

        var requestedRowVersion = Convert.FromBase64String(request.Data.RowVersion);
        if (!entity.RowVersion.SequenceEqual(requestedRowVersion))
            throw new ConcurrencyException("تم تعديل ملف الترحيل بواسطة مستخدم آخر. أعد تحميله ثم حاول مرة أخرى.");

        await ValidateAccountsAsync(request.Data.Lines.Select(x => x.AccountId), cancellationToken);

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

    private async Task ValidateAccountsAsync(
        IEnumerable<Guid> accountIds,
        CancellationToken cancellationToken)
    {
        foreach (var accountId in accountIds.Distinct())
        {
            var account = await accountRepository.GetByIdAsync(accountId, cancellationToken);
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
