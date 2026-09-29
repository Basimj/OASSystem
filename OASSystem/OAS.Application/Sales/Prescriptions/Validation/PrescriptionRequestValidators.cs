using FluentValidation;
using OAS.Contracts.Sales.Prescriptions;

namespace OAS.Application.Sales.Prescriptions.Validation;

public sealed class CreatePrescriptionRequestValidator : AbstractValidator<CreatePrescriptionRequest>
{
    public CreatePrescriptionRequestValidator()
    {
        RuleFor(x => x.CustomerId).NotEmpty();
        RuleFor(x => x.PrescriptionCode).MaximumLength(40);
        RuleFor(x => x.PrescribedBy).MaximumLength(150);
        RuleFor(x => x.ClinicName).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class UpdatePrescriptionRequestValidator : AbstractValidator<UpdatePrescriptionRequest>
{
    public UpdatePrescriptionRequestValidator()
    {
        RuleFor(x => x.RowVersion).NotEmpty();
        RuleFor(x => x.PrescribedBy).MaximumLength(150);
        RuleFor(x => x.ClinicName).MaximumLength(150);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}

public sealed class CreatePrescriptionRevisionRequestValidator : AbstractValidator<CreatePrescriptionRevisionRequest>
{
    public CreatePrescriptionRevisionRequestValidator()
    {
        RuleFor(x => x.PrescriptionRowVersion).NotEmpty();
        RuleFor(x => x.Reason).MaximumLength(500);
        RuleFor(x => x.EyeDetails).NotEmpty().Must(x => x.Select(e => e.Eye).Distinct().Count() == x.Count).WithMessage("Eye must be unique per revision.");
        RuleForEach(x => x.EyeDetails).SetValidator(new PrescriptionEyeDetailRequestValidator());
    }
}

public sealed class PrescriptionEyeDetailRequestValidator : AbstractValidator<PrescriptionEyeDetailRequest>
{
    public PrescriptionEyeDetailRequestValidator()
    {
        RuleFor(x => x.Axis).InclusiveBetween((short)0, (short)180).When(x => x.Axis.HasValue);
        RuleFor(x => x.ADD).GreaterThanOrEqualTo(0).When(x => x.ADD.HasValue);
        RuleFor(x => x.Prism).GreaterThanOrEqualTo(0).When(x => x.Prism.HasValue);
        RuleFor(x => x.PD).GreaterThan(0).When(x => x.PD.HasValue);
        RuleFor(x => x.MonocularPD).GreaterThan(0).When(x => x.MonocularPD.HasValue);
        RuleFor(x => x.FittingHeight).GreaterThan(0).When(x => x.FittingHeight.HasValue);
        RuleFor(x => x.VA).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
