using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.CRUD.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Specifications;

public sealed class PrescriptionSpecificationFactory : ICrudSpecificationFactory<Prescription>
{
    public ISpecification<Prescription> CreatePageSpecification(PageRequest request)
    {
        var r = request.Normalize();
        var spec = new Specification<Prescription>();
        if (!string.IsNullOrWhiteSpace(r.Search))
        {
            var q = r.Search;
            spec.Where(x => x.PrescriptionCode.Contains(q!) || (x.PrescribedBy != null && x.PrescribedBy.Contains(q!)) ||
                            (x.ClinicName != null && x.ClinicName.Contains(q!)));
        }
        var sort = r.SortBy is nameof(Prescription.PrescriptionCode) or nameof(Prescription.PrescriptionDate) or nameof(Prescription.Status)
            ? r.SortBy!
            : nameof(Prescription.PrescriptionDate);
        spec.AddSort(sort, r.SortDirection).ApplyPaging((r.PageNumber - 1) * r.PageSize, r.PageSize);
        return spec;
    }
}
