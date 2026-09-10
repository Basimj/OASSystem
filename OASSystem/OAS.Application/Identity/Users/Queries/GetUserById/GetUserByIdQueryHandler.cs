using MediatR;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Identity.Abstractions;
using OAS.Application.Identity.Users.Common;
using OAS.Application.Identity.Users.Mapping;
using OAS.Contracts.Identity.Users;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Users.Queries.GetUserById;

public sealed class GetUserByIdQueryHandler(
    IIdentityRepository repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
    : IRequestHandler<GetUserByIdQuery, UserDetailsDto>
{
    public async Task<UserDetailsDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var record = await repository.GetUserAsync(request.UserId, false, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);

        await UserManagementGuard.EnsureCanManageTargetAsync(record, currentUser, repository, cancellationToken);

        var auditUserIds = new HashSet<Guid>();
        if (Guid.TryParse(record.User.CreatedBy, out var createdById)) auditUserIds.Add(createdById);
        if (Guid.TryParse(record.User.LastModifiedBy, out var modifiedById)) auditUserIds.Add(modifiedById);
        var auditUserNames = await repository.GetUserNamesAsync(auditUserIds, cancellationToken);

        var createdBy = ResolveAuditActor(record.User.CreatedBy, auditUserNames);
        var modifiedBy = ResolveAuditActor(record.User.LastModifiedBy, auditUserNames);
        return UserMapping.ToDetails(record, timeProvider.GetUtcNow(), createdBy, modifiedBy);
    }

    private static string? ResolveAuditActor(string? auditValue, IReadOnlyDictionary<Guid, string> userNames)
    {
        if (string.IsNullOrWhiteSpace(auditValue)) return null;
        if (!Guid.TryParse(auditValue, out var userId)) return auditValue;
        return userNames.TryGetValue(userId, out var userName) ? userName : null;
    }
}
