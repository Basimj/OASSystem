using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Identity.Entities;

namespace OAS.Infrastructure.Identity.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles", "security");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.NormalizedName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(64);
        builder.Property(x => x.LastModifiedBy).HasMaxLength(64);
        builder.Ignore(x => x.DomainEvents);
        builder.HasIndex(x => x.NormalizedName).IsUnique();
    }
}
