using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Common.Entities;
using OAS.Infrastructure.Persistence.Configurations;

namespace OAS.Infrastructure.Purchasing.Persistence.Configurations;

internal static class PurchasingAuditConfigurationExtensions
{
    public static void ConfigurePurchasingAudit<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity<Guid>
        => builder.ConfigureOasAudit();
}
