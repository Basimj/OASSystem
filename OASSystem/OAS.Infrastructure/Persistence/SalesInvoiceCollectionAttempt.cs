namespace OAS.Infrastructure.Persistence;

/// <summary>Durable record of an invoice collection intent and its posted receipt.</summary>
public sealed class SalesInvoiceCollectionAttempt
{
    public Guid Id { get; set; }
    public Guid InvoiceId { get; set; }
    public string Fingerprint { get; set; } = string.Empty;
    public string ResultJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
