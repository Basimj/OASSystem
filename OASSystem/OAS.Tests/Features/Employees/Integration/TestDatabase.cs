using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Features.Employees.Integration;

public static class TestDatabase
{
    private const string DatabaseName = "OASSystem_EmployeesTests";

    private const string MasterConnectionString =
        "Data Source=.\\SQLEXPRESS;Initial Catalog=master;Integrated Security=True;Trust Server Certificate=True";

    public static string ConnectionString =>
        $"Data Source=.\\SQLEXPRESS;Initial Catalog={DatabaseName};Integrated Security=True;Trust Server Certificate=True";

    public static async Task EnsureCreatedAndMigratedAsync()
    {
        await using var master = new SqlConnection(MasterConnectionString);

        await master.OpenAsync();

        await using (var command = master.CreateCommand())
        {
            command.CommandText = $"""
IF DB_ID(N'{DatabaseName}') IS NULL
BEGIN
    CREATE DATABASE [{DatabaseName}];
END
""";

            await command.ExecuteNonQueryAsync();
        }

        var options = new DbContextOptionsBuilder<OasDbContext>()
    .UseSqlServer(ConnectionString)
    .ConfigureWarnings(warnings =>
        warnings.Ignore(
            Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId
                .PendingModelChangesWarning))
    .Options;

        await using var db = new OasDbContext(options);

        await db.Database.MigrateAsync();
    }
}