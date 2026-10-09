using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalJobBreakageConfiguration : IEntityTypeConfiguration<OpticalJobBreakage>
{
    public void Configure(EntityTypeBuilder<OpticalJobBreakage> builder)
    {
        builder.ToTable("tbl_OpticalJobBreakages", "dbo", t => t.HasCheckConstraint("CK_tbl_OpticalJobBreakages_Quantity_GT_0", "[Quantity] > 0"));
        builder.HasKey(x => x.Id).HasName("PK_tbl_OpticalJobBreakages");
        builder.ConfigureSalesAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OpticalJobId).IsRequired();
        builder.Property(x => x.OpticalJobLineId).IsRequired();
        builder.Property(x => x.ProductVariantId).IsRequired();
        builder.Property(x => x.Eye).HasConversion<byte?>();
        builder.Property(x => x.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.ReasonCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReasonText).HasMaxLength(1000);
        builder.Property(x => x.TechnicianId);
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.RequiresReplacement).IsRequired();
        builder.Property(x => x.InventoryTransactionId);
        builder.Property(x => x.JournalEntryId);
        builder.Property(x => x.RecordedBy).IsRequired();
        builder.Property(x => x.RecordedAtUtc).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(100);
        builder.Property(x => x.ClosedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.OpticalJobId).HasDatabaseName("IX_tbl_OpticalJobBreakages_OpticalJobId");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_tbl_OpticalJobBreakages_Status");
        builder.HasIndex(x => new { x.OpticalJobId, x.IdempotencyKey }).IsUnique().HasFilter("[IdempotencyKey] IS NOT NULL").HasDatabaseName("UX_tbl_OpticalJobBreakages_Job_IdempotencyKey");
        builder.HasOne<OpticalJob>().WithMany().HasForeignKey(x => x.OpticalJobId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobBreakages_OpticalJobs");
        builder.HasOne<OpticalJobLine>().WithMany().HasForeignKey(x => x.OpticalJobLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobBreakages_OpticalJobLines");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobBreakages_ProductVariants");
        builder.HasOne<Employee>().WithMany().HasForeignKey(x => x.TechnicianId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobBreakages_Employees");
        builder.HasOne<InventoryTransaction>().WithMany().HasForeignKey(x => x.InventoryTransactionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobBreakages_InventoryTransactions");
        builder.HasOne<JournalEntry>().WithMany().HasForeignKey(x => x.JournalEntryId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_tbl_OpticalJobBreakages_JournalEntries");
    }
}
