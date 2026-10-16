using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;

namespace OAS.Infrastructure.Persistence.Configurations.Inventory;
public sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable("tbl_Warehouses");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Code).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.AllowsSalesAllocation).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.LastModifiedBy).HasMaxLength(64);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_Warehouses_Code");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_Warehouses_IsActive");
        builder.HasIndex(x => new { x.IsActive, x.AllowsSalesAllocation }).HasDatabaseName("IX_Warehouses_SalesAllocation");
        builder.HasIndex(x => x.IsDefault).IsUnique().HasFilter("[IsDefault] = 1 AND [IsActive] = 1").HasDatabaseName("UX_Warehouses_ActiveDefault");
    }
}
