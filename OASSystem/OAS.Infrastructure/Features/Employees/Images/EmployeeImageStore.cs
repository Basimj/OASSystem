using Microsoft.Extensions.Configuration;
using OAS.Application.Features.Employees.Abstractions;

namespace OAS.Infrastructure.Features.Employees.Images;

public sealed class EmployeeImageStore : IEmployeeImageStore
{
    private const string RelativeRoot = "Resources/Employees/Photos";

    private static readonly IReadOnlyDictionary<string, string> Extensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    private readonly string _rootDirectory;
    private readonly StringComparison _pathComparison =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public EmployeeImageStore(IConfiguration configuration)
    {
        var configuredRoot = configuration["Storage:EmployeePhotosRoot"];
        var root = string.IsNullOrWhiteSpace(configuredRoot) ? RelativeRoot : configuredRoot.Trim();
        _rootDirectory = Path.GetFullPath(
            Path.IsPathRooted(root)
                ? root
                : Path.Combine(Directory.GetCurrentDirectory(), root));
    }

    public async Task<EmployeeImageData?> GetAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return null;

        var fullPath = ResolveSafePath(relativePath);
        if (!File.Exists(fullPath)) return null;

        var extension = Path.GetExtension(fullPath);
        var contentType = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };

        var content = await File.ReadAllBytesAsync(fullPath, cancellationToken);
        var updatedAt = new DateTimeOffset(File.GetLastWriteTimeUtc(fullPath), TimeSpan.Zero);
        return new EmployeeImageData(ToRelativePath(fullPath), contentType, content, updatedAt);
    }

    public async Task<string> SaveAsync(
        Guid employeeId,
        string contentType,
        byte[] content,
        CancellationToken cancellationToken = default)
    {
        if (!Extensions.TryGetValue(contentType, out var extension))
            throw new InvalidOperationException("Unsupported employee photo content type.");

        Directory.CreateDirectory(_rootDirectory);

        var fileName = $"{employeeId:N}-{Guid.NewGuid():N}{extension}";
        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, fileName));
        EnsureInsideStorageRoot(fullPath);

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);
        return ToRelativePath(fullPath);
    }

    public Task DeleteAsync(string? relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(relativePath)) return Task.CompletedTask;

        var fullPath = ResolveSafePath(relativePath);
        if (File.Exists(fullPath)) File.Delete(fullPath);
        return Task.CompletedTask;
    }

    private string ResolveSafePath(string relativePath)
    {
        var normalizedSlash = relativePath.Trim().Replace('\\', '/');
        var expectedPrefix = RelativeRoot + "/";
        if (!normalizedSlash.StartsWith(expectedPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Invalid employee photo path.");

        // Persisted paths are storage keys, never arbitrary operating-system paths.
        var fileName = Path.GetFileName(normalizedSlash);
        if (string.IsNullOrWhiteSpace(fileName) || fileName is "." or "..")
            throw new InvalidOperationException("Invalid employee photo path.");

        var fullPath = Path.GetFullPath(Path.Combine(_rootDirectory, fileName));
        EnsureInsideStorageRoot(fullPath);
        return fullPath;
    }

    private void EnsureInsideStorageRoot(string fullPath)
    {
        var rootWithSeparator = _rootDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSeparator, _pathComparison))
            throw new InvalidOperationException("Invalid employee photo path.");
    }

    private static string ToRelativePath(string fullPath) =>
        $"{RelativeRoot}/{Path.GetFileName(fullPath)}";
}
