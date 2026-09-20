using FluentValidation;
using OAS.Contracts.Inventory.Products;

namespace OAS.Application.Inventory.Products.FrameDetails;

public sealed class UpdateFrameDetailsRequestValidator : AbstractValidator<UpdateFrameDetailsRequest>
{
    public UpdateFrameDetailsRequestValidator()
    {
        RuleFor(x => x.Model)
            .NotEmpty().WithErrorCode("frame_model_required")
            .MaximumLength(100).WithErrorCode("frame_model_max_length");

        RuleFor(x => x.Material)
            .MaximumLength(50).WithErrorCode("frame_material_max_length");

        RuleFor(x => x.RimType)
            .MaximumLength(50).WithErrorCode("frame_rim_type_max_length");

        RuleFor(x => x.Gender)
            .MaximumLength(20).WithErrorCode("frame_gender_max_length");

        RuleFor(x => x.Shape)
            .MaximumLength(50).WithErrorCode("frame_shape_max_length");
    }
}
