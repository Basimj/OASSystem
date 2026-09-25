using OAS.Domain.Accounting.Enums;
using OAS.Domain.Accounting.ValueObjects;
using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Accounting.Entities;

public sealed class Supplier : AuditableEntity<Guid>
{
    private Supplier() => ContactInfo = PartyContactInfo.Empty;

    private Supplier(Guid id, string supplierCode, Guid accountId, PartyEntityType entityType, SupplierScope supplierScope,
        string nameAr, string? nameEn, string? tradeName, string? nationalId, string? commercialRegistrationNo,
        string? taxNumber, PartyContactInfo contactInfo, decimal creditLimit, int paymentTermDays,
        int? defaultLeadTimeDays, DateOnly? supplierSince, bool isActive, string? notes)
    {
        if(id==Guid.Empty) throw new DomainException("Supplier id is required.");
        Id=id;
        if(accountId==Guid.Empty) throw new DomainException("Supplier account id is required.");
        SupplierCode=NormalizeRequired(supplierCode,32,"Supplier code");
        AccountId=accountId;
        UpdateDetails(entityType,supplierScope,nameAr,nameEn,tradeName,nationalId,commercialRegistrationNo,taxNumber,
            contactInfo,creditLimit,paymentTermDays,defaultLeadTimeDays,supplierSince,notes);
        IsActive=isActive;
    }

    public string SupplierCode { get; private set; } = string.Empty;
    public Guid AccountId { get; private set; }
    public PartyEntityType EntityType { get; private set; }
    public SupplierScope SupplierScope { get; private set; }
    public string NameAr { get; private set; } = string.Empty;
    public string? NameEn { get; private set; }
    public string? TradeName { get; private set; }
    public string? NationalId { get; private set; }
    public string? CommercialRegistrationNo { get; private set; }
    public string? TaxNumber { get; private set; }
    public PartyContactInfo ContactInfo { get; private set; }
    public decimal CreditLimit { get; private set; }
    public int PaymentTermDays { get; private set; }
    public int? DefaultLeadTimeDays { get; private set; }
    public DateOnly? SupplierSince { get; private set; }
    public bool IsActive { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static Supplier Create(Guid id,string supplierCode,Guid accountId,PartyEntityType entityType,SupplierScope supplierScope,
        string nameAr,string? nameEn,string? tradeName,string? nationalId,string? commercialRegistrationNo,string? taxNumber,
        PartyContactInfo contactInfo,decimal creditLimit,int paymentTermDays,int? defaultLeadTimeDays,DateOnly? supplierSince,bool isActive,string? notes)=>
        new(id,supplierCode,accountId,entityType,supplierScope,nameAr,nameEn,tradeName,nationalId,commercialRegistrationNo,taxNumber,
            contactInfo,creditLimit,paymentTermDays,defaultLeadTimeDays,supplierSince,isActive,notes);

    public void UpdateDetails(PartyEntityType entityType,SupplierScope supplierScope,string nameAr,string? nameEn,string? tradeName,
        string? nationalId,string? commercialRegistrationNo,string? taxNumber,PartyContactInfo contactInfo,decimal creditLimit,
        int paymentTermDays,int? defaultLeadTimeDays,DateOnly? supplierSince,string? notes)
    {
        if(entityType is PartyEntityType.Unknown || !Enum.IsDefined(entityType)) throw new DomainException("Supplier entity type is required.");
        if(supplierScope is SupplierScope.Unknown || !Enum.IsDefined(supplierScope)) throw new DomainException("Supplier scope is required.");
        if(creditLimit<0) throw new DomainException("Credit limit cannot be negative.");
        if(paymentTermDays<0) throw new DomainException("Payment term days cannot be negative.");
        if(defaultLeadTimeDays<0) throw new DomainException("Default lead time days cannot be negative.");
        EntityType=entityType; SupplierScope=supplierScope; NameAr=NormalizeRequired(nameAr,150,"Arabic name");
        NameEn=NormalizeOptional(nameEn,150,"English name"); TradeName=NormalizeOptional(tradeName,150,"Trade name");
        NationalId=NormalizeOptional(nationalId,50,"National id"); CommercialRegistrationNo=NormalizeOptional(commercialRegistrationNo,50,"Commercial registration number");
        TaxNumber=NormalizeOptional(taxNumber,50,"Tax number"); ContactInfo=contactInfo??throw new DomainException("Contact information is required.");
        CreditLimit=creditLimit; PaymentTermDays=paymentTermDays; DefaultLeadTimeDays=defaultLeadTimeDays; SupplierSince=supplierSince;
        Notes=NormalizeOptional(notes,1000,"Notes");
    }

    public void SetActive(bool isActive)=>IsActive=isActive;
    private static string NormalizeRequired(string value,int max,string name){if(string.IsNullOrWhiteSpace(value))throw new DomainException($"{name} is required.");var n=value.Trim();if(n.Length>max)throw new DomainException($"{name} cannot exceed {max} characters.");return n;}
    private static string? NormalizeOptional(string? value,int max,string name){if(string.IsNullOrWhiteSpace(value))return null;var n=value.Trim();if(n.Length>max)throw new DomainException($"{name} cannot exceed {max} characters.");return n;}
}
