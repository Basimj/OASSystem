using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OAS.Application.Abstractions.Security;
using OAS.Domain.Common.Interfaces;

namespace OAS.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(
    TimeProvider timeProvider,
    ICurrentUser currentUser,
    ICurrentRequestInfo currentRequestInfo)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow();
        var device = currentRequestInfo.Device;

        foreach (var entry in context.ChangeTracker
                     .Entries<IAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetCreatedAudit(
                    now,
                    currentUser.UserId);

                var deviceProperty =
                    entry.Metadata.FindProperty("CreatedFromDevice");

                if (deviceProperty is not null)
                {
                    entry.Property("CreatedFromDevice")
                        .CurrentValue = device;
                }
            }

            if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetModifiedAudit(
                    now,
                    currentUser.UserId);

                var deviceProperty =
                    entry.Metadata.FindProperty("UpdatedFromDevice");

                if (deviceProperty is not null)
                {
                    entry.Property("UpdatedFromDevice")
                        .CurrentValue = device;
                }
            }
        }
    }
}