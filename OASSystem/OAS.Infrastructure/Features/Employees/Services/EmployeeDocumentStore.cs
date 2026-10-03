using Microsoft.Extensions.Configuration;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Domain.Exceptions;

namespace OAS.Infrastructure.Features.Employees.Services;

public sealed class EmployeeDocumentStore : IEmployeeDocumentStore
{
    private const string RelativeRoot = "Resources/Employees/Documents";
    private static readonly Dictionary<string, string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["application/pdf"] = ".pdf",
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png"
    };
    private readonly string _rootDirectory;
    private readonly StringComparison _pathComparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public EmployeeDocumentStore(IConfiguration configuration)
    {
        var configured = configuration["Storage:EmployeeDocumentsRoot"];
        var root = string.IsNullOrWhiteSpace(configured) ? RelativeRoot : configured.Trim();
        _rootDirectory = Path.GetFullPath(Path.IsPathRooted(root) ? root : Path.Combine(Directory.GetCurrentDirectory(), root));
    }

    public async Task<string> SaveAsync(Guid employeeId, string originalFileName, string contentType, byte[] content, CancellationToken cancellationToken = default)
    {
        if (!AllowedExtensions.TryGetValue(contentType, out var extension)) throw new DomainException("Unsupported employee document content type.");
        ValidateSignature(contentType, content);
        Directory.CreateDirectory(_rootDirectory);
        var employeeDirectory = Path.GetFullPath(Path.Combine(_rootDirectory, employeeId.ToString("N")));
        EnsureInsideStorageRoot(employeeDirectory + Path.DirectorySeparatorChar);
        Directory.CreateDirectory(employeeDirectory);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.GetFullPath(Path.Combine(employeeDirectory, fileName));
        EnsureInsideStorageRoot(fullPath);
        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return $"{RelativeRoot}/{employeeId:N}/{fileName}";
    }

    public async Task<EmployeeDocumentFileData?> GetAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storageKey)) return null;
        var fullPath = ResolveSafePath(storageKey);
        if (!File.Exists(fullPath)) return null;
        var extension = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = extension switch { ".pdf" => "application/pdf", ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", _ => "application/octet-stream" };
        return new EmployeeDocumentFileData(storageKey, contentType, await File.ReadAllBytesAsync(fullPath, cancellationToken));
    }

    public Task DeleteAsync(string? storageKey, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(storageKey)) return Task.CompletedTask;
        var fullPath = ResolveSafePath(storageKey);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string ResolveSafePath(string storageKey)
    {
        var normalized = storageKey.Trim().Replace('\\', '/');
        var prefix = RelativeRoot + "/";
        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid employee document storage key.");
        var relative = normalized[prefix.Length..];
        if (relative.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(relative)) throw new InvalidOperationException("Invalid employee document storage key.");
        var full = Path.GetFullPath(Path.Combine(_rootDirectory, relative.Replace('/', Path.DirectorySeparatorChar)));
        EnsureInsideStorageRoot(full);
        return full;
    }

    private void EnsureInsideStorageRoot(string fullPath)
    {
        var root = _rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(root, _pathComparison)) throw new InvalidOperationException("Invalid employee document storage path.");
    }

    private static void ValidateSignature(string contentType, byte[] content)
    {
        var valid = contentType switch
        {
            "application/pdf" => content.Length >= 5 && content[0] == 0x25 && content[1] == 0x50 && content[2] == 0x44 && content[3] == 0x46 && content[4] == 0x2D,
            "image/jpeg" => content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF,
            "image/png" => content.Length >= 8 && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47 && content[4] == 0x0D && content[5] == 0x0A && content[6] == 0x1A && content[7] == 0x0A,
            _ => false
        };
        if (!valid) throw new DomainException("Employee document content does not match its declared file type.");
    }
}
