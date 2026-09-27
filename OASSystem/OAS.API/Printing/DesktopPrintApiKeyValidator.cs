using System.Security.Cryptography;
using System.Text;

namespace OAS.API.Printing;

public sealed class DesktopPrintApiKeyValidator(IConfiguration configuration)
{
    public bool IsAuthorized(HttpRequest request)
    {
        var configured = configuration["Printing:DesktopApiKey"];

        // للتطوير المحلي يمكن ترك المفتاح فارغاً. في الإنتاج يجب ضبطه
        // وإدخال نفس القيمة في إعدادات OAS.Print.exe.
        if (string.IsNullOrWhiteSpace(configured))
            return true;

        if (!request.Headers.TryGetValue("X-OAS-Print-Key", out var suppliedValues))
            return false;

        var supplied = suppliedValues.ToString();
        if (string.IsNullOrEmpty(supplied))
            return false;

        var expectedBytes = Encoding.UTF8.GetBytes(configured);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);

        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
