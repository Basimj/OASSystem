using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.Suppliers;
public sealed record UpdateSupplierRequest(
    PartyEntityType EntityType,SupplierScope SupplierScope,string NameAr,string? NameEn,string? TradeName,string? NationalId,string? CommercialRegistrationNo,
    string? TaxNumber,string? ContactPersonName,string? ContactPersonTitle,string? Phone,string? Mobile,string? AlternatePhone,string? WhatsAppNumber,string? Email,
    string? Website,ContactMethod PreferredContactMethod,string? Country,string? Governorate,string? City,string? District,string? Street,string? Building,
    string? PostalCode,string? AddressDetails,decimal CreditLimit,int PaymentTermDays,int? DefaultLeadTimeDays,DateOnly? SupplierSince,string? Notes,string RowVersion);
