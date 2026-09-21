using NUnit.Framework;
using OAS.Application.Accounting.PostingProfiles.Commands.CreatePostingProfile;
using OAS.Application.Accounting.PostingProfiles.Commands.UpdatePostingProfile;
using OAS.Application.Accounting.PostingProfiles.Mapping;
using OAS.Application.Accounting.PostingProfiles.Queries.GetPostingProfileById;
using OAS.Contracts.Accounting.PostingProfiles;
using OAS.Domain.Accounting.Entities;
using OAS.Tests.Accounting.Application.Common;

namespace OAS.Tests.Accounting.Application.PostingProfiles;

[TestFixture]
public class PostingProfileCommandAndQueryTests
{
    private FakeRepository<PostingProfile, Guid> _repository = null!;
    private PostingProfileMapper _mapper = null!;

    [SetUp]
    public void Setup()
    {
        _repository = new FakeRepository<PostingProfile, Guid>();
        _mapper = new PostingProfileMapper();
    }

    [Test]
    public async Task CreatePostingProfileCommandHandler_CreatesProfileWithLines()
    {
        var handler = new CreatePostingProfileCommandHandler(_repository, _mapper);
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
                    AccountId: Guid.NewGuid(),
                    IsRequired: true)
            ]);

        var command = new CreatePostingProfileCommand(request);
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.Code, Is.EqualTo("PP-SALES"));
        Assert.That(_repository.Items.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task GetPostingProfileByIdQueryHandler_ReturnsDto()
    {
        var profile = PostingProfile.Create(
            Guid.NewGuid(), "PP-SALES", "ملف ترحيل", "Sales", "SalesInvoice", true);
        await _repository.AddAsync(profile);

        var handler = new GetPostingProfileByIdQueryHandler(_repository, _mapper);
        var query = new GetPostingProfileByIdQuery(profile.Id);
        var dto = await handler.Handle(query, CancellationToken.None);

        Assert.That(dto, Is.Not.Null);
        Assert.That(dto.Code, Is.EqualTo("PP-SALES"));
    }
}
