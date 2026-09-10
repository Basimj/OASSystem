using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Features.Employees.Entities;

namespace OAS.Infrastructure.Features.Employees.Persistence;

public sealed class JobTitleConfiguration : IEntityTypeConfiguration<JobTitle>
{
    public void Configure(EntityTypeBuilder<JobTitle> builder)
    {
        builder.ToTable("JobTitles", "hr");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion().IsConcurrencyToken();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.LastModifiedBy).HasMaxLength(64);
        builder.HasIndex(x => x.Name).IsUnique().HasDatabaseName("UX_JobTitles_Name");
        builder.HasIndex(x => x.IsActive).HasDatabaseName("IX_JobTitles_IsActive");
    }
}
