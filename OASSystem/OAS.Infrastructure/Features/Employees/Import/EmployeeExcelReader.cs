using ClosedXML.Excel;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Contracts.Features.Employees.Import;

namespace OAS.Infrastructure.Features.Employees.Import;

public sealed class EmployeeExcelReader : IEmployeeExcelReader
{
    private static readonly string[] RequiredHeaders =
    [
        "رمز الموظف",
        "الاسم الأول",
        "اسم العائلة",
        "رقم الهاتف",
        "المسمى الوظيفي",
        "تاريخ التوظيف",
        "موظف مبيعات",
        "فني",
        "مستحق للعمولة",
        "نشط",
        "الملاحظات"
    ];

    public Task<IReadOnlyList<EmployeeImportRowDto>> ReadAsync(
        Stream stream,
        CancellationToken cancellationToken = default)
    {
        if (stream is null)
            throw new ArgumentNullException(nameof(stream));

        if (!stream.CanRead)
            throw new InvalidOperationException(
                "لا يمكن قراءة ملف Excel.");

        if (stream.CanSeek)
            stream.Position = 0;

        using var workbook = new XLWorkbook(stream);

        var worksheet = workbook.Worksheets.FirstOrDefault();

        if (worksheet is null)
        {
            throw new InvalidOperationException(
                "ملف Excel لا يحتوي على ورقة عمل.");
        }

        var headerRow = worksheet.FirstRowUsed();

        if (headerRow is null)
        {
            throw new InvalidOperationException(
                "ملف Excel فارغ.");
        }

        var headers = new Dictionary<string, int>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var cell in headerRow.CellsUsed())
        {
            var value = NormalizeHeader(cell.GetString());

            if (string.IsNullOrWhiteSpace(value))
                continue;

            headers[value] = cell.Address.ColumnNumber;
        }

        foreach (var requiredHeader in RequiredHeaders)
        {
            var normalizedRequiredHeader =
                NormalizeHeader(requiredHeader);

            if (!headers.ContainsKey(normalizedRequiredHeader))
            {
                throw new InvalidOperationException(
                    $"العمود «{requiredHeader}» غير موجود في ملف Excel.");
            }
        }

        var rows = new List<EmployeeImportRowDto>();

        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (IsEmptyRow(row))
                continue;

            var employeeCode =
                GetString(row, headers, "رمز الموظف");

            var firstName =
                GetString(row, headers, "الاسم الأول");

            var lastName =
                GetString(row, headers, "اسم العائلة");

            var phone =
                GetNullableString(row, headers, "رقم الهاتف");

            var jobTitle =
                GetNullableString(row, headers, "المسمى الوظيفي");

            var notes =
                GetNullableString(row, headers, "الملاحظات");

            var hireDate =
                GetDate(row, headers, "تاريخ التوظيف");

            var isSalesperson =
                GetString(row, headers, "موظف مبيعات");

            var isTechnician =
                GetString(row, headers, "فني");

            var isCommissionEligible =
                GetString(row, headers, "مستحق للعمولة");

            var isActive =
                GetString(row, headers, "نشط");

            rows.Add(
                new EmployeeImportRowDto(
                    row.RowNumber(),
                    employeeCode,
                    firstName,
                    lastName,
                    phone,
                    jobTitle,
                    hireDate,
                    notes,
                    isSalesperson,
                    isTechnician,
                    isCommissionEligible,
                    isActive));
        }

        return Task.FromResult<IReadOnlyList<EmployeeImportRowDto>>(rows);
    }

    private static bool IsEmptyRow(IXLRow row)
    {
        return row.CellsUsed()
            .All(cell => string.IsNullOrWhiteSpace(cell.GetFormattedString()));
    }

    private static string GetString(
        IXLRow row,
        IReadOnlyDictionary<string, int> headers,
        string header)
    {
        var columnNumber = headers[NormalizeHeader(header)];

        return row.Cell(columnNumber)
            .GetFormattedString()
            .Trim();
    }

    private static string? GetNullableString(
        IXLRow row,
        IReadOnlyDictionary<string, int> headers,
        string header)
    {
        var value = GetString(row, headers, header);

        return string.IsNullOrWhiteSpace(value)
            ? null
            : value;
    }

    private static DateOnly? GetDate(
        IXLRow row,
        IReadOnlyDictionary<string, int> headers,
        string header)
    {
        var cell = row.Cell(headers[NormalizeHeader(header)]);

        // Excel date stored as an actual date/time value
        if (cell.DataType == XLDataType.DateTime)
        {
            return DateOnly.FromDateTime(cell.GetDateTime());
        }

        // Excel number formatted as a date
        if (cell.DataType == XLDataType.Number &&
            cell.Style.DateFormat.Format != "General")
        {
            try
            {
                return DateOnly.FromDateTime(cell.GetDateTime());
            }
            catch
            {
                // Continue with text parsing below.
            }
        }

        var value = cell.GetFormattedString().Trim();

        if (string.IsNullOrWhiteSpace(value))
            return null;

        // ISO format
        if (DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                out var isoDate))
        {
            return isoDate;
        }

        // Common Excel/display formats
        string[] formats =
        [
            "M/d/yyyy",
            "MM/d/yyyy",
            "M/dd/yyyy",
            "MM/dd/yyyy",
            "d/M/yyyy",
            "dd/M/yyyy",
            "d/MM/yyyy",
            "dd/MM/yyyy"
        ];

        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(
                    value,
                    format,
                    out var parsedDate))
            {
                return parsedDate;
            }
        }

        // Final fallback
        if (DateTime.TryParse(
                value,
                out var dateTime))
        {
            return DateOnly.FromDateTime(dateTime);
        }

        return null;
    }

    private static string NormalizeHeader(string value)
    {
        return value
            .Trim()
            .Replace('\u00A0', ' ')
            .Replace("  ", " ");
    }
}