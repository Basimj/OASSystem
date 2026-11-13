using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Sales.Prescriptions;
using OAS.Domain.Sales.Entities;

namespace OAS.Application.Sales.Prescriptions.Queries;

public sealed record GetPrescriptionRelatedDocumentsQuery(Guid PrescriptionId)
    : IQuery<IReadOnlyList<PrescriptionRelatedDocumentDto>>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Prescriptions.View, SalesPermissions.Orders.View, SalesPermissions.Invoices.View];
}

public sealed class GetPrescriptionRelatedDocumentsQueryHandler(
    IPrescriptionAggregateRepository prescriptions,
    IReadRepository<CustomerOrder, Guid> orders,
    IReadRepository<CustomerOrderLine, Guid> orderLines,
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<SalesInvoiceLine, Guid> invoiceLines)
    : IRequestHandler<GetPrescriptionRelatedDocumentsQuery, IReadOnlyList<PrescriptionRelatedDocumentDto>>
{
    public async Task<IReadOnlyList<PrescriptionRelatedDocumentDto>> Handle(GetPrescriptionRelatedDocumentsQuery request, CancellationToken ct)
    {
        var prescription = await prescriptions.GetAggregateAsync(request.PrescriptionId, false, ct);
        if (prescription is null) return [];
        var revisionNumbers = prescription.Revisions.ToDictionary(x => x.Id, x => x.RevisionNumber);
        var revisionIds = revisionNumbers.Keys.ToArray();
        if (revisionIds.Length == 0) return [];

        // Document headers and individual lines may each reference a stored revision.
        // Fetch both kinds; never infer links from the customer's identity alone.
        var matchingOrderLines = await orderLines.ListAsync(new Specification<CustomerOrderLine>()
            .Where(x => x.PrescriptionRevisionId.HasValue && revisionIds.Contains(x.PrescriptionRevisionId.Value)), ct);
        var matchingOrderIds = matchingOrderLines.Select(x => x.CustomerOrderId).Distinct().ToArray();
        var matchingOrders = await orders.ListAsync(new Specification<CustomerOrder>()
            .Where(x => (x.PrescriptionRevisionId.HasValue && revisionIds.Contains(x.PrescriptionRevisionId.Value)) ||
                        matchingOrderIds.Contains(x.Id)), ct);

        var matchingInvoiceLines = await invoiceLines.ListAsync(new Specification<SalesInvoiceLine>()
            .Where(x => x.PrescriptionRevisionId.HasValue && revisionIds.Contains(x.PrescriptionRevisionId.Value)), ct);
        var matchingInvoiceIds = matchingInvoiceLines.Select(x => x.SalesInvoiceId).Distinct().ToArray();
        var matchingInvoices = await invoices.ListAsync(new Specification<SalesInvoice>()
            .Where(x => (x.PrescriptionRevisionId.HasValue && revisionIds.Contains(x.PrescriptionRevisionId.Value)) ||
                        matchingInvoiceIds.Contains(x.Id)), ct);

        static string Labels(IEnumerable<Guid> ids, Dictionary<Guid, int> numbers) =>
            string.Join("، ", ids.Distinct().Where(numbers.ContainsKey).Select(x => numbers[x]).OrderBy(x => x));
        var result = new List<PrescriptionRelatedDocumentDto>();
        foreach (var order in matchingOrders)
        {
            var ids = matchingOrderLines.Where(x => x.CustomerOrderId == order.Id)
                .Select(x => x.PrescriptionRevisionId!.Value).ToList();
            if (order.PrescriptionRevisionId.HasValue) ids.Add(order.PrescriptionRevisionId.Value);
            result.Add(new(order.Id, order.OrderCode, "CustomerOrder", order.OrderDate,
                Labels(ids, revisionNumbers)));
        }
        foreach (var invoice in matchingInvoices)
        {
            var ids = matchingInvoiceLines.Where(x => x.SalesInvoiceId == invoice.Id)
                .Select(x => x.PrescriptionRevisionId!.Value).ToList();
            if (invoice.PrescriptionRevisionId.HasValue) ids.Add(invoice.PrescriptionRevisionId.Value);
            result.Add(new(invoice.Id, invoice.InvoiceCode, "SalesInvoice", invoice.InvoiceDate,
                Labels(ids, revisionNumbers)));
        }
        return result.OrderByDescending(x => x.DocumentDate).ThenBy(x => x.DocumentCode).ToArray();
    }
}
