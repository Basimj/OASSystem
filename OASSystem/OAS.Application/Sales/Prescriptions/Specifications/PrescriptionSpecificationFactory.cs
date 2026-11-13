using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Enums;

namespace OAS.Application.Sales.Prescriptions.Specifications;

public sealed class PrescriptionSpecificationFactory : ICrudSpecificationFactory<Prescription>
{
    public ISpecification<Prescription> CreatePageSpecification(PageRequest request) =>
        CreatePageSpecification(request, []);

    /// <summary>Search includes matching customer IDs resolved by the query handler.</summary>
    public ISpecification<Prescription> CreatePageSpecification(PageRequest request, Guid[] customerIds)
    {
        var r = request.Normalize();
        var spec = new Specification<Prescription>();
        var q = r.Search;
        var hasSearch = !string.IsNullOrWhiteSpace(q);
        var hasStatus = Enum.TryParse<PrescriptionStatus>(r.Status, true, out var status) &&
                        Enum.IsDefined(status);

        // One combined predicate: Specification.Where replaces rather than appends criteria.
        spec.Where(x =>
            (!hasSearch || x.PrescriptionCode.Contains(q!) ||
             (x.PrescribedBy != null && x.PrescribedBy.Contains(q!)) ||
             (x.ClinicName != null && x.ClinicName.Contains(q!)) ||
             customerIds.Contains(x.CustomerId)) &&
            (!hasStatus || x.Status == status));

        var sort = r.SortBy is nameof(Prescription.PrescriptionCode) or nameof(Prescription.PrescriptionDate) or nameof(Prescription.Status)
            ? r.SortBy!
            : nameof(Prescription.PrescriptionDate);
        spec.AddSort(sort, r.SortDirection).ApplyPaging((r.PageNumber - 1) * r.PageSize, r.PageSize);
        return spec;
    }
}
