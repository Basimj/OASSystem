using NUnit.Framework;
using OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfileById;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.PostingProfiles;

[TestFixture]
public class PostingProfileCommandAndQueryTests
{
    private FakeRepository<PostingProfile, Guid> _repository = null!;

    private FakeRepository<PostingProfileLine, Guid>
        _lineRepository = null!;

    private FakeRepository<Account, Guid> _accountRepository = null!;

    private PostingProfileMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        _repository =
            new FakeRepository<PostingProfile, Guid>();

        _lineRepository =
            new FakeRepository<PostingProfileLine, Guid>();

        _accountRepository =
            new FakeRepository<Account, Guid>();

        _mapper = new PostingProfileMapper();
    }

    [Test]
    public async Task CreatePostingProfileCommandHandler_CreatesProfileWithLines()
    {
        var account = CreatePostingAccount();
        await _accountRepository.AddAsync(account);

        var handler = new CreatePostingProfileCommandHandler(
            _repository,
            _accountRepository,
            _mapper);

        var request = new CreatePostingProfileRequest(
            Code: "PP-SALES",
            Name: "ملف ترحيل المبيعات",
            Module: "Sales",
            DocumentType: "SalesInvoice",
            IsActive: true,
            Lines:
            [
                new CreatePostingProfileLineRequest(
                    AccountRole: "Customer",
                    AccountId: account.Id,
                    IsRequired: true)
            ]);

        var command = new CreatePostingProfileCommand(request);

        var result = await handler.Handle(
            command,
            CancellationToken.None);

        Assert.That(result, Is.Not.Null);

        Assert.That(
            result.Code,
            Is.EqualTo("PP-SALES"));

        Assert.That(
            _repository.Items.Count,
            Is.EqualTo(1));

        Assert.That(
            _repository.Items[0].Lines.Count,
            Is.EqualTo(1));
    }

    [Test]
    public void CreatePostingProfileCommandHandler_RejectsInvalidPostingAccount()
    {
        var invalidAccount = Account.Create(
            Guid.NewGuid(),
            "1000",
            "الأصول",
            null,
            null,
            1,
            AccountClass.Asset,
            AccountType.Header,
            NormalBalance.Debit,
            isPostingAccount: false,
            isControlAccount: false,
            allowManualPosting: false,
            isSystemAccount: false,
            isActive: true,
            effectiveDate: null);

        _accountRepository.AddAsync(invalidAccount).GetAwaiter().GetResult();

        var handler = new CreatePostingProfileCommandHandler(
            _repository,
            _accountRepository,
            _mapper);

        var request = new CreatePostingProfileRequest(
            Code: "PP-BAD",
            Name: "ملف غير صالح",
            Module: "Accounting",
            DocumentType: "Test",
            IsActive: true,
            Lines:
            [
                new CreatePostingProfileLineRequest(
                    AccountRole: "Cash",
                    AccountId: invalidAccount.Id,
                    IsRequired: true)
            ]);

        Assert.ThrowsAsync<ConflictException>(async () =>
            await handler.Handle(
                new CreatePostingProfileCommand(request),
                CancellationToken.None));
    }

    [Test]
    public async Task GetPostingProfileByIdQueryHandler_ReturnsDto()
    {
        var profile = PostingProfile.Create(
            Guid.NewGuid(),
            "PP-SALES",
            "ملف ترحيل",
            "Sales",
            "SalesInvoice",
            true);

        await _repository.AddAsync(profile);

        var accountId = Guid.NewGuid();

        var line = PostingProfileLine.Create(
            Guid.NewGuid(),
            profile.Id,
            "SalesRevenue",
            accountId,
            true);

        /*
         * Query Handler أصبح يجلب الأسطر من Repository مستقل.
         */
        await _lineRepository.AddAsync(line);

        var handler = new GetPostingProfileByIdQueryHandler(
            _repository,
            _lineRepository,
            _mapper);

        var query = new GetPostingProfileByIdQuery(
            profile.Id);

        var dto = await handler.Handle(
            query,
            CancellationToken.None);

        Assert.That(dto, Is.Not.Null);

        Assert.That(
            dto.Code,
            Is.EqualTo("PP-SALES"));

        Assert.That(
            dto.Lines.Count,
            Is.EqualTo(1));

        Assert.That(
            dto.Lines[0].AccountRole,
            Is.EqualTo("SalesRevenue"));

        Assert.That(
            dto.Lines[0].AccountId,
            Is.EqualTo(accountId));
    }
    private static Account CreatePostingAccount() =>
        Account.Create(
            Guid.NewGuid(),
            "410100",
            "حساب ترحيل",
            null,
            null,
            1,
            AccountClass.Revenue,
            AccountType.Posting,
            NormalBalance.Credit,
            isPostingAccount: true,
            isControlAccount: false,
            allowManualPosting: false,
            isSystemAccount: false,
            isActive: true,
            effectiveDate: null);

}