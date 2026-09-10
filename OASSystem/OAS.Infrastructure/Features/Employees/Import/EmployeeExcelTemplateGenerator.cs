using ClosedXML.Excel;
using OAS.Application.Features.Employees.Abstractions;

namespace OAS.Infrastructure.Features.Employees.Import;

public sealed class EmployeeExcelTemplateGenerator : IEmployeeExcelTemplateGenerator
{
    private static readonly string[] Headers =
    [
        "الاسم الأول",
        "اسم العائلة",
        "رقم الهاتف",
        "البريد الإلكتروني",
        "المسمى الوظيفي",
        "تاريخ التوظيف",
        "الدولة",
        "المحافظة",
        "المدينة",
        "الرمز البريدي",
        "عنوان السكن",
        "مستحق للعمولة",
        "نشط"
    ];

    public byte[] Generate()
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("الموظفون");
        worksheet.RightToLeft = true;

        for (var column = 0; column < Headers.Length; column++)
        {
            var cell = worksheet.Cell(1, column + 1);
            cell.Value = Headers[column];
            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6F42C1");
            cell.Style.Font.FontColor = XLColor.White;
        }

        const int firstDataRow = 2;
        const int lastDataRow = 101;
        for (var row = firstDataRow; row <= lastDataRow; row++)
            worksheet.Cell(row, 6).Style.DateFormat.Format = "dd/MM/yyyy";

        for (var column = 12; column <= 13; column++)
        {
            var validation = worksheet.Range(firstDataRow, column, lastDataRow, column).CreateDataValidation();
            validation.IgnoreBlanks = true;
            validation.InCellDropdown = true;
            validation.List("\"نعم,لا\"");
        }

        var widths = new double[] { 20, 20, 18, 28, 24, 18, 18, 18, 18, 16, 34, 20, 14 };
        for (var i = 0; i < widths.Length; i++) worksheet.Column(i + 1).Width = widths[i];

        worksheet.SheetView.FreezeRows(1);
        worksheet.Range(1, 1, lastDataRow, Headers.Length).SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
