using Microsoft.EntityFrameworkCore;
using OAS.Domain.Identity.Entities;

namespace OAS.Infrastructure.Persistence;

public sealed class OasDbContext(DbContextOptions<OasDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<UserPasswordHistory> UserPasswordHistory => Set<UserPasswordHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(OasDbContext).Assembly);
    }
}
