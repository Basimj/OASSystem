using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseRequestConfiguration : IEntityTypeConfiguration<PurchaseRequest>
{
    public void Configure(EntityTypeBuilder<PurchaseRequest> builder)
    {
        builder.ToTable("tbl_PurchaseRequests", "dbo"); builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit();
        builder.Property(x => x.Id).ValueGeneratedNever(); builder.Property(x => x.RequestCode).IsRequired().HasMaxLength(40);
        builder.Property(x => x.RequestType).HasConversion<byte>().IsRequired(); builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.WarehouseId).IsRequired(); builder.Property(x => x.CustomerOrderId);
        builder.Property(x => x.RequestDate).HasColumnType("date").IsRequired(); builder.Property(x => x.RequiredDate).HasColumnType("date");
        builder.Property(x => x.Reason).HasMaxLength(500); builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.RequestedBy).HasMaxLength(64); builder.Property(x => x.SubmittedBy).HasMaxLength(64); builder.Property(x => x.SubmittedAt).HasColumnType("datetimeoffset");
        builder.Property(x => x.ApprovedBy).HasMaxLength(64); builder.Property(x => x.ApprovedAt).HasColumnType("datetimeoffset");
        builder.Property(x => x.RejectedBy).HasMaxLength(64); builder.Property(x => x.RejectedAt).HasColumnType("datetimeoffset"); builder.Property(x => x.RejectionReason).HasMaxLength(500);
        builder.Property(x => x.CancelledBy).HasMaxLength(64); builder.Property(x => x.CancelledAt).HasColumnType("datetimeoffset"); builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.RequestCode).IsUnique().HasDatabaseName("UX_PurchaseRequests_RequestCode"); builder.HasIndex(x => x.Status).HasDatabaseName("IX_PurchaseRequests_Status"); builder.HasIndex(x => x.RequestDate).HasDatabaseName("IX_PurchaseRequests_RequestDate"); builder.HasIndex(x => x.WarehouseId).HasDatabaseName("IX_PurchaseRequests_WarehouseId");
        builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseRequests_Warehouses_WarehouseId");
        builder.HasOne<CustomerOrder>().WithMany().HasForeignKey(x => x.CustomerOrderId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseRequests_CustomerOrders_CustomerOrderId");
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PurchaseRequestId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseRequestLines_Requests_PurchaseRequestId");
        builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
