namespace HealthCheck.Web.Services.Authentication;

//======================================================================================================
// Resultado simples para comunicar sucesso/erro no login.
//======================================================================================================
public record AuthSignInResult(bool IsSuccess, string? ErrorMessage = null)
{
    public static AuthSignInResult Success() => new(true);
    public static AuthSignInResult Failure(string message) => new(false, message);
}
