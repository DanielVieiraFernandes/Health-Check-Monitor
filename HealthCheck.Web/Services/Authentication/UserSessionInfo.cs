namespace HealthCheck.Web.Services.Authentication;

//======================================================================================================
// Estrutura simples para expor dados da sessão para a interface (Claims -> ViewModel de sessão).
//======================================================================================================
public record UserSessionInfo(
    Guid UserId,
    string UserName,
    string Email,
    DateTime LoggedAt,
    ThemeOptions PreferredTheme
);
