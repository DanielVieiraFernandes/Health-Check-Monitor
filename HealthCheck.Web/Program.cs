using HealthCheck.Framework.Repositories;
using HealthCheck.Framework.Services;
using HealthCheck.Web.Components;
using HealthCheck.Web.Components.Shared.Feedback;
using HealthCheck.Web.Services.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using MudBlazor.Services;
using Syncfusion.Blazor;
using System.Globalization;

namespace HealthCheck.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

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

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        // Rate Limiter — proteção contra abuso e brute-force
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Política global: 30 req/min por IP
            options.AddFixedWindowLimiter("fixed", config =>
            {
                config.PermitLimit = 30;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 5;
            });

            // Política para login: 5 req/min por IP (anti brute-force)
            options.AddFixedWindowLimiter("auth", config =>
            {
                config.PermitLimit = 5;
                config.Window = TimeSpan.FromMinutes(1);
                config.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
                config.QueueLimit = 2;
            });
        });

        //OBS: Por enquanto, irei manter a cultura fixa para pt-BR

        //==============================================================================================
        //Define a cultura padrão da aplicação para pt-BR (Brasil).
        //==============================================================================================

        var cultureInfo = new CultureInfo("pt-BR");

        CultureInfo.CurrentCulture = cultureInfo;
        CultureInfo.CurrentUICulture = cultureInfo;

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        // Kestrel limits — proteção contra ataques de recurso
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        builder.WebHost.ConfigureKestrel(options =>
        {
            options.Limits.MaxConcurrentConnections = 100;
            options.Limits.MaxConcurrentUpgradedConnections = 100;
            options.Limits.MaxRequestBodySize = 1_048_576; // 1MB
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(30);
            options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(60);
        });

        var app = builder.Build();

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

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        // Rate limiter middleware (precisa vir depois de UseAntiforgery mas antes das rotas)
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        app.UseRateLimiter();

        app.UseAntiforgery();

        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        // Security headers — proteção contra ataques XSS, clickjacking, MIME sniffing
        //-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
        app.Use(async (context, next) =>
        {
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            context.Response.Headers["X-Frame-Options"] = "DENY";
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; script-src 'self' https://cdn.syncfusion.com 'unsafe-eval';";
            await next();
        });

        app.MapStaticAssets();
        //==============================================================================================
        // Endpoints HTTP usados para login/logout (cookies precisam ser enviados em resposta HTTP).
        //==============================================================================================
        app.MapPost("/auth/login", async (LoginRequest request, AuthenticationService authService) =>
            await authService.SignInHttpAsync(request)).RequireRateLimiting("auth");
        app.MapPost("/auth/logout", async (AuthenticationService authService) =>
            await authService.SignOutHttpAsync());

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
