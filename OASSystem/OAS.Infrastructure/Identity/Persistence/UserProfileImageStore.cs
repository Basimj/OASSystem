using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;
using OAS.Application.Identity.Abstractions;
using OAS.Infrastructure.Persistence;

namespace OAS.Infrastructure.Identity.Persistence;

public sealed class UserProfileImageStore(OasDbContext dbContext) : IUserProfileImageStore
{
    public async Task<UserProfileImageData?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            EnlistCurrentTransaction(command);
            command.CommandText = "SELECT [ContentType],[ImageData],[UpdatedAtUtc] FROM [security].[UserProfileImages] WHERE [UserId]=@UserId";
            var p = command.CreateParameter(); p.ParameterName = "@UserId"; p.Value = userId; command.Parameters.Add(p);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            return new UserProfileImageData(userId, reader.GetString(0), (byte[])reader.GetValue(1), reader.GetFieldValue<DateTimeOffset>(2));
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    public async Task SaveAsync(UserProfileImageData image, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            EnlistCurrentTransaction(command);
            command.CommandText = @"
UPDATE [security].[UserProfileImages]
SET [ContentType]=@ContentType,[ImageData]=@ImageData,[FileSize]=@FileSize,[UpdatedAtUtc]=@UpdatedAtUtc
WHERE [UserId]=@UserId;
IF @@ROWCOUNT = 0
INSERT INTO [security].[UserProfileImages]([UserId],[ContentType],[ImageData],[FileSize],[UpdatedAtUtc])
VALUES(@UserId,@ContentType,@ImageData,@FileSize,@UpdatedAtUtc);";
            Add(command,"@UserId",image.UserId); Add(command,"@ContentType",image.ContentType); Add(command,"@ImageData",image.Content); Add(command,"@FileSize",image.Content.Length); Add(command,"@UpdatedAtUtc",image.UpdatedAtUtc);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);
        try
        {
            await using var command = connection.CreateCommand();
            EnlistCurrentTransaction(command);
            command.CommandText = "DELETE FROM [security].[UserProfileImages] WHERE [UserId]=@UserId";
            Add(command,"@UserId",userId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally { if (shouldClose) await connection.CloseAsync(); }
    }

    private void EnlistCurrentTransaction(System.Data.Common.DbCommand command)
    {
        var current = dbContext.Database.CurrentTransaction;
        if (current is not null)
            command.Transaction = current.GetDbTransaction();
    }

    private static void Add(System.Data.Common.DbCommand command, string name, object value)
    {
        var p=command.CreateParameter(); p.ParameterName=name; p.Value=value; command.Parameters.Add(p);
    }
}
