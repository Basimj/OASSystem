using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Abstractions.Security;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Commissions;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
using DomainBasisKind = OAS.Domain.Sales.Commissions.CommissionBasisKind;
using DomainCommissionStatementStatus = OAS.Domain.Sales.Commissions.CommissionStatementStatus;
using DomainSalesLineType = OAS.Domain.Sales.Enums.SalesLineType;

namespace OAS.Application.Sales.Commissions.Commands;

public sealed record CreateCommissionRuleCommand(CreateCommissionRuleRequest Data) : ICommand<CommissionRuleDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Edit]; }
public sealed record CalculateCommissionStatementCommand(CalculateCommissionStatementRequest Data) : ICommand<CommissionStatementDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Post]; }
public sealed record ReviewCommissionStatementCommand(Guid Id, CommissionStatementActionRequest Data) : ICommand<CommissionStatementDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Post]; }
public sealed record ApproveCommissionStatementCommand(Guid Id, CommissionStatementActionRequest Data) : ICommand<CommissionStatementDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Post]; }
public sealed record SettleCommissionStatementCommand(Guid Id, CommissionStatementActionRequest Data) : ICommand<CommissionStatementDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Post]; }
public sealed record FinalizeCommissionStatementCommand(Guid Id, CommissionStatementActionRequest Data) : ICommand<CommissionStatementDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Post]; }

public sealed class CreateCommissionRuleCommandHandler(
    IRepository<CommissionRule, Guid> rules,
    IReadRepository<Employee, Guid> employees,
    IReadRepository<ProductVariant, Guid> variants,
    IReadRepository<ProductCategory, Guid> categories,
    ICommissionQueryService queries,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CreateCommissionRuleCommand, CommissionRuleDto>
{
    public async Task<CommissionRuleDto> Handle(CreateCommissionRuleCommand request, CancellationToken ct)
    {
        var d = request.Data;
        if (d.EmployeeId.HasValue)
        {
            var employee = await employees.GetByIdAsync(d.EmployeeId.Value, ct) ?? throw new NotFoundException(nameof(Employee), d.EmployeeId.Value);
            if (!employee.IsActive) throw new ConflictException("commission_employee_inactive", "الموظف غير فعال.");
        }
        if (d.ProductVariantId.HasValue && await variants.GetByIdAsync(d.ProductVariantId.Value, ct) is null)
            throw new NotFoundException(nameof(ProductVariant), d.ProductVariantId.Value);
        if (d.ProductCategoryId.HasValue && await categories.GetByIdAsync(d.ProductCategoryId.Value, ct) is null)
            throw new NotFoundException(nameof(ProductCategory), d.ProductCategoryId.Value);
        if (await rules.CountAsync(new Specification<CommissionRule>().Where(x => x.Code == d.Code.Trim()), ct) > 0)
            throw new ConflictException("commission_rule_code_duplicate", "كود قاعدة العمولة مستخدم مسبقًا.");

        var saleType = d.SaleType.HasValue ? (DomainSalesLineType?)(byte)d.SaleType.Value : null;
        var rule = CommissionRule.Create(Guid.NewGuid(), d.Code, d.Name, d.EmployeeId, d.ProductVariantId,
            d.ProductCategoryId, saleType, d.Priority, (DomainBasisKind)(byte)d.BasisKind,
            d.RatePercent, d.EffectiveFrom, d.EffectiveTo);
        await rules.AddAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        var list = await queries.GetRulesAsync(d.EmployeeId, ct);
        return list.First(x => x.Id == rule.Id);
    }
}

public sealed class CalculateCommissionStatementCommandHandler(
    ICommissionStatementRepository statements,
    IRepository<CommissionEntry, Guid> entries,
    IReadRepository<CommissionRule, Guid> rules,
    IReadRepository<Employee, Guid> employees,
    ICommissionSourceQueryService sources,
    ICommissionQueryService queries,
    ISequenceNumberGenerator sequences,
    TimeProvider timeProvider,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
    : IRequestHandler<CalculateCommissionStatementCommand, CommissionStatementDto>
{
    public async Task<CommissionStatementDto> Handle(CalculateCommissionStatementCommand request, CancellationToken ct)
    {
        var d = request.Data;
        if (d.ToDate < d.FromDate) throw new ConflictException("commission_period_invalid", "نهاية فترة العمولة يجب ألا تسبق بدايتها.");
        var employee = await employees.GetByIdAsync(d.EmployeeId, ct) ?? throw new NotFoundException(nameof(Employee), d.EmployeeId);
        if (!employee.IsActive || !employee.IsCommissionEligible)
            throw new ConflictException("commission_employee_not_eligible", "الموظف غير فعال أو غير مؤهل للعمولة.");

        var overlap = await statements.CountAsync(new Specification<CommissionStatement>().Where(x =>
            x.EmployeeId == d.EmployeeId && x.IsActive && x.Status != DomainCommissionStatementStatus.Cancelled &&
            x.FromDate <= d.ToDate && x.ToDate >= d.FromDate), ct);
        if (overlap > 0) throw new ConflictException("commission_statement_overlap", "توجد دورة عمولة متداخلة لهذا الموظف.");

        var seq = await sequences.NextAsync("CommissionStatementCodeSequence", ct);
        var statement = CommissionStatement.Create(Guid.NewGuid(), $"COM-{d.ToDate.Year:D4}-{seq:D6}", d.EmployeeId, d.FromDate, d.ToDate);
        var sourceLines = await sources.GetSourcesAsync(d.EmployeeId, d.FromDate, d.ToDate, ct);
        var allRules = await rules.ListAsync(new Specification<CommissionRule>().Where(x =>
            x.IsActive && (!x.EmployeeId.HasValue || x.EmployeeId == d.EmployeeId)), ct);

        foreach (var source in sourceLines)
        {
            if (await entries.CountAsync(new Specification<CommissionEntry>().Where(x =>
                    x.SourceDocumentType == source.SourceDocumentType && x.SourceLineId == source.SourceLineId), ct) > 0)
                continue;

            if (source.IsReversal)
            {
                var original = (await entries.ListAsync(new Specification<CommissionEntry>().Where(x =>
                        !x.IsReversal && x.OriginalSalesInvoiceLineId == source.OriginalSalesInvoiceLineId), ct))
                    .OrderBy(x => x.SourceDate).FirstOrDefault();
                if (original is not null)
                {
                    var reversalAmount = -decimal.Abs(source.AmountFor(original.BasisKindSnapshot));
                    statement.AddEntry(CommissionEntry.Create(Guid.NewGuid(), statement.Id, d.EmployeeId,
                        source.SourceDocumentType, source.SourceDocumentId, source.SourceLineId, source.SalesInvoiceId,
                        source.OriginalSalesInvoiceLineId, source.SalesReturnId, source.SourceDate, reversalAmount,
                        original.RatePercent, original.CommissionRuleId, original.RuleCodeSnapshot, original.RuleNameSnapshot,
                        original.BasisKindSnapshot, true));
                    continue;
                }
            }

            var ruleDate = source.IsReversal ? source.OriginalSalesDate : source.SourceDate;
            var rule = ResolveRule(allRules, d.EmployeeId, source, ruleDate);
            var amount = decimal.Abs(source.AmountFor(rule.BasisKind));
            var signedAmount = source.IsReversal ? -amount : amount;
            statement.AddEntry(CommissionEntry.Create(Guid.NewGuid(), statement.Id, d.EmployeeId,
                source.SourceDocumentType, source.SourceDocumentId, source.SourceLineId, source.SalesInvoiceId,
                source.OriginalSalesInvoiceLineId, source.SalesReturnId, source.SourceDate, signedAmount,
                rule.RatePercent, rule.Id, rule.Code, rule.Name, rule.BasisKind, source.IsReversal));
        }

        statement.MarkCalculated(timeProvider.GetUtcNow(), currentUser.UserId);
        await statements.AddAsync(statement, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await queries.GetStatementAsync(statement.Id, ct) ?? Map(statement, employee.DisplayName);
    }

    private static CommissionRule ResolveRule(IReadOnlyList<CommissionRule> rules, Guid employeeId, CommissionSourceLine source, DateOnly date)
    {
        var applicable = rules
            .Where(x => x.AppliesTo(employeeId, source.ProductVariantId, source.ProductCategoryId, source.SaleType, date))
            .OrderByDescending(x => x.Priority)
            .ThenByDescending(x => x.Specificity)
            .ThenByDescending(x => x.EffectiveFrom)
            .ThenBy(x => x.Code, StringComparer.Ordinal)
            .ToList();
        if (applicable.Count == 0)
            throw new ConflictException("commission_rule_missing", $"لا توجد قاعدة عمولة صالحة بتاريخ {date:yyyy-MM-dd} للسطر المحدد.");
        var best = applicable[0];
        if (applicable.Count > 1 && applicable[1].Priority == best.Priority &&
            applicable[1].Specificity == best.Specificity && applicable[1].EffectiveFrom == best.EffectiveFrom)
            throw new ConflictException("commission_rule_ambiguous", $"توجد أكثر من قاعدة عمولة بنفس الأولوية والتخصص بتاريخ {date:yyyy-MM-dd}.");
        return best;
    }

    private static CommissionStatementDto Map(CommissionStatement x, string? employeeName) =>
        new(x.Id, x.StatementCode, x.EmployeeId, employeeName, x.FromDate, x.ToDate,
            (CommissionStatementStatus)(byte)x.Status, x.SalesBaseAmount, x.ReturnsBaseAmount, x.CommissionBaseAmount,
            x.CalculatedAtUtc, x.FinalizedAtUtc, Convert.ToBase64String(x.RowVersion),
            x.Entries.Select(e => new CommissionEntryDto(e.Id, e.SourceDocumentType, e.SourceDocumentId, e.SourceLineId,
                e.SalesInvoiceId, e.OriginalSalesInvoiceLineId, e.SalesReturnId, e.SourceDate, e.BaseSalesAmount,
                e.RatePercent, e.CommissionBaseAmount, e.CommissionRuleId, e.RuleCodeSnapshot, e.RuleNameSnapshot,
                e.IsReversal, (OAS.Contracts.Sales.Commissions.CommissionBasisKind)(byte)e.BasisKindSnapshot)).ToList(),
            x.ReviewedAtUtc, x.ApprovedAtUtc, x.SettledAtUtc);
}

public abstract class CommissionStatementActionHandlerBase(
    ICommissionStatementRepository statements,
    ICommissionQueryService queries,
    TimeProvider timeProvider,
    ICurrentUser currentUser,
    IUnitOfWork unitOfWork)
{
    protected async Task<CommissionStatementDto> Execute(Guid id, CommissionStatementActionRequest data,
        Action<CommissionStatement, DateTimeOffset, string?> transition, CancellationToken ct)
    {
        var statement = await statements.GetAggregateAsync(id, true, ct) ?? throw new NotFoundException(nameof(CommissionStatement), id);
        SalesConcurrency.Ensure(data.RowVersion, statement.RowVersion, "دورة العمولة");
        transition(statement, timeProvider.GetUtcNow(), currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);
        return await queries.GetStatementAsync(statement.Id, ct) ?? throw new NotFoundException(nameof(CommissionStatement), statement.Id);
    }
}

public sealed class ReviewCommissionStatementCommandHandler(
    ICommissionStatementRepository s, ICommissionQueryService q, TimeProvider t, ICurrentUser u, IUnitOfWork w)
    : CommissionStatementActionHandlerBase(s, q, t, u, w), IRequestHandler<ReviewCommissionStatementCommand, CommissionStatementDto>
{ public Task<CommissionStatementDto> Handle(ReviewCommissionStatementCommand r, CancellationToken ct) => Execute(r.Id, r.Data, (x,a,b)=>x.Review(a,b), ct); }

public sealed class ApproveCommissionStatementCommandHandler(
    ICommissionStatementRepository s, ICommissionQueryService q, TimeProvider t, ICurrentUser u, IUnitOfWork w)
    : CommissionStatementActionHandlerBase(s, q, t, u, w), IRequestHandler<ApproveCommissionStatementCommand, CommissionStatementDto>
{ public Task<CommissionStatementDto> Handle(ApproveCommissionStatementCommand r, CancellationToken ct) => Execute(r.Id, r.Data, (x,a,b)=>x.Approve(a,b), ct); }

public sealed class SettleCommissionStatementCommandHandler(
    ICommissionStatementRepository s, ICommissionQueryService q, TimeProvider t, ICurrentUser u, IUnitOfWork w)
    : CommissionStatementActionHandlerBase(s, q, t, u, w), IRequestHandler<SettleCommissionStatementCommand, CommissionStatementDto>
{ public Task<CommissionStatementDto> Handle(SettleCommissionStatementCommand r, CancellationToken ct) => Execute(r.Id, r.Data, (x,a,b)=>x.Settle(a,b), ct); }

public sealed class FinalizeCommissionStatementCommandHandler(
    ICommissionStatementRepository s, ICommissionQueryService q, TimeProvider t, ICurrentUser u, IUnitOfWork w)
    : CommissionStatementActionHandlerBase(s, q, t, u, w), IRequestHandler<FinalizeCommissionStatementCommand, CommissionStatementDto>
{ public Task<CommissionStatementDto> Handle(FinalizeCommissionStatementCommand r, CancellationToken ct) => Execute(r.Id, r.Data, (x,a,b)=>x.Finalize(a,b), ct); }
