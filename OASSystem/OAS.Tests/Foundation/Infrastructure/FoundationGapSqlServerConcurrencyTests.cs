using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Domain.Entities.Inventory;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Foundation.Infrastructure;

// Opt in with OAS_FOUNDATION_TEST_SQLSERVER. A uniquely named temporary database is used.
[TestFixture, Category("FoundationSqlServer"), NonParallelizable]
public sealed class FoundationGapSqlServerConcurrencyTests
{
    private string _connection = string.Empty;

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        var configured = Environment.GetEnvironmentVariable("OAS_FOUNDATION_TEST_SQLSERVER");
        if (string.IsNullOrWhiteSpace(configured))
        {
            Assert.Ignore("Set OAS_FOUNDATION_TEST_SQLSERVER to run foundation SQL Server concurrency tests.");
            return;
        }

        var builder = new SqlConnectionStringBuilder(configured)
        {
            InitialCatalog = $"OAS_Foundation_Test_{Guid.NewGuid():N}"
        };
        _connection = builder.ConnectionString;
        await using var context = Context();
        await context.Database.EnsureCreatedAsync();
    }

    [OneTimeTearDown]
    public async Task DropOwnedDatabase()
    {
        if (string.IsNullOrWhiteSpace(_connection)) return;
        await using var context = Context();
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task ConcurrentLensVariantDetails_AllowOnlyOneDetailPerProductVariant()
    {
        var variantId = await CreateVariantAsync();

        async Task<bool> InsertAsync(decimal sph)
        {
            await using var context = Context();
            context.Add(LensVariantDetail.Create(Guid.NewGuid(), variantId, sph, -0.75m, null));
            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException)
            {
                return false;
            }
        }

        var outcomes = await Task.WhenAll(InsertAsync(-2m), InsertAsync(-2.25m));

        Assert.That(outcomes.Count(x => x), Is.EqualTo(1));
        await using var verify = Context();
        Assert.That(await verify.Set<LensVariantDetail>().CountAsync(x => x.ProductVariantId == variantId), Is.EqualTo(1));
    }

    [Test]
    public async Task LensVariantDetail_RowVersionRejectsStaleWriter()
    {
        var variantId = await CreateVariantAsync();
        await using (var seed = Context())
        {
            seed.Add(LensVariantDetail.Create(Guid.NewGuid(), variantId, -2m, -0.75m, null));
            await seed.SaveChangesAsync();
        }

        await using var first = Context();
        await using var second = Context();
        var a = await first.Set<LensVariantDetail>().SingleAsync(x => x.ProductVariantId == variantId);
        var b = await second.Set<LensVariantDetail>().SingleAsync(x => x.ProductVariantId == variantId);

        a.Update(a.SPH, a.CYL, a.ADD, 8.60m, 14.20m, true);
        await first.SaveChangesAsync();

        b.Update(b.SPH, b.CYL, b.ADD, 8.70m, 14.20m, true);
        Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () => await second.SaveChangesAsync());
    }

    private async Task<Guid> CreateVariantAsync()
    {
        await using var context = Context();
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var category = new ProductCategory($"CAT-{suffix}", "تصنيف اختبار");
        var productType = new ProductType($"TYPE-{suffix}", "نوع اختبار");
        var product = new Product($"P-{suffix}", "عدسة اختبار", category.Id, productType.Id);
        var variant = new ProductVariant(product.Id, $"SKU-{suffix}", 10m, 20m);
        context.AddRange(category, productType, product, variant);
        await context.SaveChangesAsync();
        return variant.Id;
    }

    private OasDbContext Context() => new(new DbContextOptionsBuilder<OasDbContext>()
        .UseSqlServer(_connection)
        .Options);
}
