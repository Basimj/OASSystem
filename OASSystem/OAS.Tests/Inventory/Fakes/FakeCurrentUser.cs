using OAS.Application.Abstractions.Security;

namespace OAS.Tests.Inventory.Fakes;

public sealed class FakeCurrentUser(string? userId = "test-user-id", bool isAuthenticated = true) : ICurrentUser
{
    public string? UserId { get; } = userId;
    public bool IsAuthenticated { get; } = isAuthenticated;
}
