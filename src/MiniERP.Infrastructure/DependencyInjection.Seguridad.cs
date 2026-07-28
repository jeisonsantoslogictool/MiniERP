using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using MiniERP.Application.Seguridad.Contracts;
using MiniERP.Infrastructure.Seguridad;

namespace MiniERP.Infrastructure;

public static partial class DependencyInjection
{
    /// <summary>
    /// Nucleo de seguridad: autorizacion por permiso y administracion de usuarios. Registra
    /// el policy provider que crea una politica por cada permiso del catalogo, el handler que
    /// la resuelve, y el servicio de gestion de usuarios. Dueno: Jeison (plataforma). Los
    /// demas modulos solo escriben <c>[Authorize(Policy = Permisos.X)]</c> en sus pantallas.
    /// </summary>
    public static IServiceCollection AddSeguridad(this IServiceCollection services)
    {
        // Asegura los servicios base de autorizacion (proveedor por defecto, IAuthorizationService).
        services.AddAuthorization();

        // El provider a la medida debe ser singleton (lo exige el framework) y sustituye al
        // por defecto, delegando en el para todo lo que no sea un permiso del catalogo.
        services.AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>();
        services.AddSingleton<IAuthorizationHandler, PermisoAuthorizationHandler>();

        // Administracion de usuarios (envuelve UserManager, que es scoped).
        services.AddScoped<IGestionUsuariosService, GestionUsuariosService>();

        return services;
    }
}
