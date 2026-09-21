namespace OAS.UiLib.Components.Spreadsheets;
public sealed record UiSpreadsheetRow(string Sheet,int RowNumber,IReadOnlyDictionary<string,string> Values,IReadOnlyList<string> Errors,IReadOnlyList<string> Warnings);
public sealed record UiSpreadsheetPreview(IReadOnlyList<UiSpreadsheetRow> Rows,int ValidRows,int InvalidRows,int WarningRows,bool CanImport,int ImportedRecords);
