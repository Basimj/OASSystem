using FluentValidation;

namespace OAS.Application.Accounting.CostCenters.Commands.SetCostCenterStatus;

public sealed class SetCostCenterStatusCommandValidator
    : AbstractValidator<SetCostCenterStatusCommand>
{
    public SetCostCenterStatusCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("cost_center_id_required");

        RuleFor(x => x.Request.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
