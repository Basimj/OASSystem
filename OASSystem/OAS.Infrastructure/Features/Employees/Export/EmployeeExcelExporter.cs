using ClosedXML.Excel;
using OAS.Application.Features.Employees.Abstractions;

namespace OAS.Infrastructure.Features.Employees.Export;

public sealed class EmployeeExcelExporter : IEmployeeExcelExporter
{
    private static readonly string[] Headers =
    [
        "كود الموظف",
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

    public byte[] Export(IReadOnlyCollection<EmployeeExportRow> employees)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("الموظفون");
        worksheet.RightToLeft = true;

        for (var column = 0; column < Headers.Length; column++)
            worksheet.Cell(1, column + 1).Value = Headers[column];

        var header = worksheet.Range(1, 1, 1, Headers.Length);
        header.Style.Font.Bold = true;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#6F42C1");

        var row = 2;
        foreach (var employee in employees)
        {
            worksheet.Cell(row, 1).Value = employee.EmployeeCode;
            worksheet.Cell(row, 2).Value = employee.FirstName;
            worksheet.Cell(row, 3).Value = employee.LastName;
            worksheet.Cell(row, 4).Value = employee.PhoneNumber ?? string.Empty;
            worksheet.Cell(row, 5).Value = employee.Email ?? string.Empty;
            worksheet.Cell(row, 6).Value = employee.JobTitle;
            if (employee.HireDate.HasValue)
            {
                worksheet.Cell(row, 7).Value = employee.HireDate.Value;
                worksheet.Cell(row, 7).Style.DateFormat.Format = "dd/MM/yyyy";
            }
            worksheet.Cell(row, 8).Value = employee.Country ?? string.Empty;
            worksheet.Cell(row, 9).Value = employee.Governorate ?? string.Empty;
            worksheet.Cell(row, 10).Value = employee.City ?? string.Empty;
            worksheet.Cell(row, 11).Value = employee.PostalCode ?? string.Empty;
            worksheet.Cell(row, 12).Value = employee.ResidentialAddress ?? string.Empty;
            worksheet.Cell(row, 13).Value = employee.IsCommissionEligible ? "نعم" : "لا";
            worksheet.Cell(row, 14).Value = employee.IsActive ? "نعم" : "لا";
            row++;
        }

        worksheet.Columns(1, Headers.Length).AdjustToContents();
        worksheet.SheetView.FreezeRows(1);
        worksheet.Range(1, 1, Math.Max(row - 1, 1), Headers.Length).SetAutoFilter();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
