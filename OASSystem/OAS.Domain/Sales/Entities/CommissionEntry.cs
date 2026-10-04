using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;

namespace OAS.Domain.Sales.Entities;

public sealed class CommissionEntry : AuditableEntity<Guid>
{
    private CommissionEntry() { }

    private CommissionEntry(Guid id, Guid statementId, Guid employeeId, string sourceDocumentType,
        Guid sourceDocumentId, Guid sourceLineId, Guid salesInvoiceId, Guid originalSalesInvoiceLineId,
        Guid? salesReturnId, DateOnly sourceDate, decimal baseSalesAmount, decimal ratePercent,
        Guid commissionRuleId, string ruleCodeSnapshot, string ruleNameSnapshot, bool isReversal)
    {
        if (id == Guid.Empty || statementId == Guid.Empty || employeeId == Guid.Empty || sourceDocumentId == Guid.Empty ||
            sourceLineId == Guid.Empty || salesInvoiceId == Guid.Empty || originalSalesInvoiceLineId == Guid.Empty || commissionRuleId == Guid.Empty)
            throw new DomainException("Commission entry identifiers are required.");
        if (string.IsNullOrWhiteSpace(sourceDocumentType)) throw new DomainException("Commission source document type is required.");
        if (ratePercent < 0m || ratePercent > 100m) throw new DomainException("Commission rate must be between 0 and 100.");
        if ((!isReversal && baseSalesAmount < 0m) || (isReversal && baseSalesAmount > 0m))
            throw new DomainException("Commission sales amount sign does not match reversal state.");

        Id = id;
        CommissionStatementId = statementId;
        EmployeeId = employeeId;
        SourceDocumentType = sourceDocumentType.Trim();
        SourceDocumentId = sourceDocumentId;
        SourceLineId = sourceLineId;
        SalesInvoiceId = salesInvoiceId;
        OriginalSalesInvoiceLineId = originalSalesInvoiceLineId;
        SalesReturnId = salesReturnId;
        SourceDate = sourceDate;
        BaseSalesAmount = decimal.Round(baseSalesAmount, 4);
        RatePercent = decimal.Round(ratePercent, 4);
        CommissionBaseAmount = decimal.Round(BaseSalesAmount * RatePercent / 100m, 4, MidpointRounding.AwayFromZero);
        CommissionRuleId = commissionRuleId;
        RuleCodeSnapshot = Required(ruleCodeSnapshot, 40);
        RuleNameSnapshot = Required(ruleNameSnapshot, 160);
        IsReversal = isReversal;
    }

    public Guid CommissionStatementId { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string SourceDocumentType { get; private set; } = string.Empty;
    public Guid SourceDocumentId { get; private set; }
    public Guid SourceLineId { get; private set; }
    public Guid SalesInvoiceId { get; private set; }
    public Guid OriginalSalesInvoiceLineId { get; private set; }
    public Guid? SalesReturnId { get; private set; }
    public DateOnly SourceDate { get; private set; }
    public decimal BaseSalesAmount { get; private set; }
    public decimal RatePercent { get; private set; }
    public decimal CommissionBaseAmount { get; private set; }
    public Guid CommissionRuleId { get; private set; }
    public string RuleCodeSnapshot { get; private set; } = string.Empty;
    public string RuleNameSnapshot { get; private set; } = string.Empty;
    public bool IsReversal { get; private set; }

    public static CommissionEntry Create(Guid id, Guid statementId, Guid employeeId, string sourceDocumentType,
        Guid sourceDocumentId, Guid sourceLineId, Guid salesInvoiceId, Guid originalSalesInvoiceLineId,
        Guid? salesReturnId, DateOnly sourceDate, decimal baseSalesAmount, decimal ratePercent,
        Guid ruleId, string ruleCode, string ruleName, bool isReversal) =>
        new(id, statementId, employeeId, sourceDocumentType, sourceDocumentId, sourceLineId, salesInvoiceId,
            originalSalesInvoiceLineId, salesReturnId, sourceDate, baseSalesAmount, ratePercent, ruleId, ruleCode, ruleName, isReversal);

    private static string Required(string value, int max)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new DomainException("Commission rule snapshot is required.");
        value = value.Trim();
        if (value.Length > max) throw new DomainException("Commission rule snapshot is too long.");
        return value;
    }
}
