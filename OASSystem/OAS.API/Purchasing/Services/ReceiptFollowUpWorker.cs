using Microsoft.EntityFrameworkCore;
using OAS.Infrastructure.Database.Services;
using OAS.Infrastructure.Persistence;
using OAS.Infrastructure.Purchasing.Services;

namespace OAS.API.Purchasing.Services;

/// <summary>Each database profile is processed in an isolated DI scope; no HTTP user context is reused.</summary>
public sealed class ReceiptFollowUpWorker(
    IServiceScopeFactory scopes, DatabaseProfileCatalog catalog,
    ILogger<ReceiptFollowUpWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // During first-run database bootstrap a profile can be available before its schema.
        // Log missing-schema state once per profile, not on every polling interval.
        var waitingForSchema = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var profile in catalog.GetPublicProfiles().Profiles)
            {
                if (stoppingToken.IsCancellationRequested) break;
                try
                {
                    using var profileContext = ReceiptWorkerAmbient.Use(profile.Key);
                    await using var scope = scopes.CreateAsyncScope();
                    var db = scope.ServiceProvider.GetRequiredService<OasDbContext>();
                    // Do not query integration jobs until the migration has created the table.
                    // Checking OBJECT_ID is read-only and uses the same database profile/connection
                    // as the processor. SQL Server hides uncommitted DDL behind schema locks.
                    var tableReady = await db.Database.SqlQueryRaw<int>(
                            "SELECT CASE WHEN OBJECT_ID(N'dbo.tbl_PurchasingIntegrationJobs', N'U') " +
                            "IS NULL THEN 0 ELSE 1 END AS [Value]")
                        .SingleAsync(stoppingToken) == 1;
                    if (!tableReady)
                    {
                        if (waitingForSchema.Add(profile.Key))
                            logger.LogInformation(
                                "Receipt follow-up worker is paused for database profile {Profile}: " +
                                "AP-09 migration has not created tbl_PurchasingIntegrationJobs yet.",
                                profile.Key);
                        continue;
                    }
                    if (waitingForSchema.Remove(profile.Key))
                        logger.LogInformation(
                            "Receipt follow-up worker resumed after AP-09 schema was installed for profile {Profile}.",
                            profile.Key);
                    var processor = scope.ServiceProvider.GetRequiredService<ReceiptFollowUpProcessor>();
                    for (var i = 0; i < 30 && await processor.ProcessNextAsync(stoppingToken); i++) { }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception ex)
                {
                    // Database may still be bootstrapping/migrating. Keep worker alive.
                    logger.LogError(ex, "Receipt follow-up processing failed for database profile {Profile}", profile.Key);
                }
            }
            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
        }
    }
}
