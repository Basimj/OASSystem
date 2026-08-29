namespace OAS.Application.Abstractions.Security;

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(string permission, CancellationToken cancellationToken = default);
}
