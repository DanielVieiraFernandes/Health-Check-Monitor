namespace HealthCheck.Web.Services.Authentication;

//======================================================================================================
// Modelo simples enviado do cliente para o endpoint de login.
//======================================================================================================
public record LoginRequest(string Email, string Password, string PreferredTheme);
