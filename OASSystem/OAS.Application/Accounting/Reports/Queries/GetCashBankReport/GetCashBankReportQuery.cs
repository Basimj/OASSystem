using MediatR;
using FluentValidation;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Authorization;
using OAS.Application.Accounting.Journals.Authorization;
using OAS.Contracts.Accounting.Reports;
namespace OAS.Application.Accounting.Reports.Queries.GetCashBankReport;
public sealed record GetCashBankReportQuery(CashBankReportFilterDto Filter) : IQuery<CashBankReportDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions => Filter.Mode is "shifts" or "differences"
        ? [JournalPermissions.View, AccountingPermissions.CashShifts.View] : [JournalPermissions.View];
}
public sealed class GetCashBankReportQueryValidator : AbstractValidator<GetCashBankReportQuery>
{
    public GetCashBankReportQueryValidator()
    {
        RuleFor(x => x.Filter.FromDate).NotEmpty();
        RuleFor(x => x.Filter.ToDate).NotEmpty().Must((q, d) => d >= q.Filter.FromDate && d < DateOnly.MaxValue).WithMessage("الفترة غير صالحة.");
        RuleFor(x => x.Filter.Mode).Must(x => x is "balances" or "cash" or "bank" or "currency" or "transfers" or "shifts" or "differences");
        RuleFor(x => x.Filter.Kind).Must(x => x is "all" or "cash" or "bank");
        RuleFor(x => x.Filter.MovementType).Must(x => x is null or 1 or 2 or 3).WithMessage("نوع حركة الصندوق غير صالح.");
        RuleFor(x => x.Filter.Activity).Must(x => x is "all" or "active" or "inactive");
        RuleFor(x => x.Filter.ShiftState).Must(x => x is "all" or "open" or "closing" or "closed" or "approved");
        RuleFor(x => x.Filter.SettlementState).Must(x => x is "all" or "settled" or "unsettled");
    }
}
public sealed class GetCashBankReportQueryHandler(ICashBankReportQueryService service) : IRequestHandler<GetCashBankReportQuery, CashBankReportDto>
{
    public Task<CashBankReportDto> Handle(GetCashBankReportQuery request, CancellationToken ct) => service.GetAsync(request.Filter, ct);
}
