using System.Collections.Concurrent;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OAS.Contracts.Database;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Database.Services;

public sealed class DatabaseMaintenanceService(DatabaseProfileCatalog catalog, ILogger<DatabaseMaintenanceService> logger)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);

    public DatabaseProfilesResponse GetProfiles() => catalog.GetPublicProfiles();

    public async Task<DatabaseUpdateStatusDto> GetStatusAsync(string? profileKey, CancellationToken cancellationToken = default)
    {
        var profile = catalog.ResolveProfile(profileKey);
        var connectionString = catalog.ResolveConnectionString(profile.Key);
        var gate = Locks.GetOrAdd(connectionString, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = CreateContext(connectionString);
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            return new DatabaseUpdateStatusDto(profile.Key, true, pending.Length == 0, pending.Length);
        }
        catch (Exception ex)
        {
            if (await CanConnectToServerAsync(connectionString, cancellationToken))
            {
                await using var metadataDb = CreateContext(connectionString);
                var migrationCount = metadataDb.Database.GetMigrations().Count();
                logger.LogInformation("Database profile {ProfileKey} is reachable but not initialized; {PendingCount} migrations are available.", profile.Key, migrationCount);
                return new DatabaseUpdateStatusDto(profile.Key, true, false, migrationCount, "database_not_initialized");
            }
            logger.LogWarning(ex, "Unable to inspect database profile {ProfileKey}.", profile.Key);
            return new DatabaseUpdateStatusDto(profile.Key, false, false, 0, "database_unavailable");
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DatabaseUpdateStatusDto> UpdateAsync(string? profileKey, CancellationToken cancellationToken = default)
    {
        var profile = catalog.ResolveProfile(profileKey);
        var connectionString = catalog.ResolveConnectionString(profile.Key);
        var gate = Locks.GetOrAdd(connectionString, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = CreateContext(connectionString);
            logger.LogInformation("Applying OAS migrations to database profile {ProfileKey}.", profile.Key);
            await db.Database.MigrateAsync(cancellationToken);
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            return new DatabaseUpdateStatusDto(profile.Key, true, pending.Length == 0, pending.Length);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database update failed for profile {ProfileKey}.", profile.Key);
            throw;
        }
        finally
        {
            gate.Release();
        }
    }

    private static async Task<bool> CanConnectToServerAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return true;
        }
        catch { return false; }
    }

    private static OasDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new OasDbContext(options);
    }
}
