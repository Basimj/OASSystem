using FluentValidation;
using OAS.Contracts.Accounting.Enums;
namespace OAS.Application.Accounting.Customers.Commands.UpdateCustomer;
public sealed class UpdateCustomerCommandValidator:AbstractValidator<UpdateCustomerCommand>
{
    public UpdateCustomerCommandValidator()
    {
        RuleFor(x=>x.Id).NotEmpty(); RuleFor(x=>x.Request.RowVersion).NotEmpty();
        RuleFor(x=>x.Request.EntityType).Must(x=>x!=PartyEntityType.Unknown&&Enum.IsDefined(x)); RuleFor(x=>x.Request.NameAr).NotEmpty().MaximumLength(150);
        RuleFor(x=>x.Request.NameEn).MaximumLength(150); RuleFor(x=>x.Request.TradeName).MaximumLength(150); RuleFor(x=>x.Request.NationalId).MaximumLength(50);
        RuleFor(x=>x.Request.CommercialRegistrationNo).MaximumLength(50); RuleFor(x=>x.Request.TaxNumber).MaximumLength(50); RuleFor(x=>x.Request.Gender).IsInEnum(); RuleFor(x=>x.Request.PreferredContactMethod).Must(x=>x!=ContactMethod.Unspecified && Enum.IsDefined(x));
        RuleFor(x=>x.Request.DateOfBirth).Must(x=>!x.HasValue||x.Value<=DateOnly.FromDateTime(DateTime.UtcNow)); RuleFor(x=>x.Request.CreditLimit).GreaterThanOrEqualTo(0); RuleFor(x=>x.Request.PaymentTermDays).GreaterThanOrEqualTo(0);
        When(x=>!x.Request.IsCreditAllowed,()=>{RuleFor(x=>x.Request.CreditLimit).Equal(0);RuleFor(x=>x.Request.PaymentTermDays).Equal(0);}); RuleFor(x=>x.Request.Notes).MaximumLength(1000);
    }
}
