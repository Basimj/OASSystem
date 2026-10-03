namespace OAS.Domain.Features.Employees.Enums;

public enum SalaryCalculationMethod : byte
{
    FixedAmount = 1,
    PercentageOfBasic = 2,
    Hourly = 3,
    Daily = 4,
    Manual = 5,
    ExternalSource = 6
}
