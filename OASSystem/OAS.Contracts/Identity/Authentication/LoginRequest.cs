namespace OAS.Contracts.Identity.Authentication;

public sealed record LoginRequest(string Login, string Password, bool RememberMe = false);
