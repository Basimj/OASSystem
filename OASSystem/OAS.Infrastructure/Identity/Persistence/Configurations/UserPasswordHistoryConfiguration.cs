using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Identity.Entities;

namespace OAS.Infrastructure.Identity.Persistence.Configurations;

public sealed class UserPasswordHistoryConfiguration : IEntityTypeConfiguration<UserPasswordHistory>
{
    public void Configure(EntityTypeBuilder<UserPasswordHistory> builder)
    {
        builder.ToTable("UserPasswordHistory", "security");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Ignore(x => x.DomainEvents);
        builder.HasIndex(x => new { x.UserId, x.CreatedAtUtc });
        builder.HasOne<UserAccount>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
