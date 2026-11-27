using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Reports;

namespace OAS.Application.Sales.Reports;

public sealed record GetSalesReportQuery(SalesReportFilterDto Filter)
    : IQuery<SalesReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}

public sealed class GetSalesReportQueryHandler(ISalesReportQueryService reports)
    : IRequestHandler<GetSalesReportQuery, SalesReportDto>
{
    public Task<SalesReportDto> Handle(GetSalesReportQuery query, CancellationToken cancellationToken) =>
        reports.GetAsync(query.Filter, cancellationToken);
}

public sealed record GetSalesReportOptionsQuery(SalesReportOptionRequest Request)
    : IQuery<SalesReportOptionPage>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.View];
}
public sealed class GetSalesReportOptionsQueryHandler(ISalesReportQueryService reports)
    : IRequestHandler<GetSalesReportOptionsQuery, SalesReportOptionPage>
{
    public Task<SalesReportOptionPage> Handle(GetSalesReportOptionsQuery query, CancellationToken cancellationToken) =>
        reports.GetOptionsAsync(query.Request, cancellationToken);
}

/// <summary>Historical currency scope for the interactive report. No currency conversion is performed.</summary>
public static class SalesReportCurrencyScope
{
    public const string Automatic = "*";
    public static string Normalize(string? code) => (code ?? "").Trim().ToUpperInvariant();

    public static string[] Available(IEnumerable<string?> codes) => codes.Select(Normalize)
        .Where(code => code.Length > 0).Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToArray();

    public static string Select(string requested, string configured, IReadOnlyList<string> available)
    {
        var choice = Normalize(requested);
        var current = Normalize(configured);
        if (choice != Automatic) return choice;
        return available.Contains(current, StringComparer.Ordinal) || available.Count == 0
            ? current : available[0];
    }
}

/// <summary>Shared arithmetic for SQL projections and the report/export client.</summary>
public static class SalesReportArithmetic
{
    public static SalesReportTotalsDto Compose(decimal invoiceRevenue, decimal returnRevenue,
        decimal discounts, decimal tax, decimal cogs, long invoiceCount, long returnCount)
    {
        var net = Round(invoiceRevenue - returnRevenue);
        var profit = Round(net - cogs);
        var margin = net <= 0m ? 0m : Round(100m * profit / net);
        return new(Round(invoiceRevenue), Round(returnRevenue), net, Round(discounts),
            Round(tax), Round(cogs), profit, margin, invoiceCount, returnCount);
    }

    // Assign the header rounding residual to one deterministic line before any report filter.
    public static decimal[] ReconcileHeader(IReadOnlyList<decimal> lines, decimal header)
    {
        var result = lines.ToArray();
        if (result.Length == 0) return result;
        // Adjust the largest monetary line first; avoid negative line values when
        // a negative residual exceeds the first candidate (legacy rounding).
        var delta = header - result.Sum();
        foreach (var index in Enumerable.Range(0, result.Length).OrderByDescending(i => result[i]).ThenBy(i => i))
        {
            if (delta >= 0m) { result[index] += delta; break; }
            var change = Math.Min(result[index], -delta);
            result[index] -= change; delta += change;
            if (delta == 0m) break;
        }
        return result;
    }

    private static decimal Round(decimal amount) => Math.Round(amount, 4, MidpointRounding.AwayFromZero);
}
