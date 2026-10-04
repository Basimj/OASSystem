using OAS.Client.Services.Http;
using OAS.Contracts.Features.Employees.Documents;

namespace OAS.Client.Features.Employees.Services;

public sealed class EmployeeDocumentClientService(OasApiClient apiClient) : IEmployeeDocumentClientService
{
    public async Task<IReadOnlyList<EmployeeDocumentDto>> GetAsync(Guid employeeId, CancellationToken cancellationToken = default)
        => await apiClient.GetAsync<IReadOnlyList<EmployeeDocumentDto>>($"api/employees/{employeeId:D}/documents", cancellationToken) ?? [];

    public async Task<ApiCallResult<Guid>> UploadAsync(Guid employeeId, byte documentType, string title, DateOnly? issueDate, DateOnly? expiryDate, string? notes, byte[] content, string fileName, string contentType, CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream(content, writable: false);
        return await apiClient.UploadMultipartResultAsync<Guid>(
            $"api/employees/{employeeId:D}/documents",
            stream,
            fileName,
            contentType,
            new Dictionary<string, string?>
            {
                ["documentType"] = documentType.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["title"] = title,
                ["issueDate"] = issueDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["expiryDate"] = expiryDate?.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture),
                ["notes"] = notes
            },
            cancellationToken);
    }

    public async Task<ApiCallResult<bool>> UpdateAsync(Guid documentId, UpdateEmployeeDocumentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await apiClient.PutAsync<UpdateEmployeeDocumentRequest, object>($"api/employees/documents/{documentId:D}", request, cancellationToken);
            return ApiCallResult<bool>.Success(true);
        }
        catch (ApiClientException ex) { return ApiCallResult<bool>.Failure(ex.Error); }
    }

    public async Task<ApiCallResult<bool>> SetStatusAsync(Guid documentId, SetEmployeeDocumentStatusRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            await apiClient.PostAsync<SetEmployeeDocumentStatusRequest, object>($"api/employees/documents/{documentId:D}/status", request, cancellationToken);
            return ApiCallResult<bool>.Success(true);
        }
        catch (ApiClientException ex) { return ApiCallResult<bool>.Failure(ex.Error); }
    }

    public Task<byte[]> DownloadAsync(Guid documentId, CancellationToken cancellationToken = default)
        => apiClient.GetFileAsync($"api/employees/documents/{documentId:D}/file", cancellationToken);
}
