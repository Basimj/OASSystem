
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Common.Entities;

namespace OAS.Domain.Accounting.Entities;

public sealed class JournalEntry : AuditableEntity<Guid>
{
    private readonly List<JournalEntryLine> _lines = [];

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
        JournalEntryStatus status)
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


    public Guid? ApprovedBy { get; private set; }

    public DateTime? ApprovedAtUtc { get; private set; }

    public Guid? PostedBy { get; private set; }

    public DateTime? PostedAtUtc { get; private set; }

    public IReadOnlyCollection<JournalEntryLine> Lines =>
        _lines.AsReadOnly();
    public byte[] RowVersion { get; private set; } = [];

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
        JournalEntryStatus status)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Journal entry id is required.",
                nameof(id));

        if (string.IsNullOrWhiteSpace(journalNumber))
            throw new ArgumentException(
                "Journal number is required.",
                nameof(journalNumber));

        if (fiscalPeriodId == Guid.Empty)
            throw new ArgumentException(
                "Fiscal period id is required.",
                nameof(fiscalPeriodId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "Journal description is required.",
                nameof(description));

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
            status);
    }

    public void AddLine(JournalEntryLine line)
    {
        ArgumentNullException.ThrowIfNull(line);

        EnsureEditable();

        if (line.JournalEntryId != Id)
        {
            throw new InvalidOperationException(
                "Journal line does not belong to this journal entry.");
        }

        if (_lines.Any(x => x.Id == line.Id))
        {
            throw new InvalidOperationException(
                "Journal line already exists.");
        }

        _lines.Add(line);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureEditable();

        var line = _lines.FirstOrDefault(
            x => x.Id == lineId);

        if (line is null)
            throw new KeyNotFoundException(
                "Journal line was not found.");

        _lines.Remove(line);

        ReNumberLines();
    }

    public void ClearLines()
    {
        EnsureEditable();

        _lines.Clear();
    }

    public void ReplaceLines(
        IEnumerable<JournalEntryLine> lines)
    {
        EnsureEditable();

        ArgumentNullException.ThrowIfNull(lines);

        var newLines = lines.ToList();

        if (newLines.Any(x => x.JournalEntryId != Id))
        {
            throw new InvalidOperationException(
                "All journal lines must belong to this journal entry.");
        }

        if (newLines
            .GroupBy(x => x.Id)
            .Any(group => group.Count() > 1))
        {
            throw new InvalidOperationException(
                "Duplicate journal lines are not allowed.");
        }

        _lines.Clear();
        _lines.AddRange(newLines);

        ReNumberLines();
    }

    public decimal TotalDebit()
    {
        return _lines.Sum(x => x.DebitAmount);
    }

    public decimal TotalCredit()
    {
        return _lines.Sum(x => x.CreditAmount);
    }

    public bool IsBalanced()
    {
        return TotalDebit() == TotalCredit();
    }

    public void UpdateDraft(
        JournalType journalType,
        DateOnly postingDate,
        DateOnly documentDate,
        Guid fiscalPeriodId,
        string description,
        string? sourceModule,
        string? sourceDocumentType,
        Guid? sourceDocumentId)
    {
        EnsureEditable();

        if (Status != JournalEntryStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only draft journal entries can be updated.");
        }

        if (fiscalPeriodId == Guid.Empty)
            throw new ArgumentException(
                "Fiscal period id is required.",
                nameof(fiscalPeriodId));

        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException(
                "Journal description is required.",
                nameof(description));

        JournalType = journalType;
        PostingDate = postingDate;
        DocumentDate = documentDate;
        FiscalPeriodId = fiscalPeriodId;
        Description = description.Trim();
        SourceModule = Normalize(sourceModule);
        SourceDocumentType = Normalize(sourceDocumentType);
        SourceDocumentId = sourceDocumentId;
    }

    public void SetPendingApproval()
    {
        if (Status != JournalEntryStatus.Draft)
        {
            throw new InvalidOperationException(
                "Only draft journals can be submitted for approval.");
        }

        if (_lines.Count == 0)
        {
            throw new InvalidOperationException(
                "A journal entry must contain at least one line.");
        }

        if (!IsBalanced())
        {
            throw new InvalidOperationException(
                "Journal entry debit and credit totals must be equal.");
        }

        Status = JournalEntryStatus.PendingApproval;
    }

    public void Approve(
        Guid approvedBy,
        DateTime approvedAtUtc)
    {
        EnsureNotPosted();

        if (approvedBy == Guid.Empty)
            throw new ArgumentException(
                "Approved by is required.",
                nameof(approvedBy));

        if (Status != JournalEntryStatus.PendingApproval)
        {
            throw new InvalidOperationException(
                "Only pending journals can be approved.");
        }

        if (!IsBalanced())
        {
            throw new InvalidOperationException(
                "Journal entry debit and credit totals must be equal.");
        }

        Status = JournalEntryStatus.Approved;
        ApprovedBy = approvedBy;
        ApprovedAtUtc = approvedAtUtc;
    }

    public void Post(
        Guid postedBy,
        DateTime postedAtUtc)
    {
        if (postedBy == Guid.Empty)
            throw new ArgumentException(
                "Posted by is required.",
                nameof(postedBy));

        if (Status != JournalEntryStatus.Approved)
        {
            throw new InvalidOperationException(
                "Only approved journals can be posted.");
        }

        if (!IsBalanced())
        {
            throw new InvalidOperationException(
                "Journal entry debit and credit totals must be equal.");
        }

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
        {
            throw new InvalidOperationException(
                "Only posted journals can be reversed.");
        }

        ReversedJournalId = reversedJournalId;
        Status = JournalEntryStatus.Reversed;
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

    private void ReNumberLines()
    {
        for (var index = 0; index < _lines.Count; index++)
        {
            _lines[index].SetLineNumber(index + 1);
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
