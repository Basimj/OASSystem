using ClosedXML.Excel;
using System.Globalization;
using System.IO.Compression;
using OAS.Application.Spreadsheets;
using OAS.Application.Common.Exceptions;
namespace OAS.Infrastructure.Spreadsheets;
public sealed class ClosedXmlSpreadsheetWorkbook : ISpreadsheetWorkbook
{
    private static RequestValidationException Invalid(string message) => new(new Dictionary<string, string[]> { ["file"] = [message] });
    public IReadOnlyList<SpreadsheetRow> Read(byte[] content, IReadOnlyList<SpreadsheetSheet> sheets)
    {
        if (content.Length == 0 || content.Length > 10 * 1024 * 1024) throw Invalid("حجم الملف غير صالح (الحد 10 MB).");
        try
        {
            using var input = new MemoryStream(content);
            using (var zip = new ZipArchive(input, ZipArchiveMode.Read, true))
                if (zip.Entries.Sum(x => x.Length) > 100 * 1024 * 1024) throw Invalid("حجم محتوى الملف أكبر من الحد المسموح.");
            input.Position = 0;
            using var book = new XLWorkbook(input);
            var result = new List<SpreadsheetRow>();
            foreach (var definition in sheets)
            {
                var worksheetName = definition.DisplayName ?? definition.Name;
                if (!book.TryGetWorksheet(worksheetName, out var sheet) && !book.TryGetWorksheet(definition.Name, out sheet))
                    throw Invalid($"ورقة «{worksheetName}» مطلوبة.");
                var headers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var cell in sheet.Row(1).CellsUsed())
                {
                    var header = cell.GetString().Trim();
                    if (!headers.TryAdd(header, cell.Address.ColumnNumber)) throw Invalid($"عمود مكرر: {header}");
                }
                var columnIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var column in definition.Columns)
                {
                    if (headers.TryGetValue(column.Header ?? column.Key, out var index) || headers.TryGetValue(column.Key, out index))
                        columnIndexes[column.Key] = index;
                    else
                        throw Invalid($"العمود «{column.Header ?? column.Key}» مفقود في ورقة «{worksheetName}».");
                }
                foreach (var row in sheet.RowsUsed().Skip(1))
                {
                    if (result.Count >= 10000) throw Invalid("الحد الأقصى 10000 صف في الملف.");
                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var column in definition.Columns)
                    {
                        var cell = row.Cell(columnIndexes[column.Key]);
                        if (cell.HasFormula) throw Invalid($"الصيغ غير مسموحة: ورقة «{worksheetName}»، الصف {row.RowNumber()}، العمود «{column.Header ?? column.Key}».");
                        values[column.Key] = cell.DataType == XLDataType.DateTime
                            ? cell.GetDateTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                            : cell.DataType == XLDataType.Number ? cell.GetDouble().ToString("G15", CultureInfo.InvariantCulture) : cell.GetString().Trim();
                    }
                    if (values.Values.Any(x => x.Length > 0)) result.Add(new(definition.Name, row.RowNumber(), values));
                }
            }
            if (result.Count == 0) throw Invalid("الملف لا يحتوي على بيانات.");
            return result;
        }
        catch (RequestValidationException) { throw; }
        catch (Exception ex) when (ex is InvalidDataException or IOException or ArgumentException or FormatException or InvalidOperationException or System.Xml.XmlException)
        { throw Invalid("تعذر قراءة ملف Excel. استخدم القالب بصيغة xlsx."); }
    }
    public byte[] Write(IReadOnlyList<SpreadsheetTable> tables, bool template = false)
    {
        using var book = new XLWorkbook();
        foreach (var table in tables)
        {
            var sheet = book.Worksheets.Add(table.Definition.DisplayName ?? table.Definition.Name);
            for (var c = 0; c < table.Definition.Columns.Count; c++)
            {
                var column = table.Definition.Columns[c];
                sheet.Cell(1, c + 1).Value = column.Header ?? column.Key;
                sheet.Column(c + 1).Width = 23;
                // Codes and account numbers retain leading zeros; strings never become formulas.
                sheet.Column(c + 1).Style.NumberFormat.Format = column.DataType == "decimal" ? "0.####" : column.DataType == "date" ? column.Format ?? "yyyy-mm-dd" : "@";
                if (template && column.AllowedValues is { Count: > 0 })
                    sheet.Range(2, c + 1, 10001, c + 1).CreateDataValidation().List("\"" + string.Join(",", column.AllowedValues) + "\"", true);
                for (var i = 0; i < table.Rows.Count; i++)
                {
                    var value = table.Rows[i].GetValueOrDefault(column.Key) ?? "";
                    var cell = sheet.Cell(i + 2, c + 1);

                    if (column.DataType == "decimal" &&
                        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
                    {
                        cell.Value = amount;
                    }
                    else if (column.DataType == "date" &&
                             DateOnly.TryParseExact(
                                 value,
                                 "yyyy-MM-dd",
                                 CultureInfo.InvariantCulture,
                                 DateTimeStyles.None,
                                 out var date))
                    {
                        // ClosedXML ultimately uses OLE Automation dates. Very old/default
                        // DateOnly values (especially 0001-01-01) cannot be written as an
                        // Excel DateTime and throw "Not a legal OleAut date".
                        if (date == DateOnly.MinValue)
                        {
                            // Treat the CLR default date as "not supplied".
                            cell.Value = string.Empty;
                        }
                        else if (date < new DateOnly(1900, 1, 1))
                        {
                            // Preserve a genuine historical date without asking ClosedXML
                            // to convert it to an Excel/OLE date serial.
                            cell.Style.NumberFormat.Format = "@";
                            cell.Value = value;
                        }
                        else
                        {
                            cell.Value = date.ToDateTime(TimeOnly.MinValue);
                        }
                    }
                    else
                    {
                        cell.Value = value;
                    }
                }
            }
            SpreadsheetWorkbookStyle.ApplyHeader(sheet, table.Definition.Columns.Count);
            sheet.Range(1, 1, Math.Max(1, table.Rows.Count + 1), table.Definition.Columns.Count).SetAutoFilter();
        }
        if (template)
        {
            var help = book.Worksheets.Add("التعليمات");
            help.RightToLeft = true;
            help.Cell(1, 1).Value = "أدخل البيانات بدءاً من الصف الثاني. لا تغيّر أسماء الأوراق أو الأعمدة. استخدم الأكواد وليس GUID. التواريخ yyyy-MM-dd. إنشاء جديد فقط.";
            help.Cell(2, 1).Value = "تظهر المعاينة قبل التأكيد. أي صف غير صالح يمنع استيراد الملف كله. القيود تنشأ مسودات فقط. JournalKey يجمع أسطر القيد.";
            var row = 4;
            foreach (var table in tables)
                foreach (var column in table.Definition.Columns)
                { help.Cell(row, 1).Value = table.Definition.DisplayName ?? table.Definition.Name; help.Cell(row, 2).Value = column.Header ?? column.Key; help.Cell(row, 3).Value = column.Required ? "مطلوب" : "اختياري"; help.Cell(row, 4).Value = column.DataType; help.Cell(row++, 5).Value = column.DefaultValue ?? ""; }
            help.Columns().AdjustToContents(10, 70);
        }
        using var output = new MemoryStream(); book.SaveAs(output); return output.ToArray();
    }
}
