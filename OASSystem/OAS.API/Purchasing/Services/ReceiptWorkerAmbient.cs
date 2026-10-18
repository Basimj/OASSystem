using System.Threading;

namespace OAS.API.Purchasing.Services;

internal static class ReceiptWorkerAmbient
{
    private static readonly AsyncLocal<string?> Profile = new();
    public static string? CurrentProfile => Profile.Value;
    public static IDisposable Use(string profile)
    {
        var previous = Profile.Value;
        Profile.Value = profile;
        return new Pop(() => Profile.Value = previous);
    }
    private sealed class Pop(Action action) : IDisposable { public void Dispose() => action(); }
}
