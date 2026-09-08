namespace OAS.Domain.Identity.Constants;

public static class IdentityBootstrap
{
    public static readonly Guid SuperAdminId = Guid.Parse("8F4A57C8-40A6-45B7-8C7B-C2264FA07999");

    // This is a non-secret sentinel used only while the initial Super Admin has no password yet.
    // It is deliberately not a valid ASP.NET Identity password hash.
    public const string NoPasswordHash = "OAS_BOOTSTRAP_NO_PASSWORD_V1";
}
