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

/// <summary>Shared arithmetic for SQL projections and the report/export client.</summary>
public static class SalesReportArithmetic
{
    public static SalesReportTotalsDto Compose(decimal invoiceRevenue, decimal returnRevenue,
        decimal discounts, decimal tax, decimal cogs, long invoiceCount, long returnCount)
    {
        var net = Round(invoiceRevenue - returnRevenue);
        var profit = Round(net - cogs);
        var margin = net == 0m ? 0m : Round(100m * profit / net);
        return new(Round(invoiceRevenue), Round(returnRevenue), net, Round(discounts),
            Round(tax), Round(cogs), profit, margin, invoiceCount, returnCount);
    }

    private static decimal Round(decimal amount) => Math.Round(amount, 4, MidpointRounding.AwayFromZero);
}
