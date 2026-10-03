namespace OAS.Application.Features.Employees.Abstractions;

public sealed record EmployeeDocumentFileData(string StorageKey, string ContentType, byte[] Content);

public interface IEmployeeDocumentStore
{
    Task<string> SaveAsync(Guid employeeId, string originalFileName, string contentType, byte[] content, CancellationToken cancellationToken = default);
    Task<EmployeeDocumentFileData?> GetAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? storageKey, CancellationToken cancellationToken = default);
}
