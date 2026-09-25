namespace OAS.Contracts.Accounting.Customers;
public sealed record CustomerLookupDto(Guid Id,string CustomerCode,string AccountCode,string NameAr,bool IsActive)
{
    public string DisplayText => $"{CustomerCode} | {AccountCode} | {NameAr}";
}
