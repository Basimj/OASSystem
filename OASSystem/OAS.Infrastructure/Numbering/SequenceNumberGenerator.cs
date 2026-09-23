using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using OAS.Application.Abstractions.Numbering;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Numbering;

public sealed class SequenceNumberGenerator(
    OasDbContext dbContext) : ISequenceNumberGenerator
{
    public async Task<long> NextAsync(
        string sequenceName,
        CancellationToken cancellationToken = default)
    {
        var sql = GetSequenceSql(sequenceName);
        var connection = dbContext.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(cancellationToken);
        }

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            // The generator can run inside MediatR's TransactionBehavior. A raw
            // DbCommand created from the DbContext connection must explicitly
            // enlist in the current EF transaction or SqlClient rejects it.
            if (dbContext.Database.CurrentTransaction is { } currentTransaction)
            {
                command.Transaction = currentTransaction.GetDbTransaction();
            }

            var result = await command.ExecuteScalarAsync(cancellationToken);

            if (result is null || result == DBNull.Value)
            {
                throw new InvalidOperationException(
                    $"Sequence '{sequenceName}' did not return a value.");
            }

            return Convert.ToInt64(result);
        }
        finally
        {
            if (shouldCloseConnection)
            {
                await connection.CloseAsync();
            }
        }
    }

    private static string GetSequenceSql(string sequenceName)
    {
        if (sequenceName == "EmployeeNumberSequence")
        {
            return "SELECT NEXT VALUE FOR [core].[EmployeeNumberSequence];";
        }

        var explicitSequence = sequenceName switch
        {
            "CustomerCodeSequence" => "[dbo].[CustomerCodeSequence]",
            "SupplierCodeSequence" => "[dbo].[SupplierCodeSequence]",
            "CustomerAccountCodeSequence" => "[dbo].[CustomerAccountCodeSequence]",
            "SupplierAccountCodeSequence" => "[dbo].[SupplierAccountCodeSequence]",
            _ => null
        };

        if (explicitSequence is not null)
        {
            return $"SELECT NEXT VALUE FOR {explicitSequence};";
        }

        if (string.IsNullOrWhiteSpace(sequenceName) || !System.Text.RegularExpressions.Regex.IsMatch(sequenceName, @"^[a-zA-Z0-9_\-]+$"))
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceName), sequenceName, "Invalid sequence name.");
        }

        var sanitized = sequenceName.Replace("-", "_");

        // Inventory master-data codes use their own dbo sequences. Existing accounting
        // and inventory document-number sequences keep their historical schema/behavior.
        if (sequenceName.StartsWith("InventoryCode_", StringComparison.Ordinal))
        {
            return $"""
                DECLARE @seqName sysname = N'Seq_{sanitized}';
                IF NOT EXISTS (
                    SELECT 1 FROM sys.sequences
                    WHERE name = @seqName AND schema_id = SCHEMA_ID(N'dbo')
                )
                BEGIN
                    EXEC(N'CREATE SEQUENCE [dbo].[' + @seqName + N'] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
                END;
                DECLARE @sql nvarchar(500) = N'SELECT NEXT VALUE FOR [dbo].' + QUOTENAME(@seqName);
                EXEC sp_executesql @sql;
                """;
        }

        return $"""
            DECLARE @seqName sysname = N'Seq_{sanitized}';
            IF NOT EXISTS (
                SELECT 1 FROM sys.sequences 
                WHERE name = @seqName AND schema_id = SCHEMA_ID(N'accounting')
            )
            BEGIN
                EXEC(N'CREATE SEQUENCE [accounting].[' + @seqName + N'] AS BIGINT START WITH 1 INCREMENT BY 1 NO CYCLE;');
            END;
            DECLARE @sql nvarchar(500) = N'SELECT NEXT VALUE FOR [accounting].' + QUOTENAME(@seqName);
            EXEC sp_executesql @sql;
            """;
    }
}
