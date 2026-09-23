using MediatR;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Accounting.Abstractions;
using OAS.Application.Accounting.Suppliers.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Domain.Accounting;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.ValueObjects;
using DomainContactMethod=OAS.Domain.Accounting.Enums.ContactMethod;
using DomainPartyEntityType=OAS.Domain.Accounting.Enums.PartyEntityType;
using DomainSupplierScope=OAS.Domain.Accounting.Enums.SupplierScope;
namespace OAS.Application.Accounting.Suppliers.Commands.CreateSupplier;
public sealed class CreateSupplierCommandHandler(IRepository<Supplier,Guid> suppliers,ISequenceNumberGenerator sequences,IPartyAccountProvisioningService accounts):IRequestHandler<CreateSupplierCommand,Guid>
{
    public async Task<Guid> Handle(CreateSupplierCommand request,CancellationToken ct)
    {
        var dto=request.Request; var code=dto.SupplierCode?.Trim();
        if(string.IsNullOrWhiteSpace(code)){for(var i=0;i<100;i++){code=SupplierCodeFormatter.Format(await sequences.NextAsync("SupplierCodeSequence",ct));if(await suppliers.CountAsync(SupplierSpecifications.ByCode(code),ct)==0)break;}}
        if(string.IsNullOrWhiteSpace(code)||await suppliers.CountAsync(SupplierSpecifications.ByCode(code),ct)>0)throw new ConflictException("accounting_supplier_code_duplicate","Supplier code is already in use.");
        await EnsureUniqueAsync(dto.NationalId,dto.TaxNumber,dto.CommercialRegistrationNo,null,ct);
        var account=await accounts.ProvisionSupplierAccountAsync(dto.ParentAccountId,dto.NameAr,dto.NameEn,dto.IsActive,dto.SupplierSince,ct);
        var contact=PartyContactInfo.Create(dto.ContactPersonName,dto.ContactPersonTitle,dto.Phone,dto.Mobile,dto.AlternatePhone,dto.WhatsAppNumber,dto.Email,dto.Website,(DomainContactMethod)(byte)dto.PreferredContactMethod,
            PartyAddress.Create(dto.Country,dto.Governorate,dto.City,dto.District,dto.Street,dto.Building,dto.PostalCode,dto.AddressDetails));
        var entity=Supplier.Create(Guid.NewGuid(),code,account.Id,(DomainPartyEntityType)(byte)dto.EntityType,(DomainSupplierScope)(byte)dto.SupplierScope,dto.NameAr,dto.NameEn,dto.TradeName,dto.NationalId,dto.CommercialRegistrationNo,dto.TaxNumber,contact,dto.CreditLimit,dto.PaymentTermDays,dto.DefaultLeadTimeDays,dto.SupplierSince,dto.IsActive,dto.Notes);
        await suppliers.AddAsync(entity,ct);return entity.Id;
    }
    private async Task EnsureUniqueAsync(string? national,string? tax,string? cr,Guid? except,CancellationToken ct)
    {
        if(!string.IsNullOrWhiteSpace(national)&&await suppliers.CountAsync(SupplierSpecifications.ByNationalId(national.Trim(),except),ct)>0)throw new ConflictException("accounting_supplier_national_id_duplicate","National id is already in use.");
        if(!string.IsNullOrWhiteSpace(tax)&&await suppliers.CountAsync(SupplierSpecifications.ByTaxNumber(tax.Trim(),except),ct)>0)throw new ConflictException("accounting_supplier_tax_number_duplicate","Tax number is already in use.");
        if(!string.IsNullOrWhiteSpace(cr)&&await suppliers.CountAsync(SupplierSpecifications.ByCommercialRegistration(cr.Trim(),except),ct)>0)throw new ConflictException("accounting_supplier_cr_duplicate","Commercial registration number is already in use.");
    }
}
