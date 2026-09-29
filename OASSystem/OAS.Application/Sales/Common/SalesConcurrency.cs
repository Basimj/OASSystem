using OAS.Application.Common.Exceptions;

namespace OAS.Application.Sales.Common;

public static class SalesConcurrency
{
    public static void Ensure(string? requestedRowVersion, byte[] currentRowVersion, string resourceArabic)
    {
        if (string.IsNullOrWhiteSpace(requestedRowVersion))
            throw new ConcurrencyException($"بيانات التزامن الخاصة بـ{resourceArabic} مطلوبة.");

        byte[] requested;
        try
        {
            requested = Convert.FromBase64String(requestedRowVersion);
        }
        catch (FormatException ex)
        {
            throw new ConcurrencyException($"بيانات التزامن الخاصة بـ{resourceArabic} غير صالحة.", ex);
        }

        if (!currentRowVersion.SequenceEqual(requested))
            throw new ConcurrencyException($"تم تعديل {resourceArabic} بواسطة مستخدم آخر. حدّث البيانات ثم حاول مرة أخرى.");
    }
}
