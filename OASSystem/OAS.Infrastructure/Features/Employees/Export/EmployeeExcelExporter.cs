using System.Globalization;
using OAS.Application.Features.Employees.Abstractions;
using OAS.Application.Spreadsheets;
namespace OAS.Infrastructure.Features.Employees.Export;
public sealed class EmployeeExcelExporter(ISpreadsheetWorkbook workbook) : IEmployeeExcelExporter
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
        var definition=new SpreadsheetSheet("الموظفون",Headers.Select((x,index)=>new SpreadsheetColumn(x,DataType:index==6?"date":"text",Format:index==6?"dd/mm/yyyy":null)).ToArray());
        var rows=employees.Select(employee=>
        {
            string[] values=[employee.EmployeeCode,employee.FirstName,employee.LastName,employee.PhoneNumber??"",employee.Email??"",employee.JobTitle,employee.HireDate?.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)??"",employee.Country??"",employee.Governorate??"",employee.City??"",employee.PostalCode??"",employee.ResidentialAddress??"",employee.IsCommissionEligible?"نعم":"لا",employee.IsActive?"نعم":"لا"];
            return (IReadOnlyDictionary<string,string>)Headers.Select((header,index)=>(header,value:values[index])).ToDictionary(x=>x.header,x=>x.value);
        }).ToArray();
        return workbook.Write([new(definition,rows)]);
    }
}
