using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Contracts.Common.Pagination;
using OAS.Domain.Accounting.Entities;

namespace OAS.Application.Accounting.Journals.Specifications;

public sealed class JournalEntryPageSpecification
    : Specification<JournalEntry>
{
    public JournalEntryPageSpecification(
        PageRequest request)
    {
        var normalized =
            request.Normalize();

        if (!string.IsNullOrWhiteSpace(normalized.Search))
        {
            var search =
                normalized.Search.Trim();

            Where(x =>
                x.JournalNumber.Contains(search) ||
                x.Description.Contains(search) ||
                (x.SourceModule != null &&
                 x.SourceModule.Contains(search)) ||
                (x.SourceDocumentType != null &&
                 x.SourceDocumentType.Contains(search)));
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