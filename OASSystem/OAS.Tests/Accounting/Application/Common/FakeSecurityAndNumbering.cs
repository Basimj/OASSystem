using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Security;

namespace OAS.Tests.Accounting.Application.Common;

public class FakeCurrentUser : ICurrentUser
{
    public string? UserId { get; set; } = Guid.NewGuid().ToString();
    public string? UserName { get; set; } = "test-user";
    public string? Email { get; set; } = "test@example.com";
    public bool IsAuthenticated => true;
    public IReadOnlyList<string> Roles { get; set; } = ["Admin"];
    public IReadOnlyList<string> Permissions { get; set; } = [];

    public bool IsInRole(string role) => Roles.Contains(role);
    public bool HasPermission(string permission) => Permissions.Contains(permission);
}

public class FakePermissionChecker : IPermissionChecker
{
    public bool HasPermissionResult { get; set; } = true;

    public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HasPermissionResult);
    }
}

public class FakeSequenceNumberGenerator : ISequenceNumberGenerator
{
    private long _current = 1;

    public Task<long> NextAsync(string sequenceName, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_current++);
    }
}
