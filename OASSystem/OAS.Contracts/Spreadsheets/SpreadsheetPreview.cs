namespace OAS.Contracts.Spreadsheets;
public sealed record SpreadsheetRowResult(string Sheet, int RowNumber, IReadOnlyDictionary<string,string> Values, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings);
public sealed record SpreadsheetPreview(IReadOnlyList<SpreadsheetRowResult> Rows, int ImportedRecords = 0)
{
    public int ValidRows => Rows.Count(x => x.Errors.Count == 0);
    public int InvalidRows => Rows.Count(x => x.Errors.Count != 0);
    public int WarningRows => Rows.Count(x => x.Warnings.Count != 0);
    public bool CanImport => Rows.Count > 0 && InvalidRows == 0;
}
