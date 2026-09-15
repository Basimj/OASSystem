using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class PostingProfile : Entity<Guid>
{
    private PostingProfile()
    {
    }

    private PostingProfile(
        Guid id,
        string code,
        string name,
        string module,
        string documentType,
        bool isActive)
    {
        Id = id;
        Code = code;
        Name = name;
        Module = module;
        DocumentType = documentType;
        IsActive = isActive;
    }

    public string Code { get; private set; } = null!;

    public string Name { get; private set; } = null!;

    public string Module { get; private set; } = null!;

    public string DocumentType { get; private set; } = null!;

    public bool IsActive { get; private set; }

    public static PostingProfile Create(
        Guid id,
        string code,
        string name,
        string module,
        string documentType,
        bool isActive)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Posting profile id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("Module is required.", nameof(module));

        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("Document type is required.", nameof(documentType));

        return new PostingProfile(
            id,
            code.Trim(),
            name.Trim(),
            module.Trim(),
            documentType.Trim(),
            isActive);
    }

    public void UpdateDetails(
        string code,
        string name,
        string module,
        string documentType)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required.", nameof(code));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name is required.", nameof(name));

        if (string.IsNullOrWhiteSpace(module))
            throw new ArgumentException("Module is required.", nameof(module));

        if (string.IsNullOrWhiteSpace(documentType))
            throw new ArgumentException("Document type is required.", nameof(documentType));

        Code = code.Trim();
        Name = name.Trim();
        Module = module.Trim();
        DocumentType = documentType.Trim();
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
    }
}