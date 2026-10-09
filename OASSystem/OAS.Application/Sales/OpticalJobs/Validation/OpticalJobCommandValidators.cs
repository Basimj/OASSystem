using FluentValidation;
using OAS.Application.Sales.OpticalJobs.Commands;
using OAS.Contracts.Sales.Enums;

namespace OAS.Application.Sales.OpticalJobs.Validation;

public sealed class CreateOpticalJobCommandValidator : AbstractValidator<CreateOpticalJobCommand>
{
    public CreateOpticalJobCommandValidator()
    {
        RuleFor(x => x.Request.CustomerOrderId).NotEmpty();
        RuleFor(x => x.Request.Notes).MaximumLength(1000);
        RuleFor(x => x.Request.Lines).NotNull();
        RuleForEach(x => x.Request.Lines).ChildRules(line => line.RuleFor(x => x.CustomerOrderLineId).NotEmpty());
    }
}

public sealed class AssignOpticalJobCommandValidator : AbstractValidator<AssignOpticalJobCommand>
{
    public AssignOpticalJobCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); }
}
public sealed class IssueOpticalJobMaterialsCommandValidator : AbstractValidator<IssueOpticalJobMaterialsCommand>
{
    public IssueOpticalJobMaterialsCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); RuleFor(x => x.Request.WarehouseId).NotEmpty(); RuleFor(x => x.Request.RequestId).NotEmpty();
        RuleFor(x => x.Request.Lines).NotEmpty();
        RuleForEach(x => x.Request.Lines).ChildRules(l => { l.RuleFor(x => x.ProductVariantId).NotEmpty(); l.RuleFor(x => x.Quantity).GreaterThan(0); });
    }
}
public sealed class StartOpticalJobCommandValidator : AbstractValidator<StartOpticalJobCommand>
{ public StartOpticalJobCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); } }
public sealed class CompleteOpticalQualityControlCommandValidator : AbstractValidator<CompleteOpticalQualityControlCommand>
{
    public CompleteOpticalQualityControlCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.QualityCheckId).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); RuleFor(x => x.Request.QualityCheckRowVersion).NotEmpty(); RuleFor(x => x.Request.Items).NotEmpty();
        RuleForEach(x => x.Request.Items).ChildRules(i => { i.RuleFor(x => x.CheckCode).NotEmpty().MaximumLength(50); i.RuleFor(x => x.Result).IsInEnum(); i.RuleFor(x => x.Notes).MaximumLength(1000); });
        RuleFor(x => x.Request).Must(r => r.Items.All(i => i.Result != OpticalQualityCheckItemResult.NotChecked)).WithErrorCode("qc_incomplete");
        RuleFor(x => x.Request).Must(r => !r.Items.Any(i => i.Result == OpticalQualityCheckItemResult.Fail) || (r.FailureAction.HasValue && !string.IsNullOrWhiteSpace(r.Reason))).WithErrorCode("qc_failure_action_required");
        RuleFor(x => x.Request).Must(r => r.FailureAction != OpticalQcFailureAction.Remake || r.RemakeLineId.HasValue).WithErrorCode("qc_remake_line_required");
    }
}
public sealed class RecordOpticalJobBreakageCommandValidator : AbstractValidator<RecordOpticalJobBreakageCommand>
{
    public RecordOpticalJobBreakageCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); RuleFor(x => x.Request.OpticalJobLineId).NotEmpty(); RuleFor(x => x.Request.ProductVariantId).NotEmpty(); RuleFor(x => x.Request.WarehouseId).NotEmpty().When(x => x.Request.RequiresReplacement); RuleFor(x => x.Request.Quantity).GreaterThan(0); RuleFor(x => x.Request.ReasonCode).NotEmpty().MaximumLength(50); RuleFor(x => x.Request.ReasonText).MaximumLength(1000);
    }
}
public sealed class CreateOpticalJobRemakeCommandValidator : AbstractValidator<CreateOpticalJobRemakeCommand>
{
    public CreateOpticalJobRemakeCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); RuleFor(x => x.Request.OpticalJobLineId).NotEmpty(); RuleFor(x => x.Request.Quantity).GreaterThan(0); RuleFor(x => x.Request.Reason).NotEmpty().MaximumLength(1000); }
}
public sealed class MarkOpticalJobReadyCommandValidator : AbstractValidator<MarkOpticalJobReadyCommand>
{ public MarkOpticalJobReadyCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); } }

public sealed class SendOpticalJobToQualityControlCommandValidator : AbstractValidator<SendOpticalJobToQualityControlCommand>
{ public SendOpticalJobToQualityControlCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); } }
public sealed class PassOpticalJobQualityControlCommandValidator : AbstractValidator<PassOpticalJobQualityControlCommand>
{ public PassOpticalJobQualityControlCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); } }
public sealed class StartOpticalJobRemakeCommandValidator : AbstractValidator<StartOpticalJobRemakeCommand>
{ public StartOpticalJobRemakeCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.RemakeId).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); RuleFor(x => x.Request.RemakeRowVersion).NotEmpty(); } }
public sealed class SendOpticalJobRemakeToQcCommandValidator : AbstractValidator<SendOpticalJobRemakeToQcCommand>
{ public SendOpticalJobRemakeToQcCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.RemakeId).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); RuleFor(x => x.Request.RemakeRowVersion).NotEmpty(); } }
public sealed class DeliverOpticalJobCommandValidator : AbstractValidator<DeliverOpticalJobCommand>
{ public DeliverOpticalJobCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); } }
public sealed class CancelOpticalJobCommandValidator : AbstractValidator<CancelOpticalJobCommand>
{ public CancelOpticalJobCommandValidator() { RuleFor(x => x.Id).NotEmpty(); RuleFor(x => x.Request.RowVersion).NotEmpty(); } }
