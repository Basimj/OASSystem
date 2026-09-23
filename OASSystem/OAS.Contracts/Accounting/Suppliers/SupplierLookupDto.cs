namespace OAS.Contracts.Accounting.Suppliers;
public sealed record SupplierLookupDto(Guid Id,string SupplierCode,string AccountCode,string NameAr,bool IsActive)
{
    public string DisplayText => $"{SupplierCode} | {AccountCode} | {NameAr}";
}
