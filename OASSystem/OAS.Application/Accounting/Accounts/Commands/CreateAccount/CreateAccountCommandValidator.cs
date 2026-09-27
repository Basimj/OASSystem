using FluentValidation;

namespace OAS.Application.Accounting.Accounts.Commands.CreateAccount;

public sealed class CreateAccountCommandValidator
    : AbstractValidator<CreateAccountCommand>
{
    public CreateAccountCommandValidator()
    {
        RuleFor(x => x.Data.Code)
            .Cascade(CascadeMode.Stop)

            .NotEmpty()
            .WithMessage(
                "كود الحساب مطلوب.")
            .WithErrorCode(
                "account_code_required")

            .Matches(@"^[0-9]+$")
            .WithMessage(
                "كود الحساب يجب أن يتكون من أرقام فقط.")
            .WithErrorCode(
                "account_code_numeric_only")

            .MaximumLength(30)
            .WithMessage(
                "كود الحساب يجب ألا يزيد عن 30 رقمًا.")
            .WithErrorCode(
                "account_code_max_length");


        RuleFor(x => x.Data.NameAr)
            .Cascade(CascadeMode.Stop)

            .NotEmpty()
            .WithMessage(
                "اسم الحساب بالعربية مطلوب.")
            .WithErrorCode(
                "account_name_ar_required")

            .MaximumLength(150)
            .WithMessage(
                "اسم الحساب بالعربية يجب ألا يزيد عن 150 حرفًا.")
            .WithErrorCode(
                "account_name_ar_max_length");


        RuleFor(x => x.Data.NameEn)
            .MaximumLength(150)
            .WithMessage(
                "اسم الحساب بالإنجليزية يجب ألا يزيد عن 150 حرفًا.")
            .WithErrorCode(
                "account_name_en_max_length")
            .When(
                x =>
                    !string.IsNullOrWhiteSpace(
                        x.Data.NameEn));


        RuleFor(x => x.Data.Level)
            .GreaterThan((byte)0)
            .WithMessage(
                "مستوى الحساب غير صالح.")
            .WithErrorCode(
                "account_level_invalid");


        RuleFor(x => x.Data.ParentAccountId)
            .Must(
                parentId =>
                    parentId is null ||
                    parentId != Guid.Empty)
            .WithMessage(
                "الحساب الأب غير صالح.")
            .WithErrorCode(
                "parent_account_invalid");


        RuleFor(x => x.Data.EffectiveDate)
            .Must(
                date =>
                    date is null ||
                    date.Value != default)
            .WithMessage(
                "تاريخ السريان غير صالح.")
            .WithErrorCode(
                "effective_date_invalid");
    }
}