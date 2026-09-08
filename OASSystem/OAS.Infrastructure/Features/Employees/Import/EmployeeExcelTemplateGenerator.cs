using ClosedXML.Excel;
using OAS.Application.Features.Employees.Abstractions;

namespace OAS.Infrastructure.Features.Employees.Import;

public sealed class EmployeeExcelTemplateGenerator
    : IEmployeeExcelTemplateGenerator
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

    public byte[] Generate()
    {
        using var workbook = new XLWorkbook();

        var worksheet =
            workbook.Worksheets.Add("الموظفون");

        worksheet.RightToLeft = true;

        /*
         * ============================================
         * Header
         * ============================================
         */

        for (var column = 0; column < Headers.Length; column++)
        {
            var cell =
                worksheet.Cell(1, column + 1);

            cell.Value = Headers[column];

            cell.Style.Font.Bold = true;
            cell.Style.Alignment.Horizontal =
                XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical =
                XLAlignmentVerticalValues.Center;

            cell.Style.Fill.BackgroundColor =
                XLColor.FromHtml("#6F42C1");

            cell.Style.Font.FontColor =
                XLColor.White;

            cell.Style.Border.BottomBorder =
                XLBorderStyleValues.Thin;
        }

        worksheet.Row(1).Height = 30;

        /*
         * ============================================
         * Empty input rows
         * ============================================
         */

        const int firstDataRow = 2;
        const int lastDataRow = 101;

        for (var row = firstDataRow;
             row <= lastDataRow;
             row++)
        {
            /*
             * تاريخ التوظيف
             *
             * نجعل الخلايا بتنسيق تاريخ Excel
             * مع بقائها فارغة.
             */
            worksheet.Cell(row, 6)
                .Style.DateFormat.Format = "dd/MM/yyyy";

            /*
             * المحاذاة
             */

            for (var column = 1;
                 column <= Headers.Length;
                 column++)
            {
                worksheet.Cell(row, column)
                    .Style.Alignment.Vertical =
                    XLAlignmentVerticalValues.Center;
            }
        }

        /*
         * ============================================
         * نعم / لا validation
         * ============================================
         */

        for (var column = 7; column <= 10; column++)
        {
            var range =
                worksheet.Range(
                    firstDataRow,
                    column,
                    lastDataRow,
                    column);

            var validation =
                range.CreateDataValidation();

            validation.IgnoreBlanks = true;
            validation.InCellDropdown = true;

            validation.List(
                "\"نعم,لا\"");

            validation.ErrorTitle =
                "قيمة غير صحيحة";

            validation.ErrorMessage =
                "يرجى اختيار نعم أو لا.";

            validation.ShowErrorMessage = true;
        }

        /*
         * ============================================
         * Column widths
         * ============================================
         */

        worksheet.Column(1).Width = 18; // رمز الموظف
        worksheet.Column(2).Width = 20; // الاسم الأول
        worksheet.Column(3).Width = 20; // اسم العائلة
        worksheet.Column(4).Width = 18; // الهاتف
        worksheet.Column(5).Width = 24; // المسمى
        worksheet.Column(6).Width = 18; // التاريخ
        worksheet.Column(7).Width = 18; // مبيعات
        worksheet.Column(8).Width = 14; // فني
        worksheet.Column(9).Width = 20; // عمولة
        worksheet.Column(10).Width = 14; // نشط
        worksheet.Column(11).Width = 35; // ملاحظات

        /*
         * ============================================
         * Table usability
         * ============================================
         */

        worksheet.SheetView.FreezeRows(1);

        worksheet.Range(
            1,
            1,
            lastDataRow,
            Headers.Length)
            .SetAutoFilter();

        /*
         * ============================================
         * Export
         * ============================================
         */

        using var stream =
            new MemoryStream();

        workbook.SaveAs(stream);

        return stream.ToArray();
    }
}