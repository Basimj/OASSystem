using OAS.Domain.Exceptions;

namespace OAS.Domain.Common.Rules;

public static class BusinessRuleGuard
{
    public static void Check(IBusinessRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);
        if (rule.IsBroken()) throw new DomainException(rule.Message);
    }
}
