using System.Globalization;

namespace OAS.Domain.Features.Employees;

public static class EmployeeCodeFormatter
{
    public static string Format(
        long sequenceNumber,
        string prefix = "EMP-",
        int minimumDigits = 5)
    {
        if (sequenceNumber <= 0)
            return string.Empty;

        var digits = sequenceNumber.ToString(
            $"D{minimumDigits}",
            CultureInfo.InvariantCulture);

        return $"{prefix.Trim()}{digits}";
    }
}