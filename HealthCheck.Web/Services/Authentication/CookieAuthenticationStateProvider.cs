using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace HealthCheck.Web.Services.Authentication;

public class CookieAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
    : AuthenticationStateProvider
{
    //------------------------------------------------------------------------------------------------------
    // Blazor Server lê o usuário autenticado a partir do HttpContext, que é preenchido pelo cookie.
    // Esse provider mantém o AuthenticationState sincronizado com o cookie.
    //------------------------------------------------------------------------------------------------------
    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        //--------------------------------------------------------------------------------------------------
        // O HttpContext.User reflete o cookie enviado pelo navegador. Quando o cookie expira,
        // o usuário volta a ser anônimo.
        //--------------------------------------------------------------------------------------------------
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        return Task.FromResult(new AuthenticationState(user));
    }

    public void NotifyAuthenticationStateChanged()
    {
        //--------------------------------------------------------------------------------------------------
        // Força o Blazor a reavaliar o estado de autenticação após login/logout.
        //--------------------------------------------------------------------------------------------------
        var httpContext = httpContextAccessor.HttpContext;
        var user = httpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());
        NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(user)));
    }
}
