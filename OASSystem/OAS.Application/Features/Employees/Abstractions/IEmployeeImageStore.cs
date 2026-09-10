namespace OAS.Application.Features.Employees.Abstractions;

public sealed record EmployeeImageData(
    string RelativePath,
    string ContentType,
    byte[] Content,
    DateTimeOffset UpdatedAtUtc);

public interface IEmployeeImageStore
{
    Task<EmployeeImageData?> GetAsync(string relativePath, CancellationToken cancellationToken = default);
    Task<string> SaveAsync(Guid employeeId, string contentType, byte[] content, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default);
}
