using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Accounting.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

public sealed class PostingProfileLineConfiguration : IEntityTypeConfiguration<PostingProfileLine>
{
    public void Configure(EntityTypeBuilder<PostingProfileLine> builder)
    {
        builder.ToTable("tbl_PostingProfileLines", "dbo");

        builder.HasKey(x => x.Id);

        builder.ConfigureAccountingAudit();

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.PostingProfileId)
            .IsRequired();

        builder.Property(x => x.AccountRole)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.AccountId)
            .IsRequired();

        builder.Property(x => x.IsRequired)
            .IsRequired();

        builder.HasIndex(x => x.PostingProfileId)
            .HasDatabaseName("IX_PostingProfileLines_PostingProfileId");

        builder.HasIndex(x => x.AccountId)
            .HasDatabaseName("IX_PostingProfileLines_AccountId");

        builder.HasOne<Account>()
            .WithMany()
            .HasForeignKey(x => x.AccountId)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("FK_PostingProfileLines_Account");
    }
}
