using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Reports.Services;

public static class AccountingReportMath
{
    public static decimal NaturalDelta(NormalBalance normalBalance, decimal debit, decimal credit) =>
        normalBalance == NormalBalance.Credit ? credit - debit : debit - credit;

    public static (decimal Debit, decimal Credit) SplitNet(decimal net) =>
        net >= 0m ? (net, 0m) : (0m, -net);

    public static bool IsBalanced(decimal debit, decimal credit) => debit == credit;
}
