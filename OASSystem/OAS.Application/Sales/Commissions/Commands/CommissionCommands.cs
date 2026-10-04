using MediatR;
using OAS.Application.Abstractions.Messaging;
using OAS.Application.Abstractions.Numbering;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Abstractions.Security;
using OAS.Application.Sales.Abstractions;
using OAS.Application.Sales.Authorization;
using OAS.Application.Sales.Common;
using OAS.Contracts.Sales.Commissions;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;
using OAS.Domain.Sales.Commissions;
using DomainCommissionStatementStatus = OAS.Domain.Sales.Commissions.CommissionStatementStatus;

namespace OAS.Application.Sales.Commissions.Commands;

public sealed record CreateCommissionRuleCommand(CreateCommissionRuleRequest Data) : ICommand<CommissionRuleDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Edit]; }
public sealed record CalculateCommissionStatementCommand(CalculateCommissionStatementRequest Data) : ICommand<CommissionStatementDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Post]; }
public sealed record FinalizeCommissionStatementCommand(Guid Id, CommissionStatementActionRequest Data) : ICommand<CommissionStatementDto>, IAuthorizedRequest
{ public IReadOnlyCollection<string> RequiredPermissions { get; } = [SalesPermissions.Post]; }

public sealed class CreateCommissionRuleCommandHandler(
    IRepository<CommissionRule, Guid> rules, IReadRepository<Employee, Guid> employees, ICommissionQueryService queries, IUnitOfWork unitOfWork)
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
        if (await rules.CountAsync(new Specification<CommissionRule>().Where(x => x.Code == d.Code.Trim()), ct) > 0)
            throw new ConflictException("commission_rule_code_duplicate", "كود قاعدة العمولة مستخدم مسبقًا.");
        var rule = CommissionRule.Create(Guid.NewGuid(), d.Code, d.Name, d.EmployeeId, d.RatePercent, d.EffectiveFrom, d.EffectiveTo);
        await rules.AddAsync(rule, ct);
        await unitOfWork.SaveChangesAsync(ct);
        var list = await queries.GetRulesAsync(d.EmployeeId, ct);
        return list.FirstOrDefault(x => x.Id == rule.Id) ?? new CommissionRuleDto(rule.Id, rule.Code, rule.Name, rule.EmployeeId, null, rule.RatePercent, rule.EffectiveFrom, rule.EffectiveTo, rule.IsActive, Convert.ToBase64String(rule.RowVersion));
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

        var overlap = await statements.CountAsync(new Specification<CommissionStatement>().Where(x => x.EmployeeId == d.EmployeeId && x.IsActive && x.Status != DomainCommissionStatementStatus.Cancelled && x.FromDate <= d.ToDate && x.ToDate >= d.FromDate), ct);
        if (overlap > 0) throw new ConflictException("commission_statement_overlap", "توجد دورة عمولة متداخلة لهذا الموظف.");

        var seq = await sequences.NextAsync("CommissionStatementCodeSequence", ct);
        var code = $"COM-{d.ToDate.Year:D4}-{seq:D6}";
        var statement = CommissionStatement.Create(Guid.NewGuid(), code, d.EmployeeId, d.FromDate, d.ToDate);
        var sourceLines = await sources.GetSourcesAsync(d.EmployeeId, d.FromDate, d.ToDate, ct);
        var allRules = await rules.ListAsync(new Specification<CommissionRule>().Where(x => x.IsActive && (!x.EmployeeId.HasValue || x.EmployeeId == d.EmployeeId)), ct);

        foreach (var source in sourceLines)
        {
            if (await entries.CountAsync(new Specification<CommissionEntry>().Where(x => x.SourceDocumentType == source.SourceDocumentType && x.SourceLineId == source.SourceLineId), ct) > 0)
                continue;

            CommissionRule? rule = null;
            decimal rate;
            if (source.IsReversal)
            {
                var original = (await entries.ListAsync(new Specification<CommissionEntry>().Where(x => !x.IsReversal && x.OriginalSalesInvoiceLineId == source.OriginalSalesInvoiceLineId), ct))
                    .OrderBy(x => x.SourceDate).FirstOrDefault();
                if (original is not null)
                {
                    rule = await rules.GetByIdAsync(original.CommissionRuleId, ct);
                    rate = original.RatePercent;
                    var entry = CommissionEntry.Create(Guid.NewGuid(), statement.Id, d.EmployeeId, source.SourceDocumentType,
                        source.SourceDocumentId, source.SourceLineId, source.SalesInvoiceId, source.OriginalSalesInvoiceLineId,
                        source.SalesReturnId, source.SourceDate, -decimal.Abs(source.BaseNetAmount), rate,
                        original.CommissionRuleId, original.RuleCodeSnapshot, original.RuleNameSnapshot, true);
                    statement.AddEntry(entry);
                    continue;
                }
            }

            var ruleDate = source.IsReversal ? source.OriginalSalesDate : source.SourceDate;
            rule = ResolveRule(allRules, d.EmployeeId, ruleDate);
            rate = rule.RatePercent;
            var signedAmount = source.IsReversal ? -decimal.Abs(source.BaseNetAmount) : decimal.Abs(source.BaseNetAmount);
            statement.AddEntry(CommissionEntry.Create(Guid.NewGuid(), statement.Id, d.EmployeeId, source.SourceDocumentType,
                source.SourceDocumentId, source.SourceLineId, source.SalesInvoiceId, source.OriginalSalesInvoiceLineId,
                source.SalesReturnId, source.SourceDate, signedAmount, rate, rule.Id, rule.Code, rule.Name, source.IsReversal));
        }

        statement.MarkCalculated(timeProvider.GetUtcNow(), currentUser.UserId);
        await statements.AddAsync(statement, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return await queries.GetStatementAsync(statement.Id, ct) ?? Map(statement, employee.DisplayName);
    }

    private static CommissionRule ResolveRule(IReadOnlyList<CommissionRule> rules, Guid employeeId, DateOnly date)
    {
        var applicable = rules.Where(x => x.AppliesTo(employeeId, date)).OrderByDescending(x => x.EmployeeId.HasValue).ThenByDescending(x => x.EffectiveFrom).ToList();
        if (applicable.Count == 0) throw new ConflictException("commission_rule_missing", $"لا توجد قاعدة عمولة صالحة بتاريخ {date:yyyy-MM-dd}.");
        var best = applicable[0];
        if (applicable.Count > 1 && applicable[1].EmployeeId.HasValue == best.EmployeeId.HasValue && applicable[1].EffectiveFrom == best.EffectiveFrom)
            throw new ConflictException("commission_rule_ambiguous", $"توجد أكثر من قاعدة عمولة بنفس الأولوية بتاريخ {date:yyyy-MM-dd}.");
        return best;
    }

    private static CommissionStatementDto Map(CommissionStatement x, string? employeeName) => new(x.Id, x.StatementCode, x.EmployeeId, employeeName, x.FromDate, x.ToDate,
        (OAS.Contracts.Sales.Commissions.CommissionStatementStatus)(byte)x.Status, x.SalesBaseAmount, x.ReturnsBaseAmount, x.CommissionBaseAmount,
        x.CalculatedAtUtc, x.FinalizedAtUtc, Convert.ToBase64String(x.RowVersion), x.Entries.Select(e => new CommissionEntryDto(e.Id,e.SourceDocumentType,e.SourceDocumentId,e.SourceLineId,e.SalesInvoiceId,e.OriginalSalesInvoiceLineId,e.SalesReturnId,e.SourceDate,e.BaseSalesAmount,e.RatePercent,e.CommissionBaseAmount,e.CommissionRuleId,e.RuleCodeSnapshot,e.RuleNameSnapshot,e.IsReversal)).ToList());
}

public sealed class FinalizeCommissionStatementCommandHandler(ICommissionStatementRepository statements, ICommissionQueryService queries, TimeProvider timeProvider, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    : IRequestHandler<FinalizeCommissionStatementCommand, CommissionStatementDto>
{
    public async Task<CommissionStatementDto> Handle(FinalizeCommissionStatementCommand request, CancellationToken ct)
    {
        var statement = await statements.GetAggregateAsync(request.Id, true, ct) ?? throw new NotFoundException(nameof(CommissionStatement), request.Id);
        SalesConcurrency.Ensure(request.Data.RowVersion, statement.RowVersion, "دورة العمولة");
        statement.Finalize(timeProvider.GetUtcNow(), currentUser.UserId);
        await unitOfWork.SaveChangesAsync(ct);
        return await queries.GetStatementAsync(statement.Id, ct) ?? throw new NotFoundException(nameof(CommissionStatement), statement.Id);
    }
}
