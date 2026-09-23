using OAS.Contracts.Accounting.Enums;
namespace OAS.Contracts.Accounting.Customers;
public sealed record CreateCustomerRequest(
    string? CustomerCode,Guid ParentAccountId,PartyEntityType EntityType,string NameAr,string? NameEn,string? TradeName,string? NationalId,
    string? CommercialRegistrationNo,string? TaxNumber,DateOnly? DateOfBirth,Gender Gender,string? ContactPersonName,string? ContactPersonTitle,
    string? Phone,string? Mobile,string? AlternatePhone,string? WhatsAppNumber,string? Email,string? Website,ContactMethod PreferredContactMethod,
    string? Country,string? Governorate,string? City,string? District,string? Street,string? Building,string? PostalCode,string? AddressDetails,
    bool IsCreditAllowed,decimal CreditLimit,int PaymentTermDays,DateOnly? CustomerSince,bool IsActive,string? Notes);
