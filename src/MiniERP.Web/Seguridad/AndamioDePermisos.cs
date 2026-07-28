using Microsoft.AspNetCore.Authorization;

namespace MiniERP.Web.Seguridad;

/// <summary>
/// ANDAMIO PROVISIONAL — se borra cuando entre jeison/seguridad a main.
/// Ver Docs/Andamio-Permisos.md.
///
/// Registra el minimo indispensable para que [Authorize(Policy = ...)] funcione, de modo
/// que el gateo de Finanzas (rama samuel/permisos) se pueda compilar y probar antes de que
/// exista el nucleo real. NO incluye las pantallas de usuarios ni la siembra de plantillas
/// de rol: eso es de Jeison y no se toca desde aqui.
/// </summary>
public static class AndamioDePermisos
{
    public static IServiceCollection AddAndamioDePermisos(this IServiceCollection services)
    {
        services.AddAuthorizationCore();

        // Va despues de AddAuthorizationCore a proposito: el ultimo registro gana, y asi
        // este proveedor reemplaza al de por defecto.
        services.AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermisoAuthorizationHandler>();

        return services;
    }
}
