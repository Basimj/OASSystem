using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public enum CashTransferStatus : byte { Draft = 1, Approved = 2, Posted = 3, Reversed = 4, Cancelled = 5 }

public sealed class CashTransfer : AuditableEntity<Guid>
{
    private CashTransfer() { }
    public string TransferNumber { get; private set; } = null!;
    public DateOnly TransferDate { get; private set; }
    public Guid SourceAccountId { get; private set; }
    public Guid DestinationAccountId { get; private set; }
    public Guid SourceCurrencyId { get; private set; }
    public Guid DestinationCurrencyId { get; private set; }
    public string SourceName { get; private set; } = null!;
    public string DestinationName { get; private set; } = null!;
    public string SourceCurrencyCode { get; private set; } = null!;
    public string DestinationCurrencyCode { get; private set; } = null!;
    public decimal SourceAmount { get; private set; }
    public decimal DestinationAmount { get; private set; }
    public decimal FeeAmount { get; private set; }
    public Guid? FeeAccountId { get; private set; }
    public string? Reference { get; private set; }
    public string Description { get; private set; } = null!;
    public CashTransferStatus Status { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTime? ApprovedAtUtc { get; private set; }
    public Guid? JournalEntryId { get; private set; }
    public Guid? ReversalJournalEntryId { get; private set; }
    public DateOnly? ReversalDate { get; private set; }
    public string? ReversalReason { get; private set; }
    public decimal? SourceBaseAmount { get; private set; }
    public decimal? DestinationBaseAmount { get; private set; }
    public decimal? FeeBaseAmount { get; private set; }
    public decimal? FxDifferenceBase { get; private set; }
    public byte[] RowVersion { get; private set; } = [];

    public static CashTransfer Create(Guid id, string number)
        => new() { Id = id, TransferNumber = number, Status = CashTransferStatus.Draft };

    public void SetDetails(DateOnly date, Guid source, Guid destination, Guid sourceCurrency, Guid destinationCurrency,
        string sourceName, string destinationName, string sourceCode, string destinationCode,
        decimal sourceAmount, decimal destinationAmount, decimal fee, Guid? feeAccount, string? reference, string description)
    {
        if (Status != CashTransferStatus.Draft) throw new InvalidOperationException("Only draft transfers can be edited.");
        if (source == destination || sourceAmount <= 0 || destinationAmount <= 0 || fee < 0 || fee >= sourceAmount ||
            (fee > 0) != feeAccount.HasValue || (sourceCurrency == destinationCurrency && sourceAmount != destinationAmount + fee))
            throw new ArgumentException("Invalid transfer amounts or accounts.");
        TransferDate = date; SourceAccountId = source; DestinationAccountId = destination;
        SourceCurrencyId = sourceCurrency; DestinationCurrencyId = destinationCurrency;
        SourceName = sourceName; DestinationName = destinationName;
        SourceCurrencyCode = sourceCode; DestinationCurrencyCode = destinationCode;
        SourceAmount = sourceAmount; DestinationAmount = destinationAmount; FeeAmount = fee;
        FeeAccountId = feeAccount; Reference = reference; Description = description;
    }
    public void Approve(Guid user, DateTime now)
    {
        if (Status != CashTransferStatus.Draft) throw new InvalidOperationException("Only draft transfers can be approved.");
        ApprovedBy = user; ApprovedAtUtc = now; Status = CashTransferStatus.Approved;
    }
    public void Post(Guid journal, decimal source, decimal destination, decimal fee, decimal difference)
    {
        if (Status != CashTransferStatus.Approved || journal == Guid.Empty || source <= 0 || destination <= 0 || fee < 0 || difference != destination + fee - source)
            throw new InvalidOperationException("Invalid transfer posting.");
        JournalEntryId = journal; SourceBaseAmount = source; DestinationBaseAmount = destination;
        FeeBaseAmount = fee; FxDifferenceBase = difference; Status = CashTransferStatus.Posted;
    }
    public void Reverse(Guid journal, DateOnly date, string reason)
    {
        if (Status != CashTransferStatus.Posted || journal == Guid.Empty || date < TransferDate || string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("Invalid transfer reversal.");
        ReversalJournalEntryId = journal; ReversalDate = date; ReversalReason = reason; Status = CashTransferStatus.Reversed;
    }
    public void Cancel()
    {
        if (Status is not (CashTransferStatus.Draft or CashTransferStatus.Approved)) throw new InvalidOperationException("Only unposted transfers can be cancelled.");
        Status = CashTransferStatus.Cancelled;
    }
}
