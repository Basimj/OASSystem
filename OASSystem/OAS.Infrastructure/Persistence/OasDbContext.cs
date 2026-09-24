using Microsoft.EntityFrameworkCore;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Identity.Entities;

namespace OAS.Infrastructure.Persistence;

public sealed class OasDbContext(DbContextOptions<OasDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserPasswordHistory> UserPasswordHistory => Set<UserPasswordHistory>();

    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<JobTitle> JobTitles => Set<JobTitle>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<FrameDetails> FrameDetails => Set<FrameDetails>();
    public DbSet<LensDetails> LensDetails => Set<LensDetails>();

    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<InventoryBalance> InventoryBalances => Set<InventoryBalance>();
    public DbSet<InventoryTransaction> InventoryTransactions => Set<InventoryTransaction>();
    public DbSet<InventoryTransactionLine> InventoryTransactionLines => Set<InventoryTransactionLine>();
    public DbSet<InventoryLedger> InventoryLedger => Set<InventoryLedger>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();
    public DbSet<Unit> Units => Set<Unit>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OasDbContext).Assembly);
    }
}
