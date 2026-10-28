using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace MafaliCrm.Web.Services;

public class CurrentUserService
{
    private readonly AuthenticationStateProvider _authStateProvider;

    public CurrentUserService(AuthenticationStateProvider authStateProvider)
    {
        _authStateProvider = authStateProvider;
    }

    private async Task<ClaimsPrincipal> GetUserAsync()
    {
        var state = await _authStateProvider.GetAuthenticationStateAsync();
        return state.User;
    }

    public async Task<string?> GetLoginAsync() => (await GetUserAsync()).Identity?.Name;

    public async Task<string?> GetRoleAsync() => (await GetUserAsync()).FindFirst(ClaimTypes.Role)?.Value;

    public async Task<bool> CanViewAllFiltersAsync() => await GetRoleAsync() is "admin" or "assistant";

    public async Task<bool> CanCreateFiltersAsync() => await GetRoleAsync() == "admin";

    public async Task<bool> IsAdminAsync() => await GetRoleAsync() == "admin";
}
