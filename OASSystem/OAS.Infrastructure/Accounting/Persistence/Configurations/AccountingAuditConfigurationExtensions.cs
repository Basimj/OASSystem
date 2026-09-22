using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Common.Entities;

namespace OAS.Infrastructure.Accounting.Persistence.Configurations;

internal static class AccountingAuditConfigurationExtensions
{
    public static void ConfigureAccountingAudit<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity<Guid>
    {
        builder.Property(x => x.CreatedAtUtc)
            .HasColumnName("CreatedAt")
            .HasColumnType("datetimeoffset")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasColumnName("CreatedBy")
            .HasMaxLength(64);

        builder.Property(x => x.LastModifiedAtUtc)
            .HasColumnName("UpdatedAt")
            .HasColumnType("datetimeoffset");

        builder.Property(x => x.LastModifiedBy)
            .HasColumnName("UpdatedBy")
            .HasMaxLength(64);

        // Device/source values are deliberately shadow properties here. The Accounting
        // module does not accept them from API contracts or the Client. They remain
        // reserved for the project's request/audit infrastructure to populate.
        builder.Property<string?>("CreatedFromDevice")
            .HasColumnName("CreatedFromDevice")
            .HasMaxLength(256);

        builder.Property<string?>("UpdatedFromDevice")
            .HasColumnName("UpdatedFromDevice")
            .HasMaxLength(256);
    }
}
