using ClosedXML.Excel;
namespace OAS.Infrastructure.Spreadsheets;
/// <summary>Shared OAS workbook presentation, extracted from Employees.</summary>
public static class SpreadsheetWorkbookStyle
{
    public static void ApplyHeader(IXLWorksheet sheet,int columns)
    {
        sheet.RightToLeft=true;
        var header=sheet.Range(1,1,1,columns);
        header.Style.Font.Bold=true;
        header.Style.Font.FontColor=XLColor.White;
        header.Style.Fill.BackgroundColor=XLColor.FromHtml("#6F42C1");
        header.Style.Alignment.Horizontal=XLAlignmentHorizontalValues.Center;
        sheet.SheetView.FreezeRows(1);
    }
}
