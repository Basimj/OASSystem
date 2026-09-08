
using System.Data;
using Microsoft.EntityFrameworkCore;
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

        var connection =
            dbContext.Database.GetDbConnection();

        var shouldCloseConnection =
            connection.State != ConnectionState.Open;

        if (shouldCloseConnection)
        {
            await connection.OpenAsync(
                cancellationToken);
        }

        try
        {
            await using var command =
                connection.CreateCommand();

            command.CommandText = sql;
            command.CommandType = CommandType.Text;

            var result =
                await command.ExecuteScalarAsync(
                    cancellationToken);

            if (result is null ||
                result == DBNull.Value)
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

    private static string GetSequenceSql(
        string sequenceName)
    {
        return sequenceName switch
        {
            "EmployeeNumberSequence" =>
                "SELECT NEXT VALUE FOR [core].[EmployeeNumberSequence];",

            _ => throw new ArgumentOutOfRangeException(
                nameof(sequenceName),
                sequenceName,
                "Unknown sequence name.")
        };
    }
}
