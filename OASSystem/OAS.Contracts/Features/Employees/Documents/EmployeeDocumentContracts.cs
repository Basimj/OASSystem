namespace OAS.Contracts.Features.Employees.Documents;

public sealed record EmployeeDocumentDto(Guid Id, string DocumentCode, Guid EmployeeId, byte DocumentType, string Title, string OriginalFileName, string ContentType, long FileSize, string Sha256Hash, DateOnly? IssueDate, DateOnly? ExpiryDate, bool IsActive, string? Notes, string RowVersion, DateTimeOffset CreatedAtUtc);
public sealed record UpdateEmployeeDocumentRequest(byte DocumentType, string Title, DateOnly? IssueDate, DateOnly? ExpiryDate, string? Notes, string RowVersion);
public sealed record SetEmployeeDocumentStatusRequest(bool IsActive, string RowVersion);
