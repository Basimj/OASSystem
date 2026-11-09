using NUnit.Framework;
using OAS.Application.Accounting.PostingProfiles.Specifications;
using OAS.Application.Accounting.PostingProfiles.Commands.SetPostingProfileStatus;
using OAS.Application.Common.Exceptions;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.PostingProfiles;

[TestFixture]
public class PostingProfileDirectoryAndSafetyTests
{
    [Test]
    public void DirectoryFilter_CombinesSearchModuleDocumentAndStatus()
    {
        var spec = new PostingProfilePageSpecification().CreatePageSpecification(new PageRequest
        {
            Search = "SALES",
            PostingModule = "Sales",
            PostingDocumentType = "SalesInvoice",
            Status = "active"
        });
        var matches = spec.Criteria!.Compile();
        var wanted = PostingProfile.Create(Guid.NewGuid(), "PP-SALES", "ترحيل المبيعات", "Sales", "SalesInvoice", true);
        var wrongDocument = PostingProfile.Create(Guid.NewGuid(), "PP-SALES-RETURN", "مرجع", "Sales", "SalesReturn", true);
        var inactive = PostingProfile.Create(Guid.NewGuid(), "PP-SALES-OLD", "قديم", "Sales", "SalesInvoice", false);
        var wrongModule = PostingProfile.Create(Guid.NewGuid(), "PP-SALES-PUR", "تسوية", "Purchasing", "SalesInvoice", true);

        Assert.Multiple(() =>
        {
            Assert.That(matches(wanted), Is.True);
            Assert.That(matches(wrongDocument), Is.False);
            Assert.That(matches(inactive), Is.False);
            Assert.That(matches(wrongModule), Is.False);
            Assert.That(spec.Skip, Is.EqualTo(0));
            Assert.That(spec.Take, Is.EqualTo(PageRequest.DefaultPageSize));
        });
    }

    [Test]
    public void ActivatingIncompleteProfile_RejectsWithoutMutatingStatus()
    {
        var profile = PostingProfile.Create(Guid.NewGuid(), "PP-EMPTY", "ملف غير مكتمل", "Sales", "SalesInvoice", false);
        var handler = new SetPostingProfileStatusCommandHandler(
            new FakeRepository<PostingProfile, Guid>([profile]),
            new FakeRepository<PostingProfileLine, Guid>(),
            new FakeRepository<Account, Guid>());

        Assert.ThrowsAsync<ConflictException>(async () => await handler.Handle(
            new SetPostingProfileStatusCommand(profile.Id,
                new SetPostingProfileStatusRequest(true, Convert.ToBase64String(profile.RowVersion))),
            CancellationToken.None));
        Assert.That(profile.IsActive, Is.False);
    }
}
