using NUnit.Framework;
using OAS.Application.Abstractions.Persistence;
using OAS.Application.Abstractions.Persistence.Specifications;
using OAS.Application.Common.Exceptions;
using OAS.Application.Sales.Returns.Services;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Sales;
using OAS.Domain.Sales.Entities;

namespace OAS.Tests.Sales.Application;

[TestFixture]
public sealed class SalesReturnAccountingPostingServiceTests
{
    [TestCase(JournalEntryStatus.Draft)]
    [TestCase(JournalEntryStatus.PendingApproval)]
    [TestCase(JournalEntryStatus.Approved)]
    public void ExistingIncompleteJournal_BlocksRetry_InsteadOfCreatingAnotherJournal(JournalEntryStatus status)
    {
        var salesReturn = CreateReturn();
        var existing = CreateJournal(salesReturn.Id, status);
        var service = CreateService(existing);

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            service.PostAsync(salesReturn, Guid.NewGuid(), DateTime.UtcNow));

        Assert.That(ex!.Code, Is.EqualTo("sales_return_journal_partial_posting"));
    }

    [Test]
    public async Task ExistingPostedJournal_MakesRetryIdempotent()
    {
        var salesReturn = CreateReturn();
        var existing = CreateJournal(salesReturn.Id, JournalEntryStatus.Posted);
        var service = CreateService(existing);

        var journalId = await service.PostAsync(salesReturn, Guid.NewGuid(), DateTime.UtcNow);

        Assert.That(journalId, Is.EqualTo(existing.Id));
    }

    [Test]
    public void MultipleExistingJournals_AreRejectedForManualReview()
    {
        var salesReturn = CreateReturn();
        var first = CreateJournal(salesReturn.Id, JournalEntryStatus.Posted);
        var second = CreateJournal(salesReturn.Id, JournalEntryStatus.Draft);
        var service = CreateService(first, second);

        var ex = Assert.ThrowsAsync<ConflictException>(() =>
            service.PostAsync(salesReturn, Guid.NewGuid(), DateTime.UtcNow));

        Assert.That(ex!.Code, Is.EqualTo("sales_return_journal_duplicate"));
    }

    private static SalesReturnAccountingPostingService CreateService(params JournalEntry[] existingJournals) =>
        new(
            new JournalRepository(existingJournals),
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);

    private static SalesReturn CreateReturn()
    {
        var date = new DateOnly(2026, 10, 17);
        var currencyId = Guid.NewGuid();
        return SalesReturn.Create(
            Guid.NewGuid(),
            "SR-SA01-TEST",
            Guid.NewGuid(),
            Guid.NewGuid(),
            date,
            date,
            currencyId,
            "YER",
            2,
            1m,
            date,
            ExchangeRateType.Accounting,
            ExchangeRateSource.System,
            currencyId,
            "YER",
            2,
            "SA-01 retry guard test");
    }

    private static JournalEntry CreateJournal(Guid salesReturnId, JournalEntryStatus status)
    {
        var date = new DateOnly(2026, 10, 17);
        return JournalEntry.Create(
            Guid.NewGuid(),
            $"JV-SA01-{Guid.NewGuid():N}",
            JournalType.Automatic,
            date,
            date,
            Guid.NewGuid(),
            "SA-01 existing journal",
            SalesSourceReferences.Module,
            SalesSourceReferences.SalesReturn,
            salesReturnId,
            status);
    }

    private sealed class JournalRepository(IEnumerable<JournalEntry> initial)
        : IRepository<JournalEntry, Guid>
    {
        private readonly List<JournalEntry> _items = [.. initial];

        public Task<JournalEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.SingleOrDefault(x => x.Id == id));

        public Task<JournalEntry?> GetForUpdateAsync(Guid id, CancellationToken cancellationToken = default) =>
            GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<JournalEntry>> ListAsync(
            ISpecification<JournalEntry>? specification = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<JournalEntry>>(_items.ToArray());

        public Task<PagedData<JournalEntry>> GetPageAsync(
            ISpecification<JournalEntry> specification,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<long> CountAsync(
            ISpecification<JournalEntry>? specification = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult((long)_items.Count);

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.Any(x => x.Id == id));

        public Task AddAsync(JournalEntry entity, CancellationToken cancellationToken = default)
        {
            _items.Add(entity);
            return Task.CompletedTask;
        }

        public Task AddRangeAsync(IEnumerable<JournalEntry> entities, CancellationToken cancellationToken = default)
        {
            _items.AddRange(entities);
            return Task.CompletedTask;
        }

        public void Update(JournalEntry entity)
        {
        }

        public void Delete(JournalEntry entity) => _items.Remove(entity);

        public void DeleteRange(IEnumerable<JournalEntry> entities)
        {
            foreach (var entity in entities.ToArray())
                _items.Remove(entity);
        }
    }
}
