using FluentValidation;
using OAS.Application.Sales.OpticalJobs.Commands;

namespace OAS.Application.Sales.OpticalJobs.Validation;

public sealed class CreateOpticalJobCommandValidator : AbstractValidator<CreateOpticalJobCommand>
{
    public CreateOpticalJobCommandValidator()
    {
        RuleFor(x => x.Request.CustomerOrderId).NotEmpty();
        RuleFor(x => x.Request.Notes).MaximumLength(1000);
        RuleFor(x => x.Request.Lines).NotNull();
        RuleForEach(x => x.Request.Lines).ChildRules(line =>
        {
            line.RuleFor(x => x.CustomerOrderLineId).NotEmpty();
        });
    }
}

public sealed class StartOpticalJobCommandValidator : AbstractValidator<StartOpticalJobCommand>
{
    public StartOpticalJobCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty();
    }
}

public sealed class MarkOpticalJobReadyCommandValidator : AbstractValidator<MarkOpticalJobReadyCommand>
{
    public MarkOpticalJobReadyCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Request.RowVersion).NotEmpty();
    }
}
