using ClosedXML.Excel;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Contracts.Features.Employees.Import;

namespace OAS.Infrastructure.Features.Employees.Import;

public sealed class EmployeeExcelReader : IEmployeeExcelReader
{
    private static readonly string[] RequiredHeaders =
    [
        "الاسم الأول",
        "اسم العائلة",
        "رقم الهاتف",
        "المسمى الوظيفي",
        "تاريخ التوظيف",
        "مستحق للعمولة",
        "نشط"
    ];

    public Task<IReadOnlyList<EmployeeImportRowDto>> ReadAsync(Stream stream, CancellationToken cancellationToken = default)
    {
        if (stream is null) throw new ArgumentNullException(nameof(stream));
        if (!stream.CanRead) throw new InvalidOperationException("لا يمكن قراءة ملف Excel.");
        if (stream.CanSeek) stream.Position = 0;

        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault()
            ?? throw new InvalidOperationException("ملف Excel لا يحتوي على ورقة عمل.");
        var headerRow = worksheet.FirstRowUsed()
            ?? throw new InvalidOperationException("ملف Excel فارغ.");

        var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var cell in headerRow.CellsUsed())
        {
            var value = NormalizeHeader(cell.GetString());
            if (!string.IsNullOrWhiteSpace(value)) headers[value] = cell.Address.ColumnNumber;
        }

        foreach (var header in RequiredHeaders)
            if (!headers.ContainsKey(NormalizeHeader(header)))
                throw new InvalidOperationException($"العمود «{header}» غير موجود في ملف Excel.");

        var rows = new List<EmployeeImportRowDto>();
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (row.CellsUsed().All(cell => string.IsNullOrWhiteSpace(cell.GetFormattedString()))) continue;

            rows.Add(new EmployeeImportRowDto(
                row.RowNumber(),
                GetString(row, headers, "الاسم الأول"),
                GetString(row, headers, "اسم العائلة"),
                GetNullableString(row, headers, "رقم الهاتف"),
                GetOptionalString(row, headers, "البريد الإلكتروني"),
                GetOptionalString(row, headers, "الدولة"),
                GetOptionalString(row, headers, "المحافظة"),
                GetOptionalString(row, headers, "المدينة"),
                GetOptionalString(row, headers, "الرمز البريدي"),
                GetOptionalString(row, headers, "عنوان السكن"),
                GetNullableString(row, headers, "المسمى الوظيفي"),
                GetDate(row, headers, "تاريخ التوظيف"),
                GetString(row, headers, "مستحق للعمولة"),
                GetString(row, headers, "نشط")));
        }

        return Task.FromResult<IReadOnlyList<EmployeeImportRowDto>>(rows);
    }

    private static string GetString(IXLRow row, IReadOnlyDictionary<string, int> headers, string header) =>
        row.Cell(headers[NormalizeHeader(header)]).GetFormattedString().Trim();

    private static string? GetNullableString(IXLRow row, IReadOnlyDictionary<string, int> headers, string header)
    {
        var value = GetString(row, headers, header);
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? GetOptionalString(IXLRow row, IReadOnlyDictionary<string, int> headers, string header)
    {
        if (!headers.TryGetValue(NormalizeHeader(header), out var column)) return null;
        var value = row.Cell(column).GetFormattedString().Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static DateOnly? GetDate(IXLRow row, IReadOnlyDictionary<string, int> headers, string header)
    {
        var cell = row.Cell(headers[NormalizeHeader(header)]);
        if (cell.DataType == XLDataType.DateTime) return DateOnly.FromDateTime(cell.GetDateTime());
        if (cell.DataType == XLDataType.Number && cell.Style.DateFormat.Format != "General")
        {
            try { return DateOnly.FromDateTime(cell.GetDateTime()); } catch { }
        }

        var value = cell.GetFormattedString().Trim();
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (DateOnly.TryParse(value, out var parsed)) return parsed;
        if (DateTime.TryParse(value, out var dateTime)) return DateOnly.FromDateTime(dateTime);
        return null;
    }

    private static string NormalizeHeader(string value) =>
        value.Trim().Replace('\u00A0', ' ').Replace("  ", " ");
}
