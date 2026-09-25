using MediatR;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Customers.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.ValueObjects;
using DomainContactMethod=OAS.Domain.Accounting.Enums.ContactMethod;
using DomainGender=OAS.Domain.Accounting.Enums.Gender;
using DomainPartyEntityType=OAS.Domain.Accounting.Enums.PartyEntityType;
namespace OAS.Application.Accounting.Customers.Commands.UpdateCustomer;
public sealed class UpdateCustomerCommandHandler(IRepository<Customer,Guid> customers,IPartyAccountProvisioningService accounts):IRequestHandler<UpdateCustomerCommand>
{
    public async Task Handle(UpdateCustomerCommand request,CancellationToken ct)
    {
        var entity=await customers.GetForUpdateAsync(request.Id,ct)??throw new NotFoundException(nameof(Customer),request.Id);
        EnsureConcurrency(request.Request.RowVersion,entity.RowVersion);
        var d=request.Request;
        if(!string.IsNullOrWhiteSpace(d.NationalId)&&await customers.CountAsync(CustomerSpecifications.ByNationalId(d.NationalId.Trim(),entity.Id),ct)>0)throw new ConflictException("accounting_customer_national_id_duplicate","National id is already in use.");
        if(!string.IsNullOrWhiteSpace(d.TaxNumber)&&await customers.CountAsync(CustomerSpecifications.ByTaxNumber(d.TaxNumber.Trim(),entity.Id),ct)>0)throw new ConflictException("accounting_customer_tax_number_duplicate","Tax number is already in use.");
        if(!string.IsNullOrWhiteSpace(d.CommercialRegistrationNo)&&await customers.CountAsync(CustomerSpecifications.ByCommercialRegistration(d.CommercialRegistrationNo.Trim(),entity.Id),ct)>0)throw new ConflictException("accounting_customer_cr_duplicate","Commercial registration number is already in use.");
        var contact=PartyContactInfo.Create(d.ContactPersonName,d.ContactPersonTitle,d.Phone,d.Mobile,d.AlternatePhone,d.WhatsAppNumber,d.Email,d.Website,(DomainContactMethod)(byte)d.PreferredContactMethod,
            PartyAddress.Create(d.Country,d.Governorate,d.City,d.District,d.Street,d.Building,d.PostalCode,d.AddressDetails));
        entity.UpdateDetails((DomainPartyEntityType)(byte)d.EntityType,d.NameAr,d.NameEn,d.TradeName,d.NationalId,d.CommercialRegistrationNo,d.TaxNumber,d.DateOfBirth,(DomainGender)(byte)d.Gender,contact,d.IsCreditAllowed,d.CreditLimit,d.PaymentTermDays,d.CustomerSince,d.Notes);
        customers.Update(entity); await accounts.SynchronizeAsync(entity.AccountId,entity.NameAr,entity.NameEn,null,ct);
    }
    private static void EnsureConcurrency(string rowVersion,byte[] current){try{var incoming=Convert.FromBase64String(rowVersion);if(!incoming.SequenceEqual(current))throw new ConcurrencyException("Customer was modified by another user.");}catch(FormatException ex){throw new ConcurrencyException("Customer row version is invalid.",ex);}}
}
