using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class PostingProfileConfiguration : IEntityTypeConfiguration<PostingProfile>
{
    public void Configure(EntityTypeBuilder<PostingProfile> builder)
    {
        // Final database naming after AccountingAuditTableNaming + MoveAccountingTablesToDbo.
        builder.ToTable("tbl_PostingProfiles", "dbo");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(40);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Module)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.IsActive)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(x => x.Code)
            .IsUnique()
            .HasDatabaseName("UX_PostingProfiles_Code");

        builder.HasIndex(x => new { x.Module, x.DocumentType })
            .IsUnique()
            .HasDatabaseName("UX_PostingProfiles_Active_PurchaseReceipt")
            .HasFilter("[IsActive] = 1 AND [Module] = N'Purchasing' AND [DocumentType] = N'PurchaseReceipt'");

        builder.HasIndex(x => new { x.Module, x.DocumentType })
            .IsUnique()
            .HasDatabaseName("UX_PostingProfiles_Active_PurchaseInvoice")
            .HasFilter("[IsActive] = 1 AND [Module] = N'Purchasing' AND [DocumentType] = N'PurchaseInvoice'");

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.PostingProfileId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PostingProfileLines_PostingProfile");

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
