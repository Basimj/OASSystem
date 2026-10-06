using Microsoft.EntityFrameworkCore;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Contracts.Common.Pagination;
using OAS.Contracts.Sales.CustomerOrders;
using OAS.Contracts.Sales.Enums;
using OAS.Contracts.Sales.OpticalJobs;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Infrastructure.Persistence;
using ContractEyeSide = OAS.Contracts.Sales.Enums.EyeSide;
using ContractLineType = OAS.Contracts.Sales.Enums.SalesLineType;
using ContractJobStatus = OAS.Contracts.Sales.Enums.OpticalJobStatus;
using ContractMeasurementSource = OAS.Contracts.Sales.Enums.OpticalMeasurementSource;
using ContractPrismBase = OAS.Contracts.Sales.Enums.PrismBaseDirection;

namespace OAS.Infrastructure.Sales.Persistence.Queries;

/// <summary>
/// Finance-free laboratory read model. This service intentionally projects only
/// operational/customer/optical data; invoice amounts, payments, cost, accounts and
/// journal identifiers are never selected into the lab contract.
/// </summary>
public sealed class OpticalJobQueryService(OasDbContext db) : IOpticalJobQueryService
{
    public async Task<PagedResult<OpticalJobWorkQueueDto>> GetWorkQueueAsync(
        OpticalJobWorkQueueRequest request,
        CancellationToken cancellationToken = default)
    {
        var page = request.ToPageRequest().Normalize();
        var query =
            from job in db.Set<OpticalJob>().AsNoTracking()
            join order in db.Set<CustomerOrder>().AsNoTracking() on job.CustomerOrderId equals order.Id
            join customer in db.Set<Customer>().AsNoTracking() on job.CustomerId equals customer.Id
            join employee in db.Set<Employee>().AsNoTracking() on job.AssignedTechnicianId equals (Guid?)employee.Id into employeeJoin
            from employee in employeeJoin.DefaultIfEmpty()
            where job.IsActive || job.Status == OAS.Domain.Sales.Enums.OpticalJobStatus.Delivered
            select new JobRow
            {
                Id = job.Id,
                JobCode = job.JobCode,
                CustomerOrderId = job.CustomerOrderId,
                SalesInvoiceId = job.SalesInvoiceId,
                CustomerId = job.CustomerId,
                RequiredDate = job.RequiredDate,
                Status = job.Status,
                AssignedTechnicianId = job.AssignedTechnicianId,
                StartedAtUtc = job.StartedAtUtc,
                CompletedAtUtc = job.CompletedAtUtc,
                Notes = job.Notes,
                RowVersion = job.RowVersion,
                CustomerOrderCode = order.OrderCode,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.NameAr,
                Mobile = customer.ContactInfo.Mobile,
                AssignedTechnicianName = employee == null ? null : employee.FirstName + " " + employee.LastName
            };

        if (request.Status.HasValue)
            query = query.Where(x => (byte)x.Status == (byte)request.Status.Value);
        if (request.TechnicianId.HasValue)
            query = query.Where(x => x.AssignedTechnicianId == request.TechnicianId.Value);
        if (request.RequiredDate.HasValue)
            query = query.Where(x => x.RequiredDate == request.RequiredDate.Value);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            query = query.Where(x => x.JobCode.Contains(term) || x.CustomerOrderCode.Contains(term) ||
                                     x.CustomerCode.Contains(term) || x.CustomerName.Contains(term) ||
                                     (x.Mobile != null && x.Mobile.Contains(term)));
        }

        query = ApplySort(query, request.SortBy, request.SortDirection);
        var total = await query.LongCountAsync(cancellationToken);
        var rows = await query.Skip((page.PageNumber - 1) * page.PageSize).Take(page.PageSize).ToListAsync(cancellationToken);
        var summaries = await BuildLineSummariesAsync(rows.Select(x => x.Id).ToArray(), cancellationToken);

        return new PagedResult<OpticalJobWorkQueueDto>
        {
            Items = rows.Select(x =>
            {
                summaries.TryGetValue(x.Id, out var summary);
                return new OpticalJobWorkQueueDto(
                    x.Id,
                    x.JobCode,
                    x.CustomerOrderId,
                    x.SalesInvoiceId,
                    x.CustomerId,
                    x.RequiredDate,
                    (ContractJobStatus)(byte)x.Status,
                    x.AssignedTechnicianId,
                    x.StartedAtUtc,
                    x.CompletedAtUtc,
                    x.Notes,
                    Convert.ToBase64String(x.RowVersion),
                    x.CustomerOrderCode,
                    x.CustomerCode,
                    x.CustomerName,
                    x.Mobile,
                    summary?.Frame,
                    summary?.OD,
                    summary?.OS,
                    x.AssignedTechnicianName);
            }).ToArray(),
            PageNumber = page.PageNumber,
            PageSize = page.PageSize,
            TotalCount = total
        };
    }

    public async Task<OpticalJobDetailsDto> GetDetailsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var row = await (
            from job in db.Set<OpticalJob>().AsNoTracking()
            join order in db.Set<CustomerOrder>().AsNoTracking() on job.CustomerOrderId equals order.Id
            join customer in db.Set<Customer>().AsNoTracking() on job.CustomerId equals customer.Id
            join employee in db.Set<Employee>().AsNoTracking() on job.AssignedTechnicianId equals (Guid?)employee.Id into employeeJoin
            from employee in employeeJoin.DefaultIfEmpty()
            where job.Id == id
            select new
            {
                Job = job,
                OrderCode = order.OrderCode,
                CustomerCode = customer.CustomerCode,
                CustomerName = customer.NameAr,
                Mobile = customer.ContactInfo.Mobile,
                TechnicianName = employee == null ? null : employee.FirstName + " " + employee.LastName
            }).SingleOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException(nameof(OpticalJob), id);

        var lines = await BuildDetailsLinesAsync(id, cancellationToken);
        var jobEntity = row.Job;
        return new OpticalJobDetailsDto(
            jobEntity.Id,
            jobEntity.JobCode,
            jobEntity.CustomerOrderId,
            jobEntity.SalesInvoiceId,
            jobEntity.CustomerId,
            jobEntity.RequiredDate,
            (ContractJobStatus)(byte)jobEntity.Status,
            jobEntity.AssignedTechnicianId,
            jobEntity.StartedAtUtc,
            jobEntity.CompletedAtUtc,
            jobEntity.Notes,
            jobEntity.IsActive,
            Convert.ToBase64String(jobEntity.RowVersion),
            jobEntity.CreatedAtUtc,
            jobEntity.CreatedBy,
            jobEntity.LastModifiedAtUtc,
            jobEntity.LastModifiedBy,
            lines,
            row.OrderCode,
            row.CustomerCode,
            row.CustomerName,
            row.Mobile,
            row.TechnicianName);
    }

    private async Task<IReadOnlyList<OpticalJobLineDto>> BuildDetailsLinesAsync(Guid jobId, CancellationToken ct)
    {
        var lines = await db.Set<OpticalJobLine>().AsNoTracking().Where(x => x.OpticalJobId == jobId).OrderBy(x => x.LineNumber).ToListAsync(ct);
        if (lines.Count == 0) return [];
        var variantIds = lines.Where(x => x.ProductVariantId.HasValue).Select(x => x.ProductVariantId!.Value).Distinct().ToArray();
        var variants = await db.Set<ProductVariant>().AsNoTracking().Where(x => variantIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var productIds = variants.Values.Select(x => x.ProductId).Distinct().ToArray();
        var products = await db.Set<Product>().AsNoTracking().Where(x => productIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        var customerLineIds = lines.Select(x => x.CustomerOrderLineId).Distinct().ToArray();
        var snapshots = await db.Set<CustomerOrderLineOpticalSnapshot>().AsNoTracking()
            .Where(x => customerLineIds.Contains(x.CustomerOrderLineId) && x.IsActive)
            .ToDictionaryAsync(x => x.CustomerOrderLineId, ct);

        return lines.Select(line =>
        {
            variants.TryGetValue(line.ProductVariantId ?? Guid.Empty, out var variant);
            Product? product = variant is null ? null : products.GetValueOrDefault(variant.ProductId);
            snapshots.TryGetValue(line.CustomerOrderLineId, out var snapshot);
            return new OpticalJobLineDto(
                line.Id,
                line.OpticalJobId,
                line.CustomerOrderLineId,
                line.ProductVariantId,
                line.LineNumber,
                (ContractLineType)(byte)line.LineType,
                line.Eye.HasValue ? (ContractEyeSide?)(byte)line.Eye.Value : null,
                line.DescriptionSnapshot,
                line.Quantity,
                line.Notes,
                Convert.ToBase64String(line.RowVersion),
                variant?.SKU,
                product?.NameAr ?? line.DescriptionSnapshot,
                snapshot is null ? null : MapSnapshot(snapshot));
        }).ToArray();
    }

    private async Task<Dictionary<Guid, JobSummary>> BuildLineSummariesAsync(Guid[] jobIds, CancellationToken ct)
    {
        if (jobIds.Length == 0) return [];
        var lines = await db.Set<OpticalJobLine>().AsNoTracking()
            .Where(x => jobIds.Contains(x.OpticalJobId))
            .OrderBy(x => x.LineNumber)
            .ToListAsync(ct);
        var customerLineIds = lines.Select(x => x.CustomerOrderLineId).Distinct().ToArray();
        var snapshots = await db.Set<CustomerOrderLineOpticalSnapshot>().AsNoTracking()
            .Where(x => customerLineIds.Contains(x.CustomerOrderLineId) && x.IsActive)
            .ToDictionaryAsync(x => x.CustomerOrderLineId, ct);

        var result = new Dictionary<Guid, JobSummary>();
        foreach (var group in lines.GroupBy(x => x.OpticalJobId))
        {
            string? frame = group.FirstOrDefault(x => x.LineType == OAS.Domain.Sales.Enums.SalesLineType.Frame)?.DescriptionSnapshot;
            string? od = null;
            string? os = null;
            foreach (var line in group.Where(x => x.LineType == OAS.Domain.Sales.Enums.SalesLineType.Lens))
            {
                snapshots.TryGetValue(line.CustomerOrderLineId, out var snapshot);
                var text = FormatOptical(line.DescriptionSnapshot, snapshot);
                var eye = snapshot?.Eye ?? line.Eye;
                if (eye == OAS.Domain.Sales.Enums.EyeSide.RightOD) od = text;
                else if (eye == OAS.Domain.Sales.Enums.EyeSide.LeftOS) os = text;
            }
            result[group.Key] = new JobSummary(frame, od, os);
        }
        return result;
    }

    private static IQueryable<JobRow> ApplySort(IQueryable<JobRow> query, string? sortBy, SortDirection direction)
    {
        var key = sortBy?.Trim().ToLowerInvariant();
        var desc = direction == SortDirection.Descending;
        return key switch
        {
            "jobcode" => desc ? query.OrderByDescending(x => x.JobCode) : query.OrderBy(x => x.JobCode),
            "order" or "customerordercode" => desc ? query.OrderByDescending(x => x.CustomerOrderCode) : query.OrderBy(x => x.CustomerOrderCode),
            "customer" or "customername" => desc ? query.OrderByDescending(x => x.CustomerName) : query.OrderBy(x => x.CustomerName),
            "status" => desc ? query.OrderByDescending(x => x.Status) : query.OrderBy(x => x.Status),
            "technician" => desc ? query.OrderByDescending(x => x.AssignedTechnicianName) : query.OrderBy(x => x.AssignedTechnicianName),
            _ => desc ? query.OrderByDescending(x => x.RequiredDate).ThenByDescending(x => x.JobCode)
                      : query.OrderBy(x => x.RequiredDate).ThenBy(x => x.JobCode)
        };
    }

    private static string FormatOptical(string description, CustomerOrderLineOpticalSnapshot? snapshot)
    {
        if (snapshot is null) return description;
        var parts = new List<string> { description };
        if (snapshot.SPH.HasValue) parts.Add($"SPH {snapshot.SPH:0.##}");
        if (snapshot.CYL.HasValue) parts.Add($"CYL {snapshot.CYL:0.##}");
        if (snapshot.Axis.HasValue) parts.Add($"Axis {snapshot.Axis}");
        if (snapshot.ADD.HasValue) parts.Add($"ADD {snapshot.ADD:0.##}");
        return string.Join(" / ", parts);
    }

    private static CustomerOrderLineOpticalSnapshotDto MapSnapshot(CustomerOrderLineOpticalSnapshot x) => new(
        x.Id,
        x.CustomerOrderLineId,
        (ContractMeasurementSource)(byte)x.MeasurementSource,
        x.PrescriptionRevisionId,
        (ContractEyeSide)(byte)x.Eye,
        x.SPH,
        x.CYL,
        x.Axis,
        x.ADD,
        x.Prism,
        x.PrismBase.HasValue ? (ContractPrismBase?)(byte)x.PrismBase.Value : null,
        x.PD,
        x.MonocularPD,
        x.VA,
        x.FittingHeight,
        x.LensTypeSnapshot,
        x.MaterialSnapshot,
        x.CoatingSnapshot,
        x.RefractiveIndexSnapshot,
        x.IsActive,
        Convert.ToBase64String(x.RowVersion),
        x.CreatedAtUtc,
        x.CreatedBy,
        x.LastModifiedAtUtc,
        x.LastModifiedBy);

    private sealed class JobRow
    {
        public Guid Id { get; init; }
        public string JobCode { get; init; } = string.Empty;
        public Guid CustomerOrderId { get; init; }
        public Guid? SalesInvoiceId { get; init; }
        public Guid CustomerId { get; init; }
        public DateOnly? RequiredDate { get; init; }
        public OAS.Domain.Sales.Enums.OpticalJobStatus Status { get; init; }
        public Guid? AssignedTechnicianId { get; init; }
        public DateTimeOffset? StartedAtUtc { get; init; }
        public DateTimeOffset? CompletedAtUtc { get; init; }
        public string? Notes { get; init; }
        public byte[] RowVersion { get; init; } = [];
        public string CustomerOrderCode { get; init; } = string.Empty;
        public string CustomerCode { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string? Mobile { get; init; }
        public string? AssignedTechnicianName { get; init; }
    }

    private sealed record JobSummary(string? Frame, string? OD, string? OS);
}
