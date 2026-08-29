namespace OAS.Contracts.Database;

public static class DatabaseErrorCodes
{
    public const string NotInitialized = "database_not_initialized";
    public const string ServerUnavailable = "database_server_unavailable";
    public const string CertificateError = "database_certificate_error";
    public const string AuthenticationFailed = "database_authentication_failed";
    public const string AccessDenied = "database_access_denied";
    public const string ConfigurationInvalid = "database_configuration_invalid";
    public const string StatusCheckFailed = "database_status_check_failed";
    public const string UpdateFailed = "database_update_failed";
    public const string Unavailable = "database_unavailable";
}
