namespace OAS.Application.Accounting.Reports.Services;

/// <summary>Pure base-currency accounting math. Never mutate posted documents.</summary>
public static class CustomerArMath
{
    public static decimal Round(decimal amount) =>
        Math.Round(amount, 4, MidpointRounding.AwayFromZero);

    public static decimal OpenInvoice(decimal originalBase, decimal appliedPaymentsBase,
        decimal postedReturnsBase, decimal otherCreditApplicationsBase)
    {
        if (originalBase < 0m || appliedPaymentsBase < 0m || postedReturnsBase < 0m || otherCreditApplicationsBase < 0m)
            throw new ArgumentOutOfRangeException(nameof(originalBase));
        return Round(Math.Max(0m, originalBase - appliedPaymentsBase -
            postedReturnsBase - otherCreditApplicationsBase));
    }

    public static string Bucket(DateOnly? due, DateOnly asOf)
    {
        if (!due.HasValue) return "Unknown";
        var days = asOf.DayNumber - due.Value.DayNumber;
        return days < 0 ? "NotDue" : days <= 30 ? "0-30" :
               days <= 60 ? "31-60" : days <= 90 ? "61-90" : "+90";
    }

    public static decimal ReturnCredit(decimal postedReturnBase, decimal unpaidOriginalBeforeReturnBase,
        decimal usedSourceBase)
    {
        if (postedReturnBase < 0m || unpaidOriginalBeforeReturnBase < 0m || usedSourceBase < 0m)
            throw new ArgumentOutOfRangeException(nameof(postedReturnBase));
        return Round(Math.Max(0m, postedReturnBase - unpaidOriginalBeforeReturnBase - usedSourceBase));
    }
}
