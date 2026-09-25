using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.Suppliers.Specifications;
public static class SupplierSpecifications
{
    public static Specification<Supplier> ByCode(string code)=>new Specification<Supplier>().Where(x=>x.SupplierCode==code);
    public static Specification<Supplier> ByNationalId(string value,Guid? except=null)=>new Specification<Supplier>().Where(x=>x.NationalId==value && (!except.HasValue || x.Id!=except.Value));
    public static Specification<Supplier> ByTaxNumber(string value,Guid? except=null)=>new Specification<Supplier>().Where(x=>x.TaxNumber==value && (!except.HasValue || x.Id!=except.Value));
    public static Specification<Supplier> ByCommercialRegistration(string value,Guid? except=null)=>new Specification<Supplier>().Where(x=>x.CommercialRegistrationNo==value && (!except.HasValue || x.Id!=except.Value));
}
