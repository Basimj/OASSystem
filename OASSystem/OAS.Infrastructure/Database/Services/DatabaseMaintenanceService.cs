using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OAS.Contracts.Database;
using OAS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OAS.Infrastructure.Database.Services;

public sealed class DatabaseMaintenanceService(DatabaseProfileCatalog catalog, ILogger<DatabaseMaintenanceService> logger)
{
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new(StringComparer.OrdinalIgnoreCase);

    public DatabaseProfilesResponse GetProfiles() => catalog.GetPublicProfiles();

    public async Task<DatabaseUpdateStatusDto> GetStatusAsync(string? profileKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolveTarget(profileKey, out var resolvedProfileKey, out var connectionString, out var resolutionError))
            return new DatabaseUpdateStatusDto(resolvedProfileKey, false, false, 0, resolutionError);

        var gate = Locks.GetOrAdd(connectionString, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = CreateContext(connectionString);
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            return new DatabaseUpdateStatusDto(resolvedProfileKey, true, pending.Length == 0, pending.Length);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var serverProbe = await ProbeServerAsync(connectionString, cancellationToken);
            if (!serverProbe.CanConnect)
            {
                logger.LogWarning(ex, "Unable to reach SQL Server for database profile {ProfileKey}. ErrorCode={ErrorCode}", resolvedProfileKey, serverProbe.ErrorCode);
                return new DatabaseUpdateStatusDto(resolvedProfileKey, false, false, 0, serverProbe.ErrorCode);
            }

            var databaseExists = await DatabaseExistsAsync(connectionString, cancellationToken);
            if (databaseExists == false)
            {
                await using var metadataDb = CreateContext(connectionString);
                var migrationCount = metadataDb.Database.GetMigrations().Count();
                logger.LogInformation(
                    "Database profile {ProfileKey} is reachable but the target database is not initialized; {PendingCount} migrations are available.",
                    resolvedProfileKey,
                    migrationCount);

                return new DatabaseUpdateStatusDto(
                    resolvedProfileKey,
                    true,
                    false,
                    migrationCount,
                    DatabaseErrorCodes.NotInitialized);
            }

            var errorCode = DatabaseSqlErrorClassifier.Classify(ex, DatabaseErrorCodes.StatusCheckFailed);
            logger.LogWarning(ex, "Unable to inspect database profile {ProfileKey}. ErrorCode={ErrorCode}", resolvedProfileKey, errorCode);
            return new DatabaseUpdateStatusDto(resolvedProfileKey, false, false, 0, errorCode);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<DatabaseUpdateStatusDto> UpdateAsync(string? profileKey, CancellationToken cancellationToken = default)
    {
        if (!TryResolveTarget(profileKey, out var resolvedProfileKey, out var connectionString, out var resolutionError))
            return new DatabaseUpdateStatusDto(resolvedProfileKey, false, false, 0, resolutionError);

        var gate = Locks.GetOrAdd(connectionString, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            await using var db = CreateContext(connectionString);
            logger.LogInformation("Applying OAS migrations to database profile {ProfileKey}.", resolvedProfileKey);
            await db.Database.MigrateAsync(cancellationToken);
            var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
            return new DatabaseUpdateStatusDto(resolvedProfileKey, true, pending.Length == 0, pending.Length);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var serverProbe = await ProbeServerAsync(connectionString, cancellationToken);
            var errorCode = serverProbe.CanConnect
                ? DatabaseSqlErrorClassifier.Classify(ex, DatabaseErrorCodes.UpdateFailed)
                : serverProbe.ErrorCode;

            logger.LogError(ex, "Database update failed for profile {ProfileKey}. ErrorCode={ErrorCode}", resolvedProfileKey, errorCode);
            return new DatabaseUpdateStatusDto(resolvedProfileKey, false, false, 0, errorCode);
        }
        finally
        {
            gate.Release();
        }
    }

    private bool TryResolveTarget(
        string? profileKey,
        out string resolvedProfileKey,
        out string connectionString,
        out string? errorCode)
    {
        resolvedProfileKey = string.IsNullOrWhiteSpace(profileKey) ? "Default" : profileKey.Trim();
        connectionString = string.Empty;
        errorCode = null;

        try
        {
            var profile = catalog.ResolveProfile(profileKey);
            resolvedProfileKey = profile.Key;
            connectionString = catalog.ResolveConnectionString(profile.Key);
            return true;
        }
        catch (Exception ex)
        {
            errorCode = DatabaseSqlErrorClassifier.Classify(ex, DatabaseErrorCodes.ConfigurationInvalid);
            logger.LogError(ex, "Unable to resolve database profile {ProfileKey}. ErrorCode={ErrorCode}", resolvedProfileKey, errorCode);
            return false;
        }
    }

    private static async Task<DatabaseServerProbeResult> ProbeServerAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(connectionString) { InitialCatalog = "master" };
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);
            return new DatabaseServerProbeResult(true, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            return new DatabaseServerProbeResult(
                false,
                DatabaseSqlErrorClassifier.Classify(ex, DatabaseErrorCodes.ServerUnavailable));
        }
    }

    private static async Task<bool?> DatabaseExistsAsync(string connectionString, CancellationToken cancellationToken)
    {
        try
        {
            var sourceBuilder = new SqlConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(sourceBuilder.InitialCatalog))
                return true;

            var targetDatabase = sourceBuilder.InitialCatalog;
            sourceBuilder.InitialCatalog = "master";

            await using var connection = new SqlConnection(sourceBuilder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT CASE WHEN DB_ID(@databaseName) IS NULL THEN 0 ELSE 1 END;";
            command.Parameters.Add(new SqlParameter("@databaseName", SqlDbType.NVarChar, 128) { Value = targetDatabase });

            var value = await command.ExecuteScalarAsync(cancellationToken);
            return Convert.ToInt32(value) == 1;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static OasDbContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer(connectionString)
            .ConfigureWarnings(warnings =>
            {
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning);
            })
            .Options;

        return new OasDbContext(options);
    }

    private sealed record DatabaseServerProbeResult(bool CanConnect, string? ErrorCode);
}
