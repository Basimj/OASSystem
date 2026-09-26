using FluentValidation;

namespace OAS.Application.Accounting.Currencies.Commands.CreateCurrency;

public sealed class CreateCurrencyCommandValidator
    : AbstractValidator<CreateCurrencyCommand>
{
    public CreateCurrencyCommandValidator()
    {
        RuleFor(x => x.Request.Code)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("ßæÏ ÇáÚãáÉ ãØáæÈ.")
            .MinimumLength(3)
            .WithMessage("ßæÏ ÇáÚãáÉ íÌÈ ÃáÇ íŞá Úä 3 ÃÍÑİ.")
            .MaximumLength(8)
            .WithMessage("ßæÏ ÇáÚãáÉ íÌÈ ÃáÇ íÒíÏ Úä 8 ÃÍÑİ.");

        RuleFor(x => x.Request.NameAr)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("ÇÓã ÇáÚãáÉ ÈÇáÚÑÈí ãØáæÈ.")
            .MaximumLength(100)
            .WithMessage("ÇÓã ÇáÚãáÉ ÈÇáÚÑÈí íÌÈ ÃáÇ íÒíÏ Úä 100 ÍÑİ.");

        RuleFor(x => x.Request.NameEn)
            .MaximumLength(100)
            .WithMessage("ÇÓã ÇáÚãáÉ ÈÇáÅäÌáíÒí íÌÈ ÃáÇ íÒíÏ Úä 100 ÍÑİ.");

        RuleFor(x => x.Request.Symbol)
            .MaximumLength(12)
            .WithMessage("ÑãÒ ÇáÚãáÉ íÌÈ ÃáÇ íÒíÏ Úä 12 ÍÑİğÇ.");

        RuleFor(x => x.Request.DecimalPlaces)
            .InclusiveBetween((byte)0, (byte)6)
            .WithMessage("ÚÏÏ ÇáÎÇäÇÊ ÇáÚÔÑíÉ íÌÈ Ãä íßæä Èíä 0 æ6.");
    }
}