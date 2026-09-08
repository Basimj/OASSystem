using ClosedXML.Excel;
using OAS.Application.Features.Employees.Abstractions;

namespace OAS.Infrastructure.Features.Employees.Export;

public sealed class EmployeeExcelExporter
    : IEmployeeExcelExporter
{
    private static readonly string[] Headers =
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

    public byte[] Export(
        IReadOnlyCollection<EmployeeExportRow> employees)
    {
        using var workbook = new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("الموظفون");

        worksheet.RightToLeft = true;

        // العناوين
        for (var column = 0;
             column < Headers.Length;
             column++)
        {
            worksheet.Cell(1, column + 1).Value =
                Headers[column];
        }

        var headerRange =
            worksheet.Range(
                1,
                1,
                1,
                Headers.Length);

        headerRange.Style.Font.Bold = true;

        headerRange.Style.Font.FontColor =
            XLColor.White;

        headerRange.Style.Fill.BackgroundColor =
            XLColor.FromHtml("#6F42C1");

        headerRange.Style.Alignment.Horizontal =
            XLAlignmentHorizontalValues.Center;

        headerRange.Style.Alignment.Vertical =
            XLAlignmentVerticalValues.Center;

        headerRange.Style.Alignment.WrapText = true;

        worksheet.Row(1).Height = 30;

        // البيانات
        var row = 2;

        foreach (var employee in employees)
        {
            worksheet.Cell(row, 1).Value =
                employee.EmployeeCode;

            worksheet.Cell(row, 2).Value =
                employee.FirstName;

            worksheet.Cell(row, 3).Value =
                employee.LastName;

            worksheet.Cell(row, 4).Value =
                employee.PhoneNumber ?? string.Empty;

            worksheet.Cell(row, 5).Value =
                employee.JobTitle ?? string.Empty;

            if (employee.HireDate.HasValue)
            {
                worksheet.Cell(row, 6).Value =
                    employee.HireDate.Value;

                worksheet.Cell(row, 6)
                    .Style.DateFormat.Format =
                    "dd/MM/yyyy";
            }

            worksheet.Cell(row, 7).Value =
                employee.IsSalesEmployee
                    ? "نعم"
                    : "لا";

            worksheet.Cell(row, 8).Value =
                employee.IsTechnician
                    ? "نعم"
                    : "لا";

            worksheet.Cell(row, 9).Value =
                employee.IsCommissionEligible
                    ? "نعم"
                    : "لا";

            worksheet.Cell(row, 10).Value =
                employee.IsActive
                    ? "نعم"
                    : "لا";

            worksheet.Cell(row, 11).Value =
                employee.Notes ?? string.Empty;

            row++;
        }

        // التنسيق
        worksheet.Column(1).Width = 18;
        worksheet.Column(2).Width = 18;
        worksheet.Column(3).Width = 18;
        worksheet.Column(4).Width = 18;
        worksheet.Column(5).Width = 24;
        worksheet.Column(6).Width = 18;
        worksheet.Column(7).Width = 18;
        worksheet.Column(8).Width = 14;
        worksheet.Column(9).Width = 20;
        worksheet.Column(10).Width = 14;
        worksheet.Column(11).Width = 30;

        if (row > 2)
        {
            worksheet.Range(
                    2,
                    1,
                    row - 1,
                    Headers.Length)
                .Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;
        }

        worksheet.SheetView.FreezeRows(1);

        worksheet.Range(
                1,
                1,
                Math.Max(row - 1, 1),
                Headers.Length)
            .SetAutoFilter();

        using var stream = new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}