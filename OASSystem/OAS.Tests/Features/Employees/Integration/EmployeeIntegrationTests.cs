using Microsoft.Data.SqlClient;
using NUnit.Framework;

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
    public async Task EmployeeRelationalAndContactMigrations_AreApplied()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                """
                SELECT COUNT(*)
                FROM [dbo].[__EFMigrationsHistory]
                WHERE [MigrationId] IN
                (
                    '20260909211500_EmployeeRelationalRefactor',
                    '20260910170000_EmployeeContactInfo'
                )
                """,
                connection);

        Assert.That(
            Convert.ToInt32(await command.ExecuteScalarAsync()),
            Is.EqualTo(2));
    }

    [Test]
    public async Task JobTitlesTable_AndEmployeeForeignKey_Exist()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                """
                SELECT
                    CASE
                        WHEN OBJECT_ID(
                            N'[hr].[JobTitles]',
                            N'U') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END
                    +
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM sys.foreign_keys
                            WHERE parent_object_id =
                                  OBJECT_ID(N'[hr].[Employees]')
                              AND name =
                                  N'FK_Employees_JobTitles_JobTitleId'
                        )
                        THEN 1
                        ELSE 0
                    END
                """,
                connection);

        Assert.That(
            Convert.ToInt32(await command.ExecuteScalarAsync()),
            Is.EqualTo(2));
    }

    [Test]
    public async Task EmployeeCode_HasUniqueIndex_AndLegacyNumericColumnsAreGone()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                """
                SELECT
                    CASE
                        WHEN EXISTS
                        (
                            SELECT 1
                            FROM sys.indexes
                            WHERE object_id =
                                  OBJECT_ID(N'[hr].[Employees]')
                              AND name =
                                  N'UX_Employees_EmployeeCode'
                              AND is_unique = 1
                        )
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'EmployeeNumber') IS NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'NormalizedEmployeeCode') IS NULL
                        THEN 1
                        ELSE 0
                    END
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        Assert.That(
            await reader.ReadAsync(),
            Is.True);

        Assert.Multiple(() =>
        {
            Assert.That(
                reader.GetInt32(0),
                Is.EqualTo(1),
                "UX_Employees_EmployeeCode must exist and be unique.");

            Assert.That(
                reader.GetInt32(1),
                Is.EqualTo(1),
                "EmployeeNumber must not exist.");

            Assert.That(
                reader.GetInt32(2),
                Is.EqualTo(1),
                "NormalizedEmployeeCode must not exist.");
        });
    }

    [Test]
    public async Task EmployeeContactColumns_ExistAndLegacyImageTableIsRemoved()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                """
                SELECT
                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'Phone') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'Email') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'Country') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'Governorate') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'City') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'PostalCode') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'ResidentialAddress') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN COL_LENGTH(
                            N'[hr].[Employees]',
                            N'Photo') IS NOT NULL
                        THEN 1
                        ELSE 0
                    END,

                    CASE
                        WHEN OBJECT_ID(
                            N'[hr].[EmployeeImages]',
                            N'U') IS NULL
                        THEN 1
                        ELSE 0
                    END
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        Assert.That(
            await reader.ReadAsync(),
            Is.True);

        for (var ordinal = 0; ordinal < 9; ordinal++)
        {
            Assert.That(
                reader.GetInt32(ordinal),
                Is.EqualTo(1),
                $"Expected schema condition at ordinal {ordinal} to be true.");
        }
    }

    [Test]
    public async Task DatabaseSequence_GeneratesDistinctEmployeeNumbers()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                """
                DECLARE @First bigint =
                    NEXT VALUE FOR [core].[EmployeeNumberSequence];

                DECLARE @Second bigint =
                    NEXT VALUE FOR [core].[EmployeeNumberSequence];

                SELECT @First, @Second;
                """,
                connection);

        await using var reader =
            await command.ExecuteReaderAsync();

        Assert.That(
            await reader.ReadAsync(),
            Is.True);

        var first =
            reader.GetInt64(0);

        var second =
            reader.GetInt64(1);

        Assert.Multiple(() =>
        {
            Assert.That(
                first,
                Is.GreaterThan(0));

            Assert.That(
                second,
                Is.GreaterThan(first));
        });
    }

    [Test]
    public async Task LinkedEmployee_PreventsDeletingReferencedUser()
    {
        await using var connection =
            new SqlConnection(TestDatabase.ConnectionString);

        await connection.OpenAsync();

        await using var transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            var userId = Guid.NewGuid();
            var employeeId = Guid.NewGuid();

            /*
             * Use an existing active JobTitle from the test database.
             * This avoids relying on a random Guid that cannot satisfy
             * FK_Employees_JobTitles_JobTitleId.
             */
            var titleId =
                await GetActiveJobTitleIdAsync(
                    connection,
                    transaction);

            Assert.That(
                titleId,
                Is.Not.EqualTo(Guid.Empty),
                "The test database must contain at least one active job title.");

            var userName =
                $"test-linked-{Guid.NewGuid():N}";

            await ExecuteAsync(
                connection,
                transaction,
                """
                INSERT INTO [security].[Users]
                (
                    [Id],
                    [UserName],
                    [NormalizedUserName],
                    [FirstName],
                    [LastName],
                    [PasswordHash],
                    [IsActive],
                    [IsSuperAdmin],
                    [MustChangePassword],
                    [AccessFailedCount],
                    [CreatedAtUtc]
                )
                VALUES
                (
                    @UserId,
                    @UserName,
                    @NormalizedUserName,
                    N'Test',
                    N'User',
                    N'test-password-hash',
                    1,
                    0,
                    0,
                    0,
                    SYSUTCDATETIME()
                );
                """,
                new SqlParameter(
                    "@UserId",
                    userId),
                new SqlParameter(
                    "@UserName",
                    userName),
                new SqlParameter(
                    "@NormalizedUserName",
                    userName.ToUpperInvariant()));

            await ExecuteAsync(
                connection,
                transaction,
                """
                INSERT INTO [hr].[Employees]
                (
                    [Id],
                    [EmployeeCode],
                    [FirstName],
                    [LastName],
                    [JobTitleId],
                    [IsCommissionEligible],
                    [IsActive],
                    [UserAccountId],
                    [CreatedAtUtc]
                )
                VALUES
                (
                    @EmployeeId,
                    @EmployeeCode,
                    N'Test',
                    N'Employee',
                    @TitleId,
                    0,
                    1,
                    @UserId,
                    SYSUTCDATETIME()
                );
                """,
                new SqlParameter(
                    "@EmployeeId",
                    employeeId),
                new SqlParameter(
                    "@EmployeeCode",
                    $"EMP-{Random.Shared.Next(10000, 99999):D5}"),
                new SqlParameter(
                    "@TitleId",
                    titleId),
                new SqlParameter(
                    "@UserId",
                    userId));

            await using var delete =
                new SqlCommand(
                    """
                    DELETE FROM [security].[Users]
                    WHERE [Id] = @UserId;
                    """,
                    connection,
                    transaction);

            delete.Parameters.Add(
                new SqlParameter(
                    "@UserId",
                    userId));

            Assert.ThrowsAsync<SqlException>(
                () => delete.ExecuteNonQueryAsync());
        }
        finally
        {
            await transaction.RollbackAsync();
        }
    }

    private static async Task<Guid> GetActiveJobTitleIdAsync(
        SqlConnection connection,
        SqlTransaction transaction)
    {
        await using var command =
            new SqlCommand(
                """
                SELECT TOP (1) [Id]
                FROM [hr].[JobTitles]
                WHERE [IsActive] = 1
                ORDER BY [Name], [Id];
                """,
                connection,
                transaction);

        var result =
            await command.ExecuteScalarAsync();

        return result is Guid id
            ? id
            : Guid.Empty;
    }

    private static async Task ExecuteAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        params SqlParameter[] parameters)
    {
        await using var command =
            new SqlCommand(
                sql,
                connection,
                transaction);

        command.Parameters.AddRange(parameters);

        await command.ExecuteNonQueryAsync();
    }
}