using OAS.Application.Common.Exceptions;
namespace OAS.Application.Features.Employees.Services;
internal static class HrOperationsHelpers
{
    public static void EnsureRowVersion(string rowVersion, byte[] current, string entityName)
    {
        byte[] incoming;
        try { incoming = Convert.FromBase64String(rowVersion); }
        catch (FormatException ex) { throw new ConcurrencyException($"Invalid row version for {entityName}. Reload and try again.", ex); }
        if (!incoming.SequenceEqual(current)) throw new ConcurrencyException($"{entityName} was changed by another operation. Reload it and try again.");
    }
    public static string? Actor(OAS.Application.Abstractions.Security.ICurrentUser user) => string.IsNullOrWhiteSpace(user.UserId) ? null : user.UserId;
}
