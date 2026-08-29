using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Identity.Entities;

public sealed class UserPasswordHistory : Entity<Guid>
{
    private UserPasswordHistory() { }

    private UserPasswordHistory(Guid id, Guid userId, string passwordHash, DateTimeOffset createdAtUtc)
    {
        Id = id;
        UserId = userId;
        PasswordHash = passwordHash;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid UserId { get; private set; }
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static UserPasswordHistory Create(Guid id, Guid userId, string passwordHash, DateTimeOffset createdAtUtc)
    {
        if (id == Guid.Empty || userId == Guid.Empty) throw new DomainException("Password history identifiers are required.");
        if (string.IsNullOrWhiteSpace(passwordHash)) throw new DomainException("Password hash is required.");
        return new UserPasswordHistory(id, userId, passwordHash, createdAtUtc);
    }
}
