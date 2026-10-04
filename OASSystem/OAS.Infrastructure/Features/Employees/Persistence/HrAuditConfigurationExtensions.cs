using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Common.Entities;

namespace OAS.Infrastructure.Features.Employees.Persistence;

internal static class HrAuditConfigurationExtensions
{
    public static void ConfigureHrAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity<Guid>
    {
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetimeoffset").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.LastModifiedAtUtc).HasColumnType("datetimeoffset");
        builder.Property(x => x.LastModifiedBy).HasMaxLength(64);
        builder.Property<string?>("CreatedFromDevice").HasMaxLength(256);
        builder.Property<string?>("UpdatedFromDevice").HasMaxLength(256);
    }
}
