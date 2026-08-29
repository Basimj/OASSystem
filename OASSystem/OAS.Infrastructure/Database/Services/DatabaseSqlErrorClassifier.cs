using Microsoft.Data.SqlClient;
using OAS.Contracts.Database;

namespace OAS.Infrastructure.Database.Services;

internal static class DatabaseSqlErrorClassifier
{
    private static readonly int[] AuthenticationErrorNumbers = [18456];
    private static readonly int[] AccessDeniedErrorNumbers = [229, 4060, 916];
    private static readonly int[] ServerUnavailableErrorNumbers = [-2, 2, 53, 258, 11001];

    public static string Classify(Exception exception, string fallbackCode)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var sqlException = FindException<SqlException>(exception);
        if (sqlException is not null)
        {
            if (AuthenticationErrorNumbers.Contains(sqlException.Number))
                return DatabaseErrorCodes.AuthenticationFailed;

            if (AccessDeniedErrorNumbers.Contains(sqlException.Number))
                return DatabaseErrorCodes.AccessDenied;

            if (ServerUnavailableErrorNumbers.Contains(sqlException.Number))
                return DatabaseErrorCodes.ServerUnavailable;
        }

        var message = FlattenMessages(exception);

        if (ContainsAny(message,
                "certificate chain",
                "certificate validation",
                "ssl provider",
                "tls",
                "certificate"))
            return DatabaseErrorCodes.CertificateError;

        if (ContainsAny(message,
                "login failed",
                "authentication failed"))
            return DatabaseErrorCodes.AuthenticationFailed;

        if (ContainsAny(message,
                "permission was denied",
                "permission denied",
                "not able to access the database",
                "cannot open database"))
            return DatabaseErrorCodes.AccessDenied;

        if (ContainsAny(message,
                "server was not found",
                "network-related",
                "instance-specific",
                "error: 40",
                "actively refused",
                "no such host is known",
                "timed out"))
            return DatabaseErrorCodes.ServerUnavailable;

        if (exception is InvalidOperationException && ContainsAny(message, "connection string", "database profile"))
            return DatabaseErrorCodes.ConfigurationInvalid;

        return fallbackCode;
    }

    private static TException? FindException<TException>(Exception exception)
        where TException : Exception
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TException typed)
                return typed;
        }

        return null;
    }

    private static string FlattenMessages(Exception exception)
    {
        var messages = new List<string>();
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (!string.IsNullOrWhiteSpace(current.Message))
                messages.Add(current.Message);
        }

        return string.Join(" | ", messages);
    }

    private static bool ContainsAny(string value, params string[] fragments) =>
        fragments.Any(fragment => value.Contains(fragment, StringComparison.OrdinalIgnoreCase));
}
