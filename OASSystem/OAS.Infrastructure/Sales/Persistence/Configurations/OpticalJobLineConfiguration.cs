using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class OpticalJobLineConfiguration : IEntityTypeConfiguration<OpticalJobLine>
{
    public void Configure(EntityTypeBuilder<OpticalJobLine> builder)
    {
        builder.ToTable("tbl_OpticalJobLines", "dbo", table =>
        {
            table.HasCheckConstraint("CK_OpticalJobLines_LineNumber", "[LineNumber] > 0");
            table.HasCheckConstraint("CK_OpticalJobLines_Quantity", "[Quantity] > 0");
        });

        builder.HasKey(x => x.Id).HasName("PK_tbl_OpticalJobLines");
        builder.ConfigureSalesAudit();

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.OpticalJobId).IsRequired();
        builder.Property(x => x.CustomerOrderLineId).IsRequired();
        builder.Property(x => x.ProductVariantId);
        builder.Property(x => x.LineNumber).IsRequired();
        builder.Property(x => x.LineType).HasConversion<byte>().IsRequired();
        builder.Property(x => x.Eye).HasConversion<byte>();
        builder.Property(x => x.GroupId);
        builder.Property(x => x.DescriptionSnapshot).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 3).IsRequired();
        builder.Property(x => x.RequiresProduction).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();

        builder.HasIndex(x => new { x.OpticalJobId, x.LineNumber })
            .IsUnique().HasDatabaseName("UX_OpticalJobLines_Job_LineNumber");
        builder.HasIndex(x => new { x.OpticalJobId, x.CustomerOrderLineId })
            .IsUnique().HasDatabaseName("UX_OpticalJobLines_Job_CustomerOrderLine");
        builder.HasIndex(x => x.CustomerOrderLineId).HasDatabaseName("IX_OpticalJobLines_CustomerOrderLineId");
        builder.HasIndex(x => x.ProductVariantId).HasDatabaseName("IX_OpticalJobLines_ProductVariantId");
        builder.HasIndex(x => new { x.OpticalJobId, x.GroupId }).HasDatabaseName("IX_OpticalJobLines_OpticalJobId_GroupId");

        builder.HasOne<CustomerOrderLine>().WithMany().HasForeignKey(x => x.CustomerOrderLineId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobLines_CustomerOrderLines_CustomerOrderLineId");
        builder.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict).HasConstraintName("FK_OpticalJobLines_ProductVariants_ProductVariantId");
    }
}
