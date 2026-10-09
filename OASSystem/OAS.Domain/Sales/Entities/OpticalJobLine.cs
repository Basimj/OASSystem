using OAS.Domain.Common.Entities;
using OAS.Domain.Exceptions;
using OAS.Domain.Sales.Enums;
using OAS.Domain.Sales.Rules;

namespace OAS.Domain.Sales.Entities;

public sealed class OpticalJobLine : AuditableEntity<Guid>
{
    private OpticalJobLine() { }

    private OpticalJobLine(
        Guid id,
        Guid opticalJobId,
        Guid customerOrderLineId,
        Guid? productVariantId,
        int lineNumber,
        OpticalJobLineType lineType,
        EyeSide? eye,
        Guid? groupId,
        string descriptionSnapshot,
        decimal quantity,
        bool requiresProduction,
        string? notes)
    {
        Id = SalesDomainGuard.Required(id, "Optical job line id");
        OpticalJobId = SalesDomainGuard.Required(opticalJobId, "Optical job id");
        CustomerOrderLineId = SalesDomainGuard.Required(customerOrderLineId, "Customer order line id");
        if (productVariantId == Guid.Empty)
            throw new DomainException("Product variant id cannot be empty.");
        if (lineNumber <= 0)
            throw new DomainException("Optical job line number must be greater than zero.");
        if (quantity <= 0)
            throw new DomainException("Optical job line quantity must be greater than zero.");

        SalesDomainGuard.Defined(lineType, "Sales line type");
        if (eye.HasValue)
            SalesDomainGuard.Defined(eye.Value, "Eye side");

        ProductVariantId = productVariantId;
        LineNumber = lineNumber;
        LineType = lineType;
        Eye = eye;
        GroupId = groupId == Guid.Empty ? null : groupId;
        DescriptionSnapshot = SalesDomainGuard.Required(descriptionSnapshot, 500, "Optical job line description");
        Quantity = decimal.Round(quantity, 3);
        RequiresProduction = requiresProduction;
        Notes = SalesDomainGuard.Optional(notes, 500, "Optical job line notes");
    }

    public Guid OpticalJobId { get; private set; }
    public Guid CustomerOrderLineId { get; private set; }
    public Guid? ProductVariantId { get; private set; }
    public int LineNumber { get; private set; }
    public OpticalJobLineType LineType { get; private set; }
    public EyeSide? Eye { get; private set; }
    public Guid? GroupId { get; private set; }
    public string DescriptionSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public bool RequiresProduction { get; private set; }
    public string? Notes { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static OpticalJobLine Create(
        Guid id,
        Guid opticalJobId,
        Guid customerOrderLineId,
        Guid? productVariantId,
        int lineNumber,
        OpticalJobLineType lineType,
        EyeSide? eye,
        Guid? groupId,
        string descriptionSnapshot,
        decimal quantity,
        bool requiresProduction,
        string? notes = null) =>
        new(
            id,
            opticalJobId,
            customerOrderLineId,
            productVariantId,
            lineNumber,
            lineType,
            eye,
            groupId,
            descriptionSnapshot,
            quantity,
            requiresProduction,
            notes);
}
