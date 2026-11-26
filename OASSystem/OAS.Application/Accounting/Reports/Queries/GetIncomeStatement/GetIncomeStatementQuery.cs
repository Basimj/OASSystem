using MediatR;
using FluentValidation;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;
namespace OAS.Application.Accounting.Reports.Queries.GetIncomeStatement;
public sealed record GetIncomeStatementQuery(IncomeStatementFilterDto Filter) : IQuery<IncomeStatementReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [JournalPermissions.View];
}
public sealed class GetIncomeStatementQueryValidator : AbstractValidator<GetIncomeStatementQuery>
{
    public GetIncomeStatementQueryValidator() => RuleFor(x => x.Filter).Must(f => f.ToDate >= f.FromDate).WithErrorCode("report_date_range_invalid");
}
public sealed class GetIncomeStatementQueryHandler(IAccountingReportingQueryService reports) : IRequestHandler<GetIncomeStatementQuery, IncomeStatementReportDto>
{
    public Task<IncomeStatementReportDto> Handle(GetIncomeStatementQuery request, CancellationToken cancellationToken) => reports.GetIncomeStatementAsync(request.Filter, cancellationToken);
}
