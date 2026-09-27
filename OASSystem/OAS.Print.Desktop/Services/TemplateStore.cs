using OAS.Printing.Core.Models;
using OAS.Printing.Core.Services;
using System.IO;

namespace OAS.Print.Desktop.Services;

public sealed class TemplateStore
{
    public string FolderPath { get; }

    public TemplateStore()
    {
        FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OAS", "Print", "Templates");
        Directory.CreateDirectory(FolderPath);
        SeedTemplates();
    }

    public IReadOnlyList<TemplateDefinition> LoadAll()
    {
        var result = new List<TemplateDefinition>();
        foreach (var file in Directory.GetFiles(FolderPath, "*.json").OrderBy(x => x))
        {
            try { result.Add(TemplateSerializer.Deserialize(File.ReadAllText(file))); }
            catch { /* Ignore malformed user file; UI can continue. */ }
        }
        return result;
    }

    public void Save(TemplateDefinition template)
    {
        var safeCode = string.Concat(template.Code.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) ? '_' : ch));
        if (string.IsNullOrWhiteSpace(safeCode)) safeCode = template.Id.ToString("N");
        File.WriteAllText(Path.Combine(FolderPath, safeCode + ".json"), TemplateSerializer.Serialize(template));
    }

    public void Delete(TemplateDefinition template)
    {
        foreach (var file in Directory.GetFiles(FolderPath, "*.json"))
        {
            try
            {
                var existing = TemplateSerializer.Deserialize(File.ReadAllText(file));
                if (existing.Id == template.Id)
                    File.Delete(file);
            }
            catch { }
        }
    }

    private void SeedTemplates()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Templates");
        if (!Directory.Exists(source)) return;
        foreach (var file in Directory.GetFiles(source, "*.json"))
        {
            var target = Path.Combine(FolderPath, Path.GetFileName(file));
            if (!File.Exists(target)) File.Copy(file, target);
        }
    }
}
