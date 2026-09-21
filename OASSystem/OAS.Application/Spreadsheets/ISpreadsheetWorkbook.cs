namespace OAS.Application.Spreadsheets;
public sealed record SpreadsheetColumn(string Key, bool Required = false, string DataType = "text", string? DefaultValue = null, IReadOnlyList<string>? AllowedValues = null, string? Format = null, string? Header = null);
public sealed record SpreadsheetSheet(string Name, IReadOnlyList<SpreadsheetColumn> Columns, string? DisplayName = null);
public sealed record SpreadsheetRow(string Sheet, int Number, IReadOnlyDictionary<string,string> Values);
public sealed record SpreadsheetTable(SpreadsheetSheet Definition, IReadOnlyList<IReadOnlyDictionary<string,string>> Rows);
public interface ISpreadsheetWorkbook
{
    IReadOnlyList<SpreadsheetRow> Read(byte[] content, IReadOnlyList<SpreadsheetSheet> sheets);
    byte[] Write(IReadOnlyList<SpreadsheetTable> tables, bool template = false);
}
