using FluentValidation;

namespace OAS.Application.Accounting.Reports.Queries.GetTrialBalance;

public sealed class GetTrialBalanceQueryValidator : AbstractValidator<GetTrialBalanceQuery>
{
    public GetTrialBalanceQueryValidator()
    {
        RuleFor(x => x)
            .Must(x => x.AsOfDate.HasValue
                ? x.AsOfDate.Value >= x.FromDate
                : x.ToDate >= x.FromDate)
            .WithErrorCode("report_date_range_invalid");

        RuleFor(x => x.AccountLevel)
            .Must(value => !value.HasValue || value.Value > 0)
            .WithErrorCode("trial_balance_account_level_invalid");
    }
}
