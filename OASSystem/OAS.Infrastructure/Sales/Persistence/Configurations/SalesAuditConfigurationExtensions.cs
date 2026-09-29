using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OAS.Domain.Common.Entities;
using OAS.Infrastructure.Persistence.Configurations;

namespace OAS.Infrastructure.Sales.Persistence.Configurations;

internal static class SalesAuditConfigurationExtensions
{
    public static void ConfigureSalesAudit<TEntity>(
        this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity<Guid>
        => builder.ConfigureOasAudit();
}
