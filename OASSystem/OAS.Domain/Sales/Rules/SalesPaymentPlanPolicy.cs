using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;

namespace OAS.Domain.Sales.Rules;

public static class SalesPaymentPlanPolicy
{
    public static SalesPaymentTermType ToPaymentTermType(SalesPaymentPlan paymentPlan)
    {
        if (!Enum.IsDefined(paymentPlan))
            throw new DomainException("Sales payment plan is invalid.");

        return paymentPlan == SalesPaymentPlan.AccountCredit
            ? SalesPaymentTermType.Credit
            : SalesPaymentTermType.Immediate;
    }

    public static SalesPaymentPlan FromLegacyPaymentTerm(SalesPaymentTermType paymentTermType)
    {
        if (!Enum.IsDefined(paymentTermType))
            throw new DomainException("Sales payment term type is invalid.");

        return paymentTermType == SalesPaymentTermType.Credit
            ? SalesPaymentPlan.AccountCredit
            : SalesPaymentPlan.FullNow;
    }

    public static int NormalizePaymentTermDays(SalesPaymentPlan paymentPlan, int paymentTermDays)
    {
        if (paymentTermDays < 0)
            throw new DomainException("Payment term days cannot be negative.");

        return ToPaymentTermType(paymentPlan) == SalesPaymentTermType.Credit
            ? paymentTermDays
            : 0;
    }

    public static void EnsureConsistent(
        SalesPaymentPlan paymentPlan,
        SalesPaymentTermType paymentTermType,
        int paymentTermDays)
    {
        var expectedTerm = ToPaymentTermType(paymentPlan);
        if (paymentTermType != expectedTerm)
            throw new DomainException($"Payment plan {paymentPlan} requires payment term {expectedTerm}.");

        if (paymentTermDays < 0)
            throw new DomainException("Payment term days cannot be negative.");

        if (expectedTerm == SalesPaymentTermType.Immediate && paymentTermDays != 0)
            throw new DomainException("Immediate payment plans cannot have payment term days.");
    }
}
