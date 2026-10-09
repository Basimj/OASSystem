using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class OpticalQualityCheckItem : AuditableEntity<Guid>
{
    private OpticalQualityCheckItem() { }
    private OpticalQualityCheckItem(Guid id, Guid qualityCheckId, string checkCode, string checkName, int sequence)
    {
        Id = SalesDomainGuard.Required(id, "QC item id");
        QualityCheckId = SalesDomainGuard.Required(qualityCheckId, "Quality check id");
        CheckCode = SalesDomainGuard.Required(checkCode, 50, "QC check code");
        CheckName = SalesDomainGuard.Required(checkName, 200, "QC check name");
        if (sequence <= 0) throw new DomainException("QC item sequence must be greater than zero.");
        Sequence = sequence;
        Result = OpticalQualityCheckItemResult.NotChecked;
    }

    public Guid QualityCheckId { get; private set; }
    public string CheckCode { get; private set; } = string.Empty;
    public string CheckName { get; private set; } = string.Empty;
    public OpticalQualityCheckItemResult Result { get; private set; }
    public string? Notes { get; private set; }
    public int Sequence { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static OpticalQualityCheckItem Create(Guid id, Guid qualityCheckId, string checkCode, string checkName, int sequence) =>
        new(id, qualityCheckId, checkCode, checkName, sequence);

    public void SetResult(OpticalQualityCheckItemResult result, string? notes)
    {
        SalesDomainGuard.Defined(result, "QC item result");
        if (result == OpticalQualityCheckItemResult.NotChecked)
            throw new DomainException("QC item must be evaluated before completion.");
        Result = result;
        Notes = SalesDomainGuard.Optional(notes, 1000, "QC item notes");
    }
}
