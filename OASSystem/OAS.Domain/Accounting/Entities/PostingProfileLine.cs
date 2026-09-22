using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PostingProfileLine : AuditableEntity<Guid>
{
    private PostingProfileLine()
    {
    }

    private PostingProfileLine(
        Guid id,
        Guid postingProfileId,
        string accountRole,
        Guid accountId,
        bool isRequired)
    {
        Id = id;
        PostingProfileId = postingProfileId;
        AccountRole = accountRole;
        AccountId = accountId;
        IsRequired = isRequired;
    }

    public Guid PostingProfileId { get; private set; }

    public string AccountRole { get; private set; } = null!;

    public Guid AccountId { get; private set; }

    public bool IsRequired { get; private set; }

    public static PostingProfileLine Create(
        Guid id,
        Guid postingProfileId,
        string accountRole,
        Guid accountId,
        bool isRequired)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id is required.", nameof(id));

        if (postingProfileId == Guid.Empty)
            throw new ArgumentException(
                "Posting profile id is required.",
                nameof(postingProfileId));

        if (string.IsNullOrWhiteSpace(accountRole))
            throw new ArgumentException(
                "Account role is required.",
                nameof(accountRole));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        return new PostingProfileLine(
            id,
            postingProfileId,
            accountRole.Trim(),
            accountId,
            isRequired);
    }

    public void Update(
        string accountRole,
        Guid accountId,
        bool isRequired)
    {
        if (string.IsNullOrWhiteSpace(accountRole))
            throw new ArgumentException(
                "Account role is required.",
                nameof(accountRole));

        if (accountId == Guid.Empty)
            throw new ArgumentException("Account id is required.", nameof(accountId));

        AccountRole = accountRole.Trim();
        AccountId = accountId;
        IsRequired = isRequired;
    }
}
