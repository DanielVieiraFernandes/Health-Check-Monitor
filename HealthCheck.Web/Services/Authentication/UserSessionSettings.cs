namespace HealthCheck.Web.Services.Authentication;

public class UserSessionSettings
{
    //==============================================================================================
    // Configuração central do tempo de sessão (lida do appsettings).
    //==============================================================================================
    // Define quanto tempo a sessão do usuário será válida.
    // Esse valor poderá ser ajustado no appsettings no futuro.
    public TimeSpan SessionDuration { get; set; } = TimeSpan.FromHours(8);
}
