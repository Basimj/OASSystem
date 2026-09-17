using FluentValidation;

namespace OAS.Application.Accounting.CostCenters.Commands.UpdateCostCenter;

public sealed class UpdateCostCenterCommandValidator
    : AbstractValidator<UpdateCostCenterCommand>
{
    public UpdateCostCenterCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode("cost_center_id_required");

        RuleFor(x => x.Data.Code)
            .NotEmpty()
            .WithErrorCode("cost_center_code_required")
            .MaximumLength(30)
            .WithErrorCode("cost_center_code_max_length");

        RuleFor(x => x.Data.NameAr)
            .NotEmpty()
            .WithErrorCode("cost_center_name_ar_required")
            .MaximumLength(150)
            .WithErrorCode("cost_center_name_ar_max_length");

        RuleFor(x => x.Data.NameEn)
            .MaximumLength(150)
            .WithErrorCode("cost_center_name_en_max_length");

        RuleFor(x => x.Data.RowVersion)
            .NotEmpty()
            .WithErrorCode("row_version_required");
    }
}
