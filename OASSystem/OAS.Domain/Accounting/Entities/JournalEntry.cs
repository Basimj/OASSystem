using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class JournalEntry : Entity<Guid>
{
    private JournalEntry()
    {
    }

    private JournalEntry(
        Guid id,
        string journalNumber,
        JournalType journalType,
        DateOnly postingDate,
        DateOnly documentDate,
        Guid fiscalPeriodId,
        string description,
        string? sourceModule,
        string? sourceDocumentType,
        Guid? sourceDocumentId,
        JournalEntryStatus status,
        Guid createdBy,
        DateTime createdAtUtc)
    {
        Id = id;
        JournalNumber = journalNumber;
        JournalType = journalType;
        PostingDate = postingDate;
        DocumentDate = documentDate;
        FiscalPeriodId = fiscalPeriodId;
        Description = description;
        SourceModule = sourceModule;
        SourceDocumentType = sourceDocumentType;
        SourceDocumentId = sourceDocumentId;
        Status = status;
        CreatedBy = createdBy;
        CreatedAtUtc = createdAtUtc;
    }

    public string JournalNumber { get; private set; } = null!;

    public JournalType JournalType { get; private set; }

    public DateOnly PostingDate { get; private set; }

    public DateOnly DocumentDate { get; private set; }

    public Guid FiscalPeriodId { get; private set; }

    public string Description { get; private set; } = null!;

    public string? SourceModule { get; private set; }

    public string? SourceDocumentType { get; private set; }

    public Guid? SourceDocumentId { get; private set; }

    public JournalEntryStatus Status { get; private set; }

    public Guid? ReversedJournalId { get; private set; }

    public Guid CreatedBy { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? ApprovedBy { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public Guid? PostedBy { get; private set; }

    public DateTime? PostedAtUtc { get; private set; }

    public static JournalEntry Create(
        Guid id,
        string journalNumber,
        JournalType journalType,
        DateOnly postingDate,
        DateOnly documentDate,
        Guid fiscalPeriodId,
        string description,
        string? sourceModule,
        string? sourceDocumentType,
        Guid? sourceDocumentId,
        JournalEntryStatus status,
        Guid createdBy,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Journal entry id is required.", nameof(id));

        if (string.IsNullOrWhiteSpace(journalNumber))
            throw new ArgumentException("Journal number is required.", nameof(journalNumber));

        if (fiscalPeriodId == Guid.Empty)
            throw new ArgumentException("Fiscal period id is required.", nameof(fiscalPeriodId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Journal description is required.", nameof(description));

        if (createdBy == Guid.Empty)
            throw new ArgumentException("Created by is required.", nameof(createdBy));

        return new JournalEntry(
            id,
            journalNumber.Trim(),
            journalType,
            postingDate,
            documentDate,
            fiscalPeriodId,
            description.Trim(),
            Normalize(sourceModule),
            Normalize(sourceDocumentType),
            sourceDocumentId,
            status,
            createdBy,
            createdAtUtc);
    }

    public void Approve(Guid approvedBy, DateTime approvedAtUtc)
    {
        EnsureNotPosted();

        if (approvedBy == Guid.Empty)
            throw new ArgumentException("Approved by is required.", nameof(approvedBy));

        if (Status != JournalEntryStatus.PendingApproval)
            throw new InvalidOperationException("Only pending journals can be approved.");

        Status = JournalEntryStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = approvedAtUtc;
    }

    public void Post(Guid postedBy, DateTime postedAtUtc)
    {
        if (postedBy == Guid.Empty)
            throw new ArgumentException("Posted by is required.", nameof(postedBy));

        if (Status != JournalEntryStatus.Approved)
            throw new InvalidOperationException("Only approved journals can be posted.");

        Status = JournalEntryStatus.Posted;
        PostedBy = postedBy;
        PostedAtUtc = postedAtUtc;
    }

    public void MarkReversed(Guid reversedJournalId)
    {
        if (reversedJournalId == Guid.Empty)
            throw new ArgumentException(
                "Reversed journal id is required.",
                nameof(reversedJournalId));

        if (Status != JournalEntryStatus.Posted)
            throw new InvalidOperationException(
                "Only posted journals can be reversed.");

        ReversedJournalId = reversedJournalId;
        Status = JournalEntryStatus.Reversed;
    }

    public void UpdateDraft(
        DateOnly postingDate,
        DateOnly documentDate,
        Guid fiscalPeriodId,
        string description)
    {
        EnsureEditable();

        if (fiscalPeriodId == Guid.Empty)
            throw new ArgumentException("Fiscal period id is required.", nameof(fiscalPeriodId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Journal description is required.", nameof(description));

        PostingDate = postingDate;
        DocumentDate = documentDate;
        FiscalPeriodId = fiscalPeriodId;
        Description = description.Trim();
    }

    public void SetPendingApproval()
    {
        if (Status != JournalEntryStatus.Draft)
            throw new InvalidOperationException(
                "Only draft journals can be submitted for approval.");

        Status = JournalEntryStatus.PendingApproval;
    }

    public void EnsureEditable()
    {
        if (Status == JournalEntryStatus.Posted ||
            Status == JournalEntryStatus.Reversed)
        {
            throw new InvalidOperationException(
                "Posted or reversed journal entries cannot be modified.");
        }
    }

    private void EnsureNotPosted()
    {
        if (Status == JournalEntryStatus.Posted ||
            Status == JournalEntryStatus.Reversed)
        {
            throw new InvalidOperationException(
                "Posted or reversed journal entries cannot be modified.");
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}