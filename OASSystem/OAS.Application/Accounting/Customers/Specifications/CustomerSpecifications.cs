using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Domain.Accounting.Entities;
namespace OAS.Application.Accounting.Customers.Specifications;
public static class CustomerSpecifications
{
    public static Specification<Customer> ByCode(string code)=>new Specification<Customer>().Where(x=>x.CustomerCode==code);
    public static Specification<Customer> ByNationalId(string value,Guid? except=null)=>new Specification<Customer>().Where(x=>x.NationalId==value && (!except.HasValue || x.Id!=except.Value));
    public static Specification<Customer> ByTaxNumber(string value,Guid? except=null)=>new Specification<Customer>().Where(x=>x.TaxNumber==value && (!except.HasValue || x.Id!=except.Value));
    public static Specification<Customer> ByCommercialRegistration(string value,Guid? except=null)=>new Specification<Customer>().Where(x=>x.CommercialRegistrationNo==value && (!except.HasValue || x.Id!=except.Value));
}
