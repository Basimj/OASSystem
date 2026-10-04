using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Features.Employees.Enums;

namespace OAS.Domain.Features.Employees.Entities;

public sealed class EmployeeDocument : AuditableEntity<Guid>
{
    private EmployeeDocument() { }

    private EmployeeDocument(Guid id, string documentCode, Guid employeeId, EmployeeDocumentType documentType, string title, string storageKey, string originalFileName, string contentType, long fileSize, string sha256Hash, DateOnly? issueDate, DateOnly? expiryDate, string? notes)
    {
        if (id == Guid.Empty) throw new DomainException("Employee document id is required.");
        if (string.IsNullOrWhiteSpace(documentCode)) throw new DomainException("Document code is required.");
        if (employeeId == Guid.Empty) throw new DomainException("Employee is required.");
        Id = id;
        DocumentCode = documentCode.Trim();
        EmployeeId = employeeId;
        StorageKey = Required(storageKey, 512, "Storage key");
        OriginalFileName = Required(originalFileName, 260, "Original file name");
        ContentType = Required(contentType, 100, "Content type");
        if (fileSize <= 0) throw new DomainException("File size must be greater than zero.");
        FileSize = fileSize;
        Sha256Hash = Required(sha256Hash, 64, "SHA-256 hash");
        IsActive = true;
        UpdateMetadata(documentType, title, issueDate, expiryDate, notes);
    }

    public string DocumentCode { get; private set; } = string.Empty;
    public Guid EmployeeId { get; private set; }
    public EmployeeDocumentType DocumentType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string StorageKey { get; private set; } = string.Empty;
    public string OriginalFileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }
    public string Sha256Hash { get; private set; } = string.Empty;
    public DateOnly? IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static EmployeeDocument Create(Guid id, string documentCode, Guid employeeId, EmployeeDocumentType documentType, string title, string storageKey, string originalFileName, string contentType, long fileSize, string sha256Hash, DateOnly? issueDate, DateOnly? expiryDate, string? notes)
        => new(id, documentCode, employeeId, documentType, title, storageKey, originalFileName, contentType, fileSize, sha256Hash, issueDate, expiryDate, notes);

    public void UpdateMetadata(EmployeeDocumentType documentType, string title, DateOnly? issueDate, DateOnly? expiryDate, string? notes)
    {
        if (!Enum.IsDefined(documentType)) throw new DomainException("Document type is invalid.");
        title = Required(title, 200, "Document title");
        if (issueDate.HasValue && expiryDate.HasValue && expiryDate.Value < issueDate.Value) throw new DomainException("Document expiry date cannot be before issue date.");
        notes = Normalize(notes);
        if (notes is { Length: > 500 }) throw new DomainException("Document notes cannot exceed 500 characters.");
        DocumentType = documentType;
        Title = title;
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        Notes = notes;
    }

    public void SetActive(bool isActive) => IsActive = isActive;

    private static string Required(string value, int maxLength, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException($"{field} is required.");
        value = value.Trim();
        if (value.Length > maxLength) throw new DomainException($"{field} cannot exceed {maxLength} characters.");
        return value;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
