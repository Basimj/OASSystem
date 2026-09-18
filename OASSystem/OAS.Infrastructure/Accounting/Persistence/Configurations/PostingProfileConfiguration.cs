using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class PostingProfileConfiguration : IEntityTypeConfiguration<PostingProfile>
{
    public void Configure(EntityTypeBuilder<PostingProfile> builder)
    {
        builder.ToTable("PostingProfiles", "accounting");

        builder.HasKey(x => x.Id);

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

        builder.HasMany(x => x.Lines)
            .WithOne()
            .HasForeignKey(x => x.PostingProfileId)
            .OnDelete(DeleteBehavior.Cascade)
            .HasConstraintName("FK_PostingProfileLines_PostingProfile");

        builder.Navigation(x => x.Lines)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
