using HealthCheck.Framework.Services.Database.UsersService;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace HealthCheck.Web.Services.Authentication;

public class AuthenticationService(
    UsersService usersService,
    IHttpContextAccessor httpContextAccessor,
    IOptions<UserSessionSettings> sessionSettings)
{
    //======================================================================================================
    // ANTI BRUTE-FORCE: dicionário em memória para rastrear tentativas por IP
    //======================================================================================================
    private static readonly ConcurrentDictionary<string, (int attempts, DateTime lockedUntil)> _loginAttempts = new();
    private const int MaxLoginAttempts = 5;
    private const int LockoutMinutes = 15;
    //======================================================================================================
    // PONTO CENTRAL DA AUTENTICAÇÃO: valida usuário/senha no banco e prepara o cookie com as claims.
    //======================================================================================================
    //------------------------------------------------------------------------------------------------------
    // Responsável por autenticar o usuário usando email e senha.
    // Caso esteja válido, cria o cookie de autenticação com as claims principais.
    //------------------------------------------------------------------------------------------------------
    public async Task<AuthSignInResult> SignInAsync(string email, string password, string preferredTheme)
    {
        //--------------------------------------------------------------------------------------------------
        // 1) Valida credenciais no serviço de usuários.
        // 2) Monta as claims que representam o usuário na aplicação.
        // 3) Define tempo de expiração da sessão.
        // 4) Escreve o cookie (precisa ocorrer antes de a resposta começar).
        //--------------------------------------------------------------------------------------------------
        var authResult = await usersService.AuthenticateUser(email, password);

        if (authResult.IsFailure || authResult.Success == null)
            return AuthSignInResult.Failure("Credenciais inválidas");

        var user = authResult.Success;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Email, user.Email),
            new("preferred_theme", preferredTheme),
            new("logged_at", DateTime.UtcNow.ToString("O"))
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            IssuedUtc = DateTime.UtcNow,
            ExpiresUtc = DateTime.UtcNow.Add(sessionSettings.Value.SessionDuration)
        };

        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return AuthSignInResult.Failure("Falha ao iniciar sessão");

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

        return AuthSignInResult.Success();
    }

    //------------------------------------------------------------------------------------------------------
    // Endpoint HTTP usado para gravar o cookie de autenticação antes da resposta iniciar.
    // Isso evita erros de headers já enviados no fluxo interativo do Blazor Server.
    //------------------------------------------------------------------------------------------------------
    public async Task<IResult> SignInHttpAsync(LoginRequest request)
    {
        //----------------------------------------------------------------------------------------------
        // ANTI BRUTE-FORCE: verifica se o IP está bloqueado por excesso de tentativas
        //----------------------------------------------------------------------------------------------
        var remoteIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

        if (!string.IsNullOrEmpty(remoteIp))
        {
            if (_loginAttempts.TryGetValue(remoteIp, out var entry) && entry.lockedUntil > DateTime.UtcNow)
            {
                var remainingMinutes = (int)(entry.lockedUntil - DateTime.UtcNow).TotalMinutes + 1;
                return Results.StatusCode(429);
            }
        }

        var authResult = await SignInAsync(request.Email, request.Password, request.PreferredTheme);

        if (!authResult.IsSuccess)
        {
            //------------------------------------------------------------------------------------------
            // Incrementa contador de tentativas para o IP
            //------------------------------------------------------------------------------------------
            if (!string.IsNullOrEmpty(remoteIp))
            {
                _loginAttempts.AddOrUpdate(remoteIp,
                    _ => (1, DateTime.MinValue),
                    (_, existing) => existing.attempts + 1 >= MaxLoginAttempts
                        ? (0, DateTime.UtcNow.AddMinutes(LockoutMinutes))
                        : (existing.attempts + 1, DateTime.MinValue));
            }

            return Results.BadRequest(authResult.ErrorMessage);
        }

        // Login bem-sucedido: remove o IP do cache de tentativas
        if (!string.IsNullOrEmpty(remoteIp))
            _loginAttempts.TryRemove(remoteIp, out _);

        return Results.Ok();
    }

    //------------------------------------------------------------------------------------------------------
    // Realiza o logout removendo o cookie de autenticação.
    //------------------------------------------------------------------------------------------------------
    public async Task SignOutAsync()
    {
        //--------------------------------------------------------------------------------------------------
        // Remove o cookie usando o middleware de autenticação.
        //--------------------------------------------------------------------------------------------------
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return;

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    //------------------------------------------------------------------------------------------------------
    // Endpoint HTTP usado para remover o cookie de autenticação antes da resposta iniciar.
    //------------------------------------------------------------------------------------------------------
    public async Task<IResult> SignOutHttpAsync()
    {
        //--------------------------------------------------------------------------------------------------
        // Logout via endpoint HTTP, garantindo que o navegador receba o Set-Cookie para expirar.
        //--------------------------------------------------------------------------------------------------
        var httpContext = httpContextAccessor.HttpContext;

        if (httpContext == null)
            return Results.BadRequest("Falha ao encerrar sessão");

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        return Results.Ok();
    }

    //------------------------------------------------------------------------------------------------------
    // Obtém informações da sessão atual a partir das claims.
    //------------------------------------------------------------------------------------------------------
    public UserSessionInfo? GetSessionInfo(ClaimsPrincipal? user)
    {
        // Se o usuário não estiver autenticado, retorna null
        if (user?.Identity?.IsAuthenticated == false)
            return null;

        var userIdValue = user!.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = user.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
        var email = user.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var theme = user.FindFirst("preferred_theme")?.Value ?? "dark";
        var loggedAtValue = user.FindFirst("logged_at")?.Value ?? DateTime.UtcNow.ToString("O");

        if (!Guid.TryParse(userIdValue, out var userId))
            return null;

        return new UserSessionInfo(userId, userName, email, DateTime.Parse(loggedAtValue), Enum.Parse<ThemeOptions>(theme, true));
    }
}
