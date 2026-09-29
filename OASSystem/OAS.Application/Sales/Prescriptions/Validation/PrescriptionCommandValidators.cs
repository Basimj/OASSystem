using FluentValidation;
using OAS.Application.Sales.Prescriptions.Commands;

namespace OAS.Application.Sales.Prescriptions.Validation;

public sealed class CreatePrescriptionCommandValidator : AbstractValidator<CreatePrescriptionCommand>
{
    public CreatePrescriptionCommandValidator(CreatePrescriptionRequestValidator validator) =>
        RuleFor(x => x.Data).SetValidator(validator);
}

public sealed class UpdatePrescriptionCommandValidator : AbstractValidator<UpdatePrescriptionCommand>
{
    public UpdatePrescriptionCommandValidator(UpdatePrescriptionRequestValidator validator) =>
        RuleFor(x => x.Data).SetValidator(validator);
}

public sealed class CreatePrescriptionRevisionCommandValidator : AbstractValidator<CreatePrescriptionRevisionCommand>
{
    public CreatePrescriptionRevisionCommandValidator(CreatePrescriptionRevisionRequestValidator validator)
    {
        RuleFor(x => x.PrescriptionId).NotEmpty().WithErrorCode("sales_prescription_id_required");
        RuleFor(x => x.Request).SetValidator(validator);
    }
}

public sealed class SetPrescriptionStatusCommandValidator : AbstractValidator<SetPrescriptionStatusCommand>
{
    public SetPrescriptionStatusCommandValidator()
    {
        RuleFor(x => x.PrescriptionId).NotEmpty().WithErrorCode("sales_prescription_id_required");
        RuleFor(x => x.Request.RowVersion).NotEmpty().WithErrorCode("row_version_required");
        RuleFor(x => x.Request.Status).IsInEnum().WithErrorCode("sales_prescription_status_invalid");
    }
}
