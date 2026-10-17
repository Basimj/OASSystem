using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Entities.Inventory;
using OAS.Domain.Features.Employees.Entities;
using OAS.Domain.Sales.Entities;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

public sealed class CommissionRuleConfiguration : IEntityTypeConfiguration<CommissionRule>
{
    public void Configure(EntityTypeBuilder<CommissionRule> b)
    {
        b.ToTable("tbl_CommissionRules", "dbo", t =>
        {
            t.HasCheckConstraint("CK_CommissionRules_Rate", "[RatePercent] >= 0 AND [RatePercent] <= 100");
            t.HasCheckConstraint("CK_CommissionRules_Priority", "[Priority] >= 0 AND [Priority] <= 10000");
        });
        b.HasKey(x => x.Id);
        b.ConfigureSalesAudit();
        b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Code).HasMaxLength(40).IsRequired();
        b.Property(x => x.Name).HasMaxLength(160).IsRequired();
        b.Property(x => x.EmployeeId);
        b.Property(x => x.ProductVariantId);
        b.Property(x => x.ProductCategoryId);
        b.Property(x => x.SaleType).HasConversion<byte>();
        b.Property(x => x.Priority).IsRequired().HasDefaultValue(0);
        b.Property(x => x.BasisKind).IsRequired().HasConversion<byte>().HasDefaultValue(OAS.Domain.Sales.Commissions.CommissionBasisKind.NetAfterDiscountExcludingTax);
        b.Property(x => x.RatePercent).HasPrecision(9, 4).IsRequired();
        b.Property(x => x.EffectiveFrom).HasColumnType("date").IsRequired();
        b.Property(x => x.EffectiveTo).HasColumnType("date");
        b.Property(x => x.IsActive).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        b.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_CommissionRules_Code");
        b.HasIndex(x => new { x.EmployeeId, x.ProductVariantId, x.ProductCategoryId, x.SaleType, x.Priority, x.EffectiveFrom, x.IsActive })
            .HasDatabaseName("IX_CommissionRules_Resolution");
        b.HasOne<Employee>().WithMany().HasForeignKey(x => x.EmployeeId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CommissionRules_Employees_EmployeeId");
        b.HasOne<ProductVariant>().WithMany().HasForeignKey(x => x.ProductVariantId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CommissionRules_ProductVariants_ProductVariantId");
        b.HasOne<ProductCategory>().WithMany().HasForeignKey(x => x.ProductCategoryId).OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_CommissionRules_ProductCategories_ProductCategoryId");
    }
}
