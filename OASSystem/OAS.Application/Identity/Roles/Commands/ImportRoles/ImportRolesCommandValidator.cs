using FluentValidation;
using OAS.Domain.Identity.Entities;

namespace OAS.Application.Identity.Roles.Commands.ImportRoles;

public sealed class ImportRolesCommandValidator : AbstractValidator<ImportRolesCommand>
{
    public ImportRolesCommandValidator()
    {
        RuleFor(x => x.Request.Roles)
            .NotNull().WithErrorCode("roles_import_required")
            .Must(x => x is { Count: > 0 and <= 200 }).WithErrorCode("roles_import_count_invalid");

        RuleForEach(x => x.Request.Roles).ChildRules(role =>
        {
            role.RuleFor(x => x.Name)
                .NotEmpty().WithErrorCode("role_name_required")
                .MaximumLength(64).WithErrorCode("role_name_max_length")
                .Matches("^[A-Za-z][A-Za-z0-9_.-]*$").WithErrorCode("role_name_invalid");
            role.RuleFor(x => x.DisplayName)
                .NotEmpty().WithErrorCode("role_display_name_required")
                .MaximumLength(100).WithErrorCode("role_display_name_max_length");
        });

        RuleFor(x => x.Request.Roles)
            .Must(items => items is not null && items.Where(x => !string.IsNullOrWhiteSpace(x.Name)).Select(x => UserAccount.Normalize(x.Name)).Distinct(StringComparer.Ordinal).Count() == items.Count(x => !string.IsNullOrWhiteSpace(x.Name)))
            .WithErrorCode("roles_import_duplicate_name");
    }
}
