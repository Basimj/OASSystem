using NUnit.Framework;
using OAS.Application.Accounting.Posting;
using OAS.Application.Common.Exceptions;
using OAS.Application.Inventory.Services;
using OAS.Application.Inventory.Transactions;
using OAS.Application.Inventory.Transactions.Openings;
using OAS.Contracts.Inventory.Reports;
using OAS.Contracts.Inventory.Transactions;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Accounting.Enums;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Enums.Inventory;
using OAS.Tests.Inventory.Fakes;

namespace OAS.Tests.Inventory;

[TestFixture]
public sealed class InventoryOpeningTests
{
    private InventoryOpeningHandler _handler = null!;
    private FakeInventoryTransactionRepository _documents = null!;
    private FakeGenericRepository<InventoryTransactionLine, Guid> _lines = null!;
    private FakeInventoryBalanceRepository _balances = null!;
    private FakeGenericRepository<InventoryLedger, Guid> _ledger = null!;
    private FakeGenericRepository<JournalEntry, Guid> _journals = null!;
    private FakeGenericRepository<JournalEntryLine, Guid> _journalLines = null!;
    private PostingProfile _profile = null!;
    private Guid _warehouse, _variant;
    private static readonly DateOnly Date = new(2026,1,1);
    private static Account AccountFor(string code, AccountClass kind, NormalBalance normal) => Account.Create(Guid.NewGuid(),code,code,null,null,1,kind,AccountType.Posting,normal,true,false,false,false,true,null);

    [SetUp]
    public void SetUp()
    {
        _lines = new(); _documents = new(lines: _lines.Items); _balances = new(); _ledger = new(); _journals = new(); _journalLines = new();
        var warehouse = new Warehouse("W-01","Warehouse"); _warehouse = warehouse.Id;
        var product = new Product("P-01","Product",Guid.NewGuid(),isStockItem:true);
        var variant = new ProductVariant(product.Id,"SKU-01",10m,20m); _variant = variant.Id;
        var inventory = AccountFor("1100",AccountClass.Asset,NormalBalance.Debit);
        var offset = AccountFor("3100",AccountClass.Equity,NormalBalance.Credit);
        var currency = Currency.Create(Guid.NewGuid(),"YER","ريال",null,null,2);
        var settings = new FakeGenericRepository<AccountingSettings,Guid>([AccountingSettings.Create(currency.Id,inventoryAccountId:inventory.Id)]);
        var currencies = new FakeGenericRepository<Currency,Guid>([currency]);
        var periods = new FakeGenericRepository<FiscalPeriod,Guid>([FiscalPeriod.Create(Guid.NewGuid(),Guid.NewGuid(),1,"2026",Date,new DateOnly(2026,12,31),FiscalPeriodStatus.Open,false,false,false)]);
        _profile = PostingProfile.Create(Guid.NewGuid(),"INV-OPEN","Opening","Inventory","InventoryOpening",true);
        var profiles = new FakeGenericRepository<PostingProfile,Guid>([_profile]);
        var roles = new FakeGenericRepository<PostingProfileLine,Guid>([PostingProfileLine.Create(Guid.NewGuid(),_profile.Id,"Inventory",inventory.Id,true),PostingProfileLine.Create(Guid.NewGuid(),_profile.Id,"OpeningOffset",offset.Id,true)]);
        var accounts = new FakeGenericRepository<Account,Guid>([inventory,offset]);
        var seq = new FakeInventorySequenceNumberGenerator();
        var posting = new InventoryPostingService(_balances,_ledger,seq,TimeProvider.System);
        var accounting = new ProfileAccountingPostingService(_journals,periods,profiles,roles,accounts,new FakeGenericRepository<Supplier,Guid>(),settings,currencies,seq);
        _handler = new InventoryOpeningHandler(_documents,_lines,new FakeGenericRepository<Warehouse,Guid>([warehouse]),new FakeGenericRepository<ProductVariant,Guid>([variant]),new FakeGenericRepository<Product,Guid>([product]),_balances,_ledger,profiles,roles,accounts,settings,currencies,_journals,_journalLines,periods,posting,accounting,seq,new FakeCurrentUser(Guid.NewGuid().ToString()),TimeProvider.System);
    }
    private InventoryOpeningRequest Request(decimal qty=10m,decimal cost=25m) => new(_warehouse,Date,null,[new(_variant,qty,cost,null)]);
    private Task<Guid> Save(InventoryOpeningRequest request) => _handler.Handle(new SaveInventoryOpening(null,request),CancellationToken.None);

    [Test]
    public async Task DraftDoesNotAffectStockAndPostingCreatesBalancedJournal()
    {
        var id=await Save(Request());
        Assert.That(_balances.Items,Is.Empty); Assert.That(_journals.Items,Is.Empty);
        await _handler.Handle(new PostInventoryOpening(id),CancellationToken.None);
        var doc=_documents.Items.Single(); var journal=_journals.Items.Single(); var balance=_balances.Items.Single();
        Assert.Multiple(()=>{
            Assert.That(doc.Status,Is.EqualTo(InventoryTransactionStatus.Posted));
            Assert.That(doc.ReferenceId,Is.EqualTo(journal.Id));
            Assert.That(balance.OnHandQuantity,Is.EqualTo(10m));
            Assert.That(balance.InventoryValue,Is.EqualTo(250m));
            Assert.That(journal.Lines.Sum(x=>x.DebitAmount),Is.EqualTo(250m));
            Assert.That(journal.IsBalanced(),Is.True);
            Assert.That(_ledger.Items,Has.Count.EqualTo(1));
        });
        Assert.ThrowsAsync<ConflictException>(async()=>await _handler.Handle(new PostInventoryOpening(id),CancellationToken.None));
        Assert.ThrowsAsync<ConflictException>(async()=>await Save(Request()));
    }
    [TestCase(0,25)] [TestCase(10,0)] [TestCase(-1,25)]
    public void InvalidValuesFailBeforeCreatingDraft(int qty,int cost)
    {
        Assert.ThrowsAsync<ConflictException>(async()=>await Save(Request(qty,cost)));
        Assert.That(_documents.Items,Is.Empty);
    }
    [Test]
    public void ClosedSetupAndGenericInboundAreBlocked()
    {
        _profile.SetActive(false);
        Assert.ThrowsAsync<ConflictException>(async()=>await Save(Request()));
        Assert.That(ManualInventoryTransactionPolicy.IsAllowed(InventoryTransactionType.Opening),Is.False);
    }
    [Test]
    public async Task ReversalKeepsOriginalAndCreatesOppositeStockAndJournal()
    {
        var id=await Save(Request()); await _handler.Handle(new PostInventoryOpening(id),CancellationToken.None);
        _journalLines.Items.AddRange(_journals.Items.Single().Lines);
        var reverseId=await _handler.Handle(new ReverseInventoryOpening(id,new(Date,"Correction")),CancellationToken.None);
        Assert.Multiple(()=>{
            Assert.That(_balances.Items.Single().OnHandQuantity,Is.Zero);
            Assert.That(_balances.Items.Single().InventoryValue,Is.Zero);
            Assert.That(_documents.Items.Count,Is.EqualTo(2));
            Assert.That(_documents.Items.Single(x=>x.Id==reverseId).ReferenceId,Is.EqualTo(id));
            Assert.That(_journals.Items.Sum(x=>x.Lines.Sum(l=>l.DebitAmount-l.CreditAmount)),Is.Zero);
            Assert.That(_journals.Items.First().ReversedJournalId,Is.EqualTo(_journals.Items.Last().Id));
        });
        Assert.ThrowsAsync<ConflictException>(async()=>await _handler.Handle(new ReverseInventoryOpening(id,new(Date,"Again")),CancellationToken.None));
    }
    [Test]
    public async Task ReservedStockCannotBeReversed()
    {
        var id=await Save(Request());await _handler.Handle(new PostInventoryOpening(id),CancellationToken.None);
        _balances.Items.Single().Reserve(1);
        Assert.ThrowsAsync<ConflictException>(async()=>await _handler.Handle(new ReverseInventoryOpening(id,new(Date,"Correction")),CancellationToken.None));
        Assert.That(_documents.Items,Has.Count.EqualTo(1));
    }
    [Test]
    public async Task DraftCanBeEditedAndDeletedWithoutStockImpact()
    {
        var id=await Save(Request());
        await _handler.Handle(new SaveInventoryOpening(id,Request(15m,30m)),CancellationToken.None);
        Assert.That(_lines.Items.Single().Quantity,Is.EqualTo(15m));
        await _handler.Handle(new DeleteInventoryOpening(id),CancellationToken.None);
        Assert.That(_documents.Items,Is.Empty); Assert.That(_lines.Items,Is.Empty); Assert.That(_balances.Items,Is.Empty);
    }
    [Test]
    public async Task ZeroBalanceWithMovementHistoryCannotBeInitializedAgain()
    {
        var balance=new InventoryBalance(_warehouse,_variant);
        balance.ApplyInbound(3m,25m,DateTimeOffset.UtcNow);balance.ApplyOutbound(3m,DateTimeOffset.UtcNow);
        await _balances.AddAsync(balance);
        Assert.ThrowsAsync<ConflictException>(async()=>await Save(Request()));
        Assert.That(_documents.Items,Is.Empty);
    }
    [Test]
    public void OpeningWithinPeriodIsNotBeginningBalanceOrPurchase()
    {
        var totals=WarehouseReportAggregation.SumFlows([new WarehouseFlow(10,2,1,0,0,0,0,0,0,18,5)]);
        Assert.Multiple(()=>{Assert.That(totals.Opening,Is.EqualTo(10m));Assert.That(totals.OpeningIn,Is.EqualTo(5m));Assert.That(totals.Purchases,Is.EqualTo(2m));Assert.That(totals.In,Is.EqualTo(8m));});
    }
}
