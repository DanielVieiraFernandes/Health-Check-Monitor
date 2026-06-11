using HealthCheck.Framework.Repositories;
using HealthCheck.Framework.Services;
using HealthCheck.Framework.Utils;
using HealthCheck.Web.Components;
using HealthCheck.Web.Components.Shared.Feedback;
using HealthCheck.Web.Services.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using MudBlazor.Services;
using Serilog;
using Syncfusion.Blazor;
using System.Globalization;

namespace HealthCheck.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Host.UseSerilog();

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("App", "Web")
    .CreateLogger();

        // Add services to the container.
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        // Registra a chave de licença do Syncfusion 
        Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense(builder.Configuration["Syncfusion:LicenseKey"]);

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        // Injeção de dependência para os componentes da aplicação
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

        builder.Services.AddSyncfusionBlazor();
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.VisibleStateDuration = 1000;
        }); ;

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        // Injeção de dependência para os serviços e repositórios da aplicação
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        builder.Services.AddScoped<LoadingState>();
        builder.Services.AddHttpContextAccessor();
        builder.Services.Configure<UserSessionSettings>(builder.Configuration.GetSection("UserSession"));

        builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/";
                options.SlidingExpiration = true;
                //options.Cookie = new() { SameSite = SameSiteMode.Strict, HttpOnly = true, };
                options.ExpireTimeSpan = builder.Configuration
                    .GetSection("UserSession")
                    .Get<UserSessionSettings>()!.SessionDuration;
            });

        builder.Services.AddAuthorization();
        builder.Services.AddScoped<AuthenticationService>();
        builder.Services.AddScoped<AuthenticationStateProvider, CookieAuthenticationStateProvider>();

        builder.Services.AddFrameworkServices(builder.Configuration);
        builder.Services.AddFrameworkRepositories();

        //OBS: Por enquanto, irei manter a cultura fixa para pt-BR

        //==============================================================================================
        //Define a cultura padrão da aplicação para pt-BR (Brasil).
        //==============================================================================================

        var cultureInfo = new CultureInfo("pt-BR");

        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        var app = builder.Build();

        RecordLog.Initialize(app.Services.GetRequiredService<ILoggerFactory>());

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseHttpsRedirection();

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseAntiforgery();

        app.MapStaticAssets();
        //==============================================================================================
        // Endpoints HTTP usados para login/logout (cookies precisam ser enviados em resposta HTTP).
        //==============================================================================================
        app.MapPost("/auth/login", async (LoginRequest request, AuthenticationService authService) =>
            await authService.SignInHttpAsync(request));
        app.MapPost("/auth/logout", async (AuthenticationService authService) =>
            await authService.SignOutHttpAsync());

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        try
        {
            app.Run();
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
