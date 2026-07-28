using Microsoft.AspNetCore.Authorization;
using MiniERP.Domain.Core;

namespace MiniERP.Web.Seguridad;

/// <summary>
/// ANDAMIO PROVISIONAL — parte del nucleo de permisos, propiedad de Jeison.
/// Se borra cuando entre jeison/seguridad a main. Ver Docs/Andamio-Permisos.md.
///
/// Decide si un usuario cumple con el permiso que exige la pantalla.
/// </summary>
/// <remarks>
/// Dos formas de pasar, y el orden importa:
/// 1. Ser Administrador. Es un atajo deliberado: el dueno del negocio no puede quedarse
///    fuera de su propio sistema por haber olvidado marcar una casilla.
/// 2. Traer el permiso como claim. Es el camino normal de un empleado.
/// Si no se cumple ninguna, no se llama a Fail(): basta con no conceder. Llamar a Fail()
/// bloquearia la politica aunque otro manejador la concediera, y eso cierra la puerta a
/// combinar reglas mas adelante.
/// </remarks>
public sealed class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermisoRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // El Administrador pasa siempre: es el dueno, no se le puede dejar afuera.
        if (context.User.IsInRole(Roles.Administrador))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.HasClaim(Permisos.TipoDeClaim, requirement.Permiso))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
