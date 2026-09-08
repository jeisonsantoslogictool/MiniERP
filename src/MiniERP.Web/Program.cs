using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using MiniERP.Infrastructure;
using MiniERP.Infrastructure.Persistence;
using MiniERP.Web.Components;
using MiniERP.Web.Components.Account;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Probar el sistema exige a veces dos instancias abiertas a la vez, una por usuario. El
// navegador NO separa las cookies por puerto: localhost:5099 y localhost:5100 comparten el
// mismo tarro, asi que ambas guardaban su sesion con el mismo nombre y la segunda desalojaba
// a la primera; no habia manera de tener al administrador y al cajero abiertos al tiempo.
// Metiendo el puerto en el nombre, cada instancia guarda la suya. Solo en desarrollo: en
// produccion hay una sola instancia y cambiar el nombre cerraria las sesiones vivas.
if (builder.Environment.IsDevelopment())
{
    var direccion = (builder.Configuration["urls"] ?? builder.Configuration["ASPNETCORE_URLS"])
        ?.Split(';', StringSplitOptions.RemoveEmptyEntries)
        .FirstOrDefault();

    var puerto = Uri.TryCreate(direccion, UriKind.Absolute, out var uri) ? uri.Port : 0;

    if (puerto > 0)
    {
        builder.Services.Configure<CookieAuthenticationOptions>(
            IdentityConstants.ApplicationScheme,
            opciones => opciones.Cookie.Name = $".AspNetCore.Identity.Application.{puerto}");

        // El antiforgery corre la misma suerte: con el mismo nombre en el mismo host, el
        // token de una instancia llega a la otra y el formulario de acceso se cae al enviar.
        builder.Services.AddAntiforgery(
            opciones => opciones.Cookie.Name = $".AspNetCore.Antiforgery.{puerto}");
    }
}

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();

var app = builder.Build();

// Crea la base si no existe, aplica lo pendiente y siembra lo indispensable.
// Arrancar la aplicación es el único paso de despliegue.
await app.InicializarBaseDatosAsync();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

app.Run();
