using OAS.Contracts.Accounting.Reports;

namespace OAS.Client.Accounting.Pages;

// Only direct account rows may be passed to these calculations. Header rows
// are presentation rollups, never additional financial movements.
public static class TrialBalancePresentation
{
    public sealed record Amounts(decimal OpeningDebit, decimal OpeningCredit,
        decimal PeriodDebit, decimal PeriodCredit, decimal ClosingDebit, decimal ClosingCredit,
        decimal? CumulativeDebit, decimal? CumulativeCredit)
    {
        public decimal? Value(string key) => key switch
        {
            "od" => OpeningDebit, "oc" => OpeningCredit,
            "pd" => PeriodDebit, "pc" => PeriodCredit,
            "cd" => ClosingDebit, "cc" => ClosingCredit,
            "gd" => CumulativeDebit, "gc" => CumulativeCredit, _ => null
        };
    }

    public static Amounts SumDirect(IEnumerable<TrialBalanceRowDto> source, bool netForSingleAccount = false)
    {
        var rows = source.ToArray();
        if (rows.Any(r => r.IsAggregate))
            throw new ArgumentException("Aggregated rows cannot be counted as direct movements.", nameof(source));
        var od = rows.Sum(r => r.OpeningDebit);
        var oc = rows.Sum(r => r.OpeningCredit);
        var pd = rows.Sum(r => r.PeriodDebit);
        var pc = rows.Sum(r => r.PeriodCredit);
        var cd = rows.Sum(r => r.ClosingDebit);
        var cc = rows.Sum(r => r.ClosingCredit);
        if (netForSingleAccount)
        {
            (od, oc) = (Math.Max(od - oc, 0m), Math.Max(oc - od, 0m));
            (cd, cc) = (Math.Max(cd - cc, 0m), Math.Max(cc - cd, 0m));
        }
        return new Amounts(od, oc, pd, pc, cd, cc,
            rows.Any(r => r.PreviousDebitTotal is null) ? null : rows.Sum(r => r.PreviousDebitTotal!.Value) + pd,
            rows.Any(r => r.PreviousCreditTotal is null) ? null : rows.Sum(r => r.PreviousCreditTotal!.Value) + pc);
    }
}
