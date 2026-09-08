namespace OAS.Application.Features.Employees.Abstractions;

public interface IEmployeeExcelTemplateGenerator
{
    byte[] Generate();
}