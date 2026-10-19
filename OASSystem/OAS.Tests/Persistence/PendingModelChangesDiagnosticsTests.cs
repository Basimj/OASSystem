using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using NUnit.Framework;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Persistence;

/// <summary>
/// EF Core 9 schema drift diagnostic. Does not open a SQL connection or modify any database.
/// Reports the precise relational operations that differ between the last snapshot
/// and the design-time OasDbContext model. Do not suppress PendingModelChangesWarning.
/// </summary>
[TestFixture]
[Category("MigrationDiagnostics")]
public sealed class PendingModelChangesDiagnosticsTests
{
    [Test]
    public void Snapshot_ShouldHaveNoPendingRelationalChanges()
    {
        // EF needs a provider, but no database is ever opened. This is an intentionally
        // dummy database name, not the user's DefaultConnection or production database.
        var options = new DbContextOptionsBuilder<OasDbContext>()
            .UseSqlServer("Server=.;Database=OAS_DIAGNOSTIC_NEVER_CONNECT;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        using var db = new OasDbContext(options);

        var migrations = db.GetService<IMigrationsAssembly>();
        var snapshot = migrations.ModelSnapshot?.Model
            ?? throw new InvalidOperationException("There is no EF Core model snapshot.");

        // Mirror the EF Core 9 migrator's pending-change detection, but print the
        // operations rather than only throwing PendingModelChangesWarning.
        var initializedSnapshot = db.GetService<IModelRuntimeInitializer>()
            .Initialize(snapshot, designTime: true);
        var current = db.GetService<IDesignTimeModel>().Model;
        var differences = db.GetService<IMigrationsModelDiffer>()
            .GetDifferences(initializedSnapshot.GetRelationalModel(), current.GetRelationalModel());

        TestContext.Progress.WriteLine($"EF CORE MODEL DIFF: {differences.Count} operations");
        foreach (var (operation, index) in differences.Select((operation, index) => (operation, index)))
        {
            TestContext.Progress.WriteLine($"{index + 1,3}. {Describe(operation)}");
        }

        Assert.That(differences, Is.Empty,
            "Current OasDbContext model differs from the last migration snapshot. " +
            "Review the EF CORE MODEL DIFF lines above before adding a migration. " +
            "Do not disable PendingModelChangesWarning.");
    }

    private static string Describe(MigrationOperation operation)
    {
        var fields = new[]
        {
            "Schema", "Table", "Name", "NewName", "NewSchema", "PrincipalSchema",
            "PrincipalTable", "ColumnType", "ClrType", "IsNullable", "IsUnique",
            "MaxLength", "DefaultValue", "DefaultValueSql", "ComputedColumnSql"
        };
        var values = fields.Select(field =>
        {
            var prop = operation.GetType().GetProperty(field);
            if (prop is null) return null;
            var value = prop.GetValue(operation);
            return value is null ? null : $"{field}={value}";
        }).Where(x => x is not null);
        return $"{operation.GetType().Name}: {string.Join(", ", values)}";
    }
}
