using System.Globalization;

namespace OAS.Domain.Features.Employees;

public static class EmployeeCodeFormatter
{
    public static string Format(int employeeNumber, string? prefix = null, int minimumDigits = 0)
    {
        if (employeeNumber <= 0) return string.Empty;
        var digits = minimumDigits > 0
            ? employeeNumber.ToString($"D{minimumDigits}", CultureInfo.InvariantCulture)
            : employeeNumber.ToString(CultureInfo.InvariantCulture);
        return string.IsNullOrWhiteSpace(prefix) ? digits : $"{prefix.Trim()}{digits}";
    }
}
