using Microsoft.AspNetCore.Authorization;
using MiniERP.Domain.Core;

namespace MiniERP.Infrastructure.Seguridad;

/// <summary>
/// Resuelve un <see cref="PermisoRequirement"/>: concede el acceso si el usuario es
/// <see cref="Roles.Administrador"/> (acceso total por regla, no por claims) o si tiene el
/// claim del permiso exigido. En cualquier otro caso no llama a <c>Succeed</c> y la politica
/// falla.
/// </summary>
public sealed class PermisoAuthorizationHandler : AuthorizationHandler<PermisoRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermisoRequirement requirement)
    {
        if (context.User.IsInRole(Roles.Administrador)
            || context.User.HasClaim(Permisos.ClaimType, requirement.Permiso))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
