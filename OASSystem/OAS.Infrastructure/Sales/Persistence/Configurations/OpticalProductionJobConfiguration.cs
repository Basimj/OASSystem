using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalProductionJobConfiguration : IEntityTypeConfiguration<OpticalProductionJob>
{
    public void Configure(EntityTypeBuilder<OpticalProductionJob> builder)
    {
        builder.ToTable("tbl_OpticalProductionJobs", "dbo", table =>
        {
            table.HasCheckConstraint("CK_OpticalProductionJobs_RemakeNumber", "[RemakeNumber] >= 0");
            table.HasCheckConstraint("CK_OpticalProductionJobs_QcCounters", "[QcAttemptCount] >= 0 AND [FailedQcCount] >= 0 AND [FailedQcCount] <= [QcAttemptCount]");
            table.HasCheckConstraint("CK_OpticalProductionJobs_MaterialCost", "[MaterialCostBase] >= 0");
        });

        builder.HasKey(x => x.Id);
        builder.ConfigureSalesAudit();
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.JobCode).HasMaxLength(40).IsRequired();
        builder.Property(x => x.JobDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.TargetDate).HasColumnType("date");
        builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.MaterialCostBase).HasPrecision(19, 4).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.RemakeOfJobId);
        builder.Property(x => x.RemakeNumber).IsRequired();
        builder.Property(x => x.LastQcResult).HasConversion<byte>();
        builder.Property(x => x.QcAttemptCount).IsRequired();
        builder.Property(x => x.FailedQcCount).IsRequired();
        builder.Property(x => x.LastQcNotes).HasMaxLength(1000);
        builder.Property(x => x.LastQcWasBreakage).IsRequired();
        builder.Property(x => x.ReleasedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.StartedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.QcAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.FailedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.CompletedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => x.JobCode).IsUnique().HasDatabaseName("UX_OpticalProductionJobs_JobCode");
        builder.HasIndex(x => x.SalesInvoiceLineId).IsUnique().HasFilter("[IsActive] = 1").HasDatabaseName("UX_OpticalProductionJobs_ActiveInvoiceLine");
        builder.HasIndex(x => x.Status).HasDatabaseName("IX_OpticalProductionJobs_Status");
        builder.HasIndex(x => x.RemakeOfJobId).HasDatabaseName("IX_OpticalProductionJobs_RemakeOfJobId");

        builder.HasOne<SalesInvoice>().WithMany().HasForeignKey(x => x.SalesInvoiceId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionJobs_SalesInvoices_SalesInvoiceId");
        builder.HasOne<SalesInvoiceLine>().WithMany().HasForeignKey(x => x.SalesInvoiceLineId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionJobs_SalesInvoiceLines_SalesInvoiceLineId");
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionJobs_Customers_CustomerId");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionJobs_Warehouses_WarehouseId");
        builder.HasOne<InventoryTransaction>().WithMany().HasForeignKey(x => x.InventoryTransactionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionJobs_InventoryTransactions_InventoryTransactionId");
        builder.HasOne<OpticalProductionJob>().WithMany().HasForeignKey(x => x.RemakeOfJobId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionJobs_RemakeOfJobId");
        builder.HasMany(x => x.Materials).WithOne().HasForeignKey(x => x.OpticalProductionJobId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalProductionMaterials_Jobs_OpticalProductionJobId");
        builder.Navigation(x => x.Materials).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
