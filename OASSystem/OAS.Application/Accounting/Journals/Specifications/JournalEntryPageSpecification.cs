using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;

namespace OAS.Application.Accounting.Journals.Specifications;

public sealed class JournalEntryPageSpecification
    : Specification<JournalEntry>
{
    public JournalEntryPageSpecification(
        PageRequest request)
    {
        var normalized =
            request.Normalize();

        // Combine the two predicates in ONE Where call: Specification.Where
        // replaces the criterion rather than appending a second one.
        var search = normalized.Search?.Trim();
        JournalEntryStatus? status = normalized.Status switch
        {
            "draft" => JournalEntryStatus.Draft,
            "pending" => JournalEntryStatus.PendingApproval,
            "approved" => JournalEntryStatus.Approved,
            "posted" => JournalEntryStatus.Posted,
            "reversed" => JournalEntryStatus.Reversed,
            _ => (JournalEntryStatus?)null
        };
        var unposted = normalized.Status == "unposted";
        if (status.HasValue || unposted || !string.IsNullOrWhiteSpace(search))
        {
            Where(x =>
                (!status.HasValue || x.Status == status.Value) &&
                (!unposted || (x.Status != JournalEntryStatus.Posted && x.Status != JournalEntryStatus.Reversed)) &&
                (string.IsNullOrEmpty(search) ||
                 x.JournalNumber.Contains(search) ||
                 x.Description.Contains(search) ||
                 (x.SourceModule != null && x.SourceModule.Contains(search)) ||
                 (x.SourceDocumentType != null && x.SourceDocumentType.Contains(search))));
        }

        var sortBy =
            normalized.SortBy;

        switch (sortBy)
        {
            case nameof(JournalEntry.JournalNumber):
                AddSort(
                    nameof(JournalEntry.JournalNumber),
                    normalized.SortDirection);
                break;

            case nameof(JournalEntry.PostingDate):
                AddSort(
                    nameof(JournalEntry.PostingDate),
                    normalized.SortDirection);
                break;

            case nameof(JournalEntry.DocumentDate):
                AddSort(
                    nameof(JournalEntry.DocumentDate),
                    normalized.SortDirection);
                break;

            case nameof(JournalEntry.Status):
                AddSort(
                    nameof(JournalEntry.Status),
                    normalized.SortDirection);
                break;

            case nameof(JournalEntry.CreatedAtUtc):
                AddSort(
                    nameof(JournalEntry.CreatedAtUtc),
                    normalized.SortDirection);
                break;

            default:
                AddSort(
                    nameof(JournalEntry.JournalNumber),
                    SortDirection.Ascending);
                break;
        }

        var skip =
            (normalized.PageNumber - 1) *
            normalized.PageSize;

        ApplyPaging(
            skip,
            normalized.PageSize);
    }
}