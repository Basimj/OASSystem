namespace OAS.Application.Accounting.Abstractions;
public interface IManagedAccountGuard
{
    Task<bool> IsManagedAsync(Guid accountId,CancellationToken cancellationToken=default);
}
