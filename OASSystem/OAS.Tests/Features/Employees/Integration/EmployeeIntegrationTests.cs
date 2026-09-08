
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
using OAS.Domain.Features.Employees.Entities;
using OAS.Infrastructure.Persistence;

namespace OAS.Tests.Features.Employees.Integration;

[TestFixture]
public sealed class EmployeeIntegrationTests
{
    [OneTimeSetUp]
    public async Task Setup()
    {
        await TestDatabase.EnsureCreatedAndMigratedAsync();
    }

    [Test]
    public async Task Migration_IsDiscoverable()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
SELECT COUNT(*)
FROM [OASSystem_EmployeesTests].[dbo].[__EFMigrationsHistory]
WHERE [MigrationId] = '20260831145400_EmployeesInitial'
""";

        await using var command = new SqlCommand(sql, connection);

        var count = Convert.ToInt32(
            await command.ExecuteScalarAsync());

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Migration_CreatesHrSchema()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
SELECT COUNT(*)
FROM sys.schemas
WHERE name = N'hr'
""";

        await using var command = new SqlCommand(sql, connection);

        var count = Convert.ToInt32(
            await command.ExecuteScalarAsync());

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task Migration_CreatesEmployeesTable()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
SELECT COUNT(*)
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_SCHEMA = N'hr'
  AND TABLE_NAME = N'Employees'
""";

        await using var command = new SqlCommand(sql, connection);

        var count = Convert.ToInt32(
            await command.ExecuteScalarAsync());

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task EmployeePrimaryKey_IsUniqueIdentifier()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
SELECT DATA_TYPE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = N'hr'
  AND TABLE_NAME = N'Employees'
  AND COLUMN_NAME = N'Id'
""";

        await using var command = new SqlCommand(sql, connection);

        var type = Convert.ToString(
            await command.ExecuteScalarAsync());

        Assert.That(type, Is.EqualTo("uniqueidentifier"));
    }

    [Test]
    public async Task EmployeeCode_HasUniqueIndex()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
SELECT COUNT(*)
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
  AND name = N'UX_Employees_NormalizedEmployeeCode'
  AND is_unique = 1
""";

        await using var command = new SqlCommand(sql, connection);

        var count = Convert.ToInt32(
            await command.ExecuteScalarAsync());

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task UserAccountId_HasFilteredUniqueIndex()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
SELECT filter_definition
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'[hr].[Employees]')
  AND name = N'UX_Employees_UserAccountId'
  AND is_unique = 1
""";

        await using var command = new SqlCommand(sql, connection);

        var filter = Convert.ToString(
            await command.ExecuteScalarAsync());

        Assert.That(
            filter,
            Does.Contain("[UserAccountId] IS NOT NULL"));
    }

    [Test]
    public async Task Employee_HasUserForeignKey()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        const string sql = """
SELECT COUNT(*)
FROM sys.foreign_keys
WHERE name = N'FK_Employees_Users_UserAccountId'
  AND parent_object_id = OBJECT_ID(N'[hr].[Employees]')
""";

        await using var command = new SqlCommand(sql, connection);

        var count = Convert.ToInt32(
            await command.ExecuteScalarAsync());

        Assert.That(count, Is.EqualTo(1));
    }

    [Test]
    public async Task UserDelete_SetsEmployeeUserAccountIdToNull()
    {
        var userId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();

        var employeeCode =
            $"T{Guid.NewGuid():N}"[..10];

        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        try
        {
            await using (var command = connection.CreateCommand())
            {
                command.Transaction = (SqlTransaction)transaction;

                command.CommandText = """
INSERT INTO [security].[Users]
(
    [Id],
    [UserName],
    [NormalizedUserName],
    [FirstName],
    [LastName],
    [Email],
    [NormalizedEmail],
    [PasswordHash],
    [IsActive],
    [IsSuperAdmin],
    [MustChangePassword],
    [AccessFailedCount],
    [CreatedAtUtc]
)
VALUES
(
    @Id,
    N'test-user',
    N'TEST-USER',
    N'Test',
    N'User',
    NULL,
    NULL,
    N'test',
    1,
    0,
    0,
    0,
    SYSUTCDATETIME()
);
""";

                command.Parameters.AddWithValue("@Id", userId);

                await command.ExecuteNonQueryAsync();
            }

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = (SqlTransaction)transaction;

                command.CommandText = """
INSERT INTO [hr].[Employees]
(
    [Id],
    [EmployeeCode],
    [NormalizedEmployeeCode],
    [FirstName],
    [LastName],
    [IsSalesperson],
    [IsTechnician],
    [IsCommissionEligible],
    [IsActive],
    [UserAccountId],
    [CreatedAtUtc]
)
VALUES
(
    @Id,
    @EmployeeCode,
    @EmployeeCode,
    N'Test',
    N'Employee',
    0,
    0,
    0,
    1,
    @UserId,
    SYSUTCDATETIME()
);
""";

                command.Parameters.AddWithValue(
                    "@Id",
                    employeeId);

                command.Parameters.AddWithValue(
                    "@EmployeeCode",
                    employeeCode);

                command.Parameters.AddWithValue(
                    "@UserId",
                    userId);

                await command.ExecuteNonQueryAsync();
            }

            await using (var command = connection.CreateCommand())
            {
                command.Transaction = (SqlTransaction)transaction;

                command.CommandText =
                    "DELETE FROM [security].[Users] WHERE [Id] = @Id";

                command.Parameters.AddWithValue("@Id", userId);

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }

        await using var verify =
            new SqlConnection(TestDatabase.ConnectionString);

        await verify.OpenAsync();

        await using var verifyCommand =
            verify.CreateCommand();

        verifyCommand.CommandText = """
SELECT [UserAccountId]
FROM [hr].[Employees]
WHERE [Id] = @Id
""";

        verifyCommand.Parameters.AddWithValue(
            "@Id",
            employeeId);

        var value =
            await verifyCommand.ExecuteScalarAsync();

        Assert.That(value, Is.EqualTo(DBNull.Value));
    }

    [Test]
    public async Task RowVersion_IsGeneratedBySqlServer()
    {
        var employee = Employee.Create(
            Guid.NewGuid(),
            $"RV-{Guid.NewGuid():N}"[..10],
            "Test",
            "Employee",
            null,
            null,
            null,
            null,
            false,
            false,
            false,
            true,
            null);

        var options =
            new DbContextOptionsBuilder<OasDbContext>()
                .UseSqlServer(TestDatabase.ConnectionString)
                .Options;

        await using var db = new OasDbContext(options);

        await InsertEmployeeAsync(db, employee);

        var firstVersion =
            employee.RowVersion.ToArray();

        employee.UpdateDetails(
            employee.EmployeeCode,
            "Changed",
            employee.LastName,
            employee.Phone,
            employee.JobTitle,
            employee.HireDate,
            employee.Notes);

        await db.SaveChangesAsync();

        var secondVersion =
            employee.RowVersion.ToArray();

        Assert.That(
            secondVersion,
            Is.Not.EqualTo(firstVersion));
    }

    [Test]
    public async Task OasDbContext_Employees_Works()
    {
        var options =
            new DbContextOptionsBuilder<OasDbContext>()
                .UseSqlServer(TestDatabase.ConnectionString)
                .Options;

        await using var db = new OasDbContext(options);

        var count =
            await db.Employees.CountAsync();

        Assert.That(count, Is.GreaterThanOrEqualTo(0));
    }

    private static async Task InsertEmployeeAsync(
        OasDbContext db,
        Employee employee)
    {
        db.Employees.Add(employee);

        await db.SaveChangesAsync();
    }
}

