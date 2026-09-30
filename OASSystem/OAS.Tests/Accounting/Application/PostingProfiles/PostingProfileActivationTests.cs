using NUnit.Framework;
using OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;
using OAS.Application.Accounting.PostingProfiles.Commands.SetPostingProfileStatus;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.PostingProfiles;

[TestFixture]
public class PostingProfileActivationTests
{
    [Test]
    public async Task CreatingActiveProfile_DeactivatesPreviousProfileForSameScope()
    {
        var previous = PostingProfile.Create(
            Guid.NewGuid(),
            "PP-SALES-OLD",
            "Old sales profile",
            "Sales",
            "SalesInvoice",
            true);

        var repository = new FakeRepository<PostingProfile, Guid>([previous]);
        var accountRepository = new FakeRepository<Account, Guid>();

        var handler = new CreatePostingProfileCommandHandler(
            repository,
            accountRepository,
            new PostingProfileMapper());

        var created = await handler.Handle(
            new CreatePostingProfileCommand(
                new CreatePostingProfileRequest(
                    "PP-SALES-NEW",
                    "New sales profile",
                    "Sales",
                    "SalesInvoice",
                    [],
                    true)),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(created.IsActive, Is.True);
            Assert.That(previous.IsActive, Is.False);
            Assert.That(repository.Items.Count(x =>
                x.IsActive &&
                x.Module == "Sales" &&
                x.DocumentType == "SalesInvoice"), Is.EqualTo(1));
        });
    }

    [Test]
    public async Task ActivatingProfile_DeactivatesCompetingProfileForSameScope()
    {
        var current = PostingProfile.Create(
            Guid.NewGuid(),
            "PP-SALES-A",
            "Sales A",
            "Sales",
            "SalesInvoice",
            false);

        var competing = PostingProfile.Create(
            Guid.NewGuid(),
            "PP-SALES-B",
            "Sales B",
            "Sales",
            "SalesInvoice",
            true);

        var repository = new FakeRepository<PostingProfile, Guid>([current, competing]);
        var handler = new SetPostingProfileStatusCommandHandler(repository);

        await handler.Handle(
            new SetPostingProfileStatusCommand(
                current.Id,
                new SetPostingProfileStatusRequest(
                    true,
                    Convert.ToBase64String(current.RowVersion))),
            CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(current.IsActive, Is.True);
            Assert.That(competing.IsActive, Is.False);
        });
    }

    [Test]
    public async Task CreatingProfileForDifferentScope_DoesNotDeactivateOtherProfile()
    {
        var salesInvoice = PostingProfile.Create(
            Guid.NewGuid(),
            "PP-SALES",
            "Sales profile",
            "Sales",
            "SalesInvoice",
            true);

        var repository = new FakeRepository<PostingProfile, Guid>([salesInvoice]);
        var accountRepository = new FakeRepository<Account, Guid>();

        var handler = new CreatePostingProfileCommandHandler(
            repository,
            accountRepository,
            new PostingProfileMapper());

        await handler.Handle(
            new CreatePostingProfileCommand(
                new CreatePostingProfileRequest(
                    "PP-PURCHASE",
                    "Purchase profile",
                    "Purchasing",
                    "PurchaseInvoice",
                    [],
                    true)),
            CancellationToken.None);

        Assert.That(salesInvoice.IsActive, Is.True);
    }
}
