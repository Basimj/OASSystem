using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Customers.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.ValueObjects;
using DomainContactMethod=OAS.Domain.Accounting.Enums.ContactMethod;
using DomainGender=OAS.Domain.Accounting.Enums.Gender;
using DomainPartyEntityType=OAS.Domain.Accounting.Enums.PartyEntityType;
namespace OAS.Application.Accounting.Customers.Commands.CreateCustomer;
public sealed class CreateCustomerCommandHandler(IRepository<Customer,Guid> customers,ISequenceNumberGenerator sequences,IPartyAccountProvisioningService accounts):IRequestHandler<CreateCustomerCommand,Guid>
{
    public async Task<Guid> Handle(CreateCustomerCommand request,CancellationToken ct)
    {
        var dto=request.Request;
        var code=dto.CustomerCode?.Trim();
        if(string.IsNullOrWhiteSpace(code))
        {
            for(var i=0;i<100;i++) { code=CustomerCodeFormatter.Format(await sequences.NextAsync("CustomerCodeSequence",ct)); if(await customers.CountAsync(CustomerSpecifications.ByCode(code),ct)==0) break; }
        }
        if(string.IsNullOrWhiteSpace(code) || await customers.CountAsync(CustomerSpecifications.ByCode(code),ct)>0)
            throw new ConflictException("accounting_customer_code_duplicate","Customer code is already in use.");
        await EnsureUniqueAsync(dto.NationalId,dto.TaxNumber,dto.CommercialRegistrationNo,null,ct);
        var account=await accounts.ProvisionCustomerAccountAsync(dto.ParentAccountId,dto.NameAr,dto.NameEn,dto.IsActive,dto.CustomerSince,ct);
        var contact=PartyContactInfo.Create(dto.ContactPersonName,dto.ContactPersonTitle,dto.Phone,dto.Mobile,dto.AlternatePhone,dto.WhatsAppNumber,dto.Email,dto.Website,(DomainContactMethod)(byte)dto.PreferredContactMethod,
            PartyAddress.Create(dto.Country,dto.Governorate,dto.City,dto.District,dto.Street,dto.Building,dto.PostalCode,dto.AddressDetails));
        var entity=Customer.Create(Guid.NewGuid(),code,account.Id,(DomainPartyEntityType)(byte)dto.EntityType,dto.NameAr,dto.NameEn,dto.TradeName,dto.NationalId,dto.CommercialRegistrationNo,dto.TaxNumber,dto.DateOfBirth,(DomainGender)(byte)dto.Gender,contact,dto.IsCreditAllowed,dto.CreditLimit,dto.PaymentTermDays,dto.CustomerSince,dto.IsActive,dto.Notes);
        await customers.AddAsync(entity,ct); return entity.Id;
    }
    private async Task EnsureUniqueAsync(string? national,string? tax,string? cr,Guid? except,CancellationToken ct)
    {
        if(!string.IsNullOrWhiteSpace(national) && await customers.CountAsync(CustomerSpecifications.ByNationalId(national.Trim(),except),ct)>0) throw new ConflictException("accounting_customer_national_id_duplicate","National id is already in use.");
        if(!string.IsNullOrWhiteSpace(tax) && await customers.CountAsync(CustomerSpecifications.ByTaxNumber(tax.Trim(),except),ct)>0) throw new ConflictException("accounting_customer_tax_number_duplicate","Tax number is already in use.");
        if(!string.IsNullOrWhiteSpace(cr) && await customers.CountAsync(CustomerSpecifications.ByCommercialRegistration(cr.Trim(),except),ct)>0) throw new ConflictException("accounting_customer_cr_duplicate","Commercial registration number is already in use.");
    }
}
