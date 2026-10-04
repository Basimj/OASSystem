using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Documents;

namespace OAS.Client.Features.Employees.Services;

public interface IEmployeeDocumentClientService
{
    Task<IReadOnlyList<EmployeeDocumentDto>> GetAsync(Guid employeeId, CancellationToken cancellationToken = default);
    Task<ApiCallResult<Guid>> UploadAsync(Guid employeeId, byte documentType, string title, DateOnly? issueDate, DateOnly? expiryDate, string? notes, byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ApiCallResult<bool>> UpdateAsync(Guid documentId, UpdateEmployeeDocumentRequest request, CancellationToken cancellationToken = default);
    Task<ApiCallResult<bool>> SetStatusAsync(Guid documentId, SetEmployeeDocumentStatusRequest request, CancellationToken cancellationToken = default);
    Task<byte[]> DownloadAsync(Guid documentId, CancellationToken cancellationToken = default);
}
