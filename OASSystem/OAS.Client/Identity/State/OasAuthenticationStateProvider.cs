using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using OAS.Client.Identity.Services;
using OAS.Contracts.Identity.Authentication;

namespace OAS.Client.Identity.State;

public sealed class OasAuthenticationStateProvider(IAuthClientService authClientService) : AuthenticationStateProvider
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());
    private CurrentUserDto? _currentUser;
    private bool _initialized;

    public CurrentUserDto? CurrentUser => _currentUser;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        if (!_initialized)
        {
            try { _currentUser = await authClientService.GetCurrentUserAsync(); }
            catch (OAS.Client.Services.Http.ApiClientException) { _currentUser = null; }
            _initialized = true;
        }
        return new AuthenticationState(BuildPrincipal(_currentUser));
    }

    public void SetAuthenticated(CurrentUserDto user)
    {
        _currentUser = user;
        _initialized = true;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(BuildPrincipal(user))));
    }

    public void SetAnonymous()
    {
        _currentUser = null;
        _initialized = true;
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(Anonymous)));
    }

    private static ClaimsPrincipal BuildPrincipal(CurrentUserDto? user)
    {
        if (user is null || !user.IsAuthenticated) return Anonymous;
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new("display_name", user.DisplayName),
            new("is_super_admin", user.IsSuperAdmin ? "true" : "false"),
            new("must_change_password", user.MustChangePassword ? "true" : "false")
        };
        if (!string.IsNullOrWhiteSpace(user.Email)) claims.Add(new Claim(ClaimTypes.Email, user.Email));
        claims.AddRange(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, "OAS.Cookie"));
    }
}
