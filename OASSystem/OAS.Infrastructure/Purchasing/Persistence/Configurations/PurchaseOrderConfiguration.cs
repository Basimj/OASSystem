using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Purchasing.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

public sealed class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> builder)
    {
        builder.ToTable("tbl_PurchaseOrders", "dbo", t => { t.HasCheckConstraint("CK_PurchaseOrders_ExchangeRate_Positive", "[ExchangeRate] > 0"); t.HasCheckConstraint("CK_PurchaseOrders_PaymentTermDays_NonNegative", "[PaymentTermDays] >= 0"); t.HasCheckConstraint("CK_PurchaseOrders_Totals_NonNegative", "[Subtotal] >= 0 AND [DiscountAmount] >= 0 AND [TaxAmount] >= 0 AND [TotalAmount] >= 0"); });
        builder.HasKey(x => x.Id); builder.ConfigurePurchasingAudit(); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.PurchaseOrderCode).IsRequired().HasMaxLength(40); builder.Property(x => x.SupplierId).IsRequired(); builder.Property(x => x.DestinationWarehouseId).IsRequired();
        builder.Property(x => x.OrderDate).HasColumnType("date").IsRequired(); builder.Property(x => x.ExpectedDeliveryDate).HasColumnType("date"); builder.Property(x => x.CurrencyId).IsRequired(); builder.Property(x => x.ExchangeRate).HasPrecision(19,8).IsRequired(); builder.Property(x => x.ExchangeRateDate).HasColumnType("date").IsRequired(); builder.Property(x => x.TaxCalculationMode).HasConversion<byte>().IsRequired(); builder.Property(x => x.Status).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(19,4).IsRequired(); builder.Property(x => x.DiscountAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.TaxAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.TotalAmount).HasPrecision(19,4).IsRequired(); builder.Property(x => x.PaymentTermDays).IsRequired(); builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.SubmittedBy).HasMaxLength(64); builder.Property(x => x.SubmittedAt).HasColumnType("datetimeoffset"); builder.Property(x => x.ApprovedBy).HasMaxLength(64); builder.Property(x => x.ApprovedAt).HasColumnType("datetimeoffset"); builder.Property(x => x.RejectedBy).HasMaxLength(64); builder.Property(x => x.RejectedAt).HasColumnType("datetimeoffset"); builder.Property(x => x.RejectionReason).HasMaxLength(500); builder.Property(x => x.SentBy).HasMaxLength(64); builder.Property(x => x.SentAt).HasColumnType("datetimeoffset"); builder.Property(x => x.ClosedBy).HasMaxLength(64); builder.Property(x => x.ClosedAt).HasColumnType("datetimeoffset"); builder.Property(x => x.CancelledBy).HasMaxLength(64); builder.Property(x => x.CancelledAt).HasColumnType("datetimeoffset"); builder.Property(x => x.CancellationReason).HasMaxLength(500); builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.HasIndex(x => x.PurchaseOrderCode).IsUnique().HasDatabaseName("UX_PurchaseOrders_PurchaseOrderCode"); builder.HasIndex(x => x.SupplierId).HasDatabaseName("IX_PurchaseOrders_SupplierId"); builder.HasIndex(x => x.Status).HasDatabaseName("IX_PurchaseOrders_Status"); builder.HasIndex(x => x.OrderDate).HasDatabaseName("IX_PurchaseOrders_OrderDate"); builder.HasIndex(x => x.DestinationWarehouseId).HasDatabaseName("IX_PurchaseOrders_DestinationWarehouseId");
        builder.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrders_Suppliers_SupplierId"); builder.HasOne<Warehouse>().WithMany().HasForeignKey(x => x.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrders_Warehouses_DestinationWarehouseId"); builder.HasOne<Currency>().WithMany().HasForeignKey(x => x.CurrencyId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrders_Currencies_CurrencyId");
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_PurchaseOrderLines_Orders_PurchaseOrderId"); builder.Navigation(x => x.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
