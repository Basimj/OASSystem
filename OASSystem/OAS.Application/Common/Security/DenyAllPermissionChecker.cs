using OAS.Application.Abstractions.Security;

namespace OAS.Application.Common.Security;

public sealed class DenyAllPermissionChecker : IPermissionChecker
{
    public Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default) => Task.FromResult(false);
}
