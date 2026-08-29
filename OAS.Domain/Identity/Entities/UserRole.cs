using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Identity.Entities;

public sealed class UserRole : Entity<Guid>
{
    private UserRole() { }

    private UserRole(Guid id, Guid userId, Guid roleId)
    {
        if (id == Guid.Empty || userId == Guid.Empty || roleId == Guid.Empty)
            throw new DomainException("User-role identifiers are required.");
        Id = id;
        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }

    public static UserRole Create(Guid id, Guid userId, Guid roleId) => new(id, userId, roleId);
}
