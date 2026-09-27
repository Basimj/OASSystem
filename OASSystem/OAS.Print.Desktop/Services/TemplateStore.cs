using OAS.Printing.Core.Models;
using OAS.Printing.Core.Services;

namespace OAS.Print.Desktop.Services;

public sealed class TemplateStore
{
    public string FolderPath { get; }

    public TemplateStore()
    {
        FolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OAS",
            "Print",
            "Templates");

        Directory.CreateDirectory(FolderPath);
        SeedTemplates();
    }

    public IReadOnlyList<TemplateDefinition> LoadAll()
    {
        var loaded = new List<(TemplateDefinition Template, string File, DateTime WrittenAt)>();

        foreach (var file in Directory.GetFiles(FolderPath, "*.json"))
        {
            try
            {
                var template = TemplateSerializer.Deserialize(File.ReadAllText(file));
                loaded.Add((template, file, File.GetLastWriteTimeUtc(file)));
            }
            catch
            {
                // A malformed custom template must not prevent the printing app from starting.
            }
        }

        return loaded
            .GroupBy(x => x.Template.Id)
            .Select(g => g
                .OrderByDescending(x => x.Template.Version)
                .ThenByDescending(x => x.WrittenAt)
                .First().Template)
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.DocumentType)
            .ThenBy(x => x.Name)
            .ToArray();
    }

    public void Save(TemplateDefinition template)
    {
        DeleteFilesForTemplate(template.Id);
        WriteTemplate(template);
    }

    public void Delete(TemplateDefinition template) => DeleteFilesForTemplate(template.Id);

    private void SeedTemplates()
    {
        var sourceFolder = Path.Combine(AppContext.BaseDirectory, "Templates");
        if (!Directory.Exists(sourceFolder))
            return;

        foreach (var file in Directory.GetFiles(sourceFolder, "*.json"))
        {
            try
            {
                var packaged = TemplateSerializer.Deserialize(File.ReadAllText(file));
                var existing = FindLocalCopies(packaged.Id).ToArray();
                var newestLocalVersion = existing.Length == 0
                    ? -1
                    : existing.Max(x => x.Template.Version);

                // Built-in templates are upgraded only when the packaged template has a newer
                // version. Custom templates (different Id) are never overwritten.
                if (existing.Length == 0 || packaged.Version > newestLocalVersion)
                {
                    foreach (var item in existing)
                        TryDelete(item.File);

                    WriteTemplate(packaged);
                }
            }
            catch
            {
                // Ignore a damaged packaged template and continue loading the application.
            }
        }
    }

    private IReadOnlyList<(TemplateDefinition Template, string File)> FindLocalCopies(Guid id)
    {
        var result = new List<(TemplateDefinition Template, string File)>();
        foreach (var file in Directory.GetFiles(FolderPath, "*.json"))
        {
            try
            {
                var template = TemplateSerializer.Deserialize(File.ReadAllText(file));
                if (template.Id == id)
                    result.Add((template, file));
            }
            catch
            {
                // Ignore malformed files.
            }
        }
        return result;
    }

    private void DeleteFilesForTemplate(Guid id)
    {
        foreach (var item in FindLocalCopies(id).ToArray())
            TryDelete(item.File);
    }

    private void WriteTemplate(TemplateDefinition template)
    {
        var safeCode = string.Concat(
            template.Code.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));

        if (string.IsNullOrWhiteSpace(safeCode))
            safeCode = template.Id.ToString("N");

        File.WriteAllText(
            Path.Combine(FolderPath, safeCode + ".json"),
            TemplateSerializer.Serialize(template));
    }

    private static void TryDelete(string file)
    {
        try
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        catch
        {
            // Keep application startup resilient even if a file is temporarily locked.
        }
    }
}
