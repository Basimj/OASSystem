using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Authorization;
using OAS.Contracts.Sales.Returns;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Rules;
using DomainSalesInvoiceStatus = OAS.Domain.Sales.Enums.SalesInvoiceStatus;

namespace OAS.Application.Sales.Returns.Queries;

public sealed record GetSalesReturnCreationPolicyQuery(Guid InvoiceId)
    : IQuery<SalesReturnCreationPolicyDto>, IAuthorizedRequest
{
    public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Invoices.View];
}

/// <summary>
/// Exact same reference-date rules as CreateSalesReturnCommandHandler; returns
/// a value instead of raising a normal user-input ConflictException.
/// </summary>
public sealed class GetSalesReturnCreationPolicyQueryHandler(
    IReadRepository<SalesInvoice, Guid> invoices,
    IReadRepository<OpticalJob, Guid> opticalJobs,
    IPermissionChecker permissions)
    : IRequestHandler<GetSalesReturnCreationPolicyQuery, SalesReturnCreationPolicyDto>
{
    public async Task<SalesReturnCreationPolicyDto> Handle(GetSalesReturnCreationPolicyQuery request, CancellationToken ct)
    {
        var invoice = await invoices.GetByIdAsync(request.InvoiceId, ct);
        if (invoice is null || invoice.Status != DomainSalesInvoiceStatus.Posted)
            return new(request.InvoiceId, false, null, null, "يجب اختيار فاتورة مبيعات مرحلة.");

        var jobs = await opticalJobs.ListAsync(new Specification<OpticalJob>()
            .Where(x => x.SalesInvoiceId == invoice.Id), ct);
        DateOnly reference;
        if (jobs.Count == 0)
            reference = invoice.PostingDate;
        else
        {
            var delivered = jobs.Where(x => x.DeliveredAtUtc.HasValue)
                .Select(x => x.DeliveredAtUtc!.Value).ToArray();
            if (delivered.Length == 0)
                return new(invoice.Id, false, null, null, "لا يمكن إرجاع فاتورة معمل لم يسلم إنتاجها بعد.");
            reference = DateOnly.FromDateTime(delivered.Max().UtcDateTime);
        }
        var canOverride = await permissions.HasPermissionAsync(SalesPermissions.Returns.OverrideWindow, ct);
        return new(invoice.Id, true, reference,
            reference.AddDays(SalesReturnPolicy.DefaultReturnWindowDays), null, canOverride);
    }
}
