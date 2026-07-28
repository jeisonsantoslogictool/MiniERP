using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MiniERP.Domain.Core;

namespace MiniERP.Infrastructure.Seguridad;

/// <summary>
/// Convierte cada permiso del catalogo en una politica de autorizacion al vuelo, sin tener
/// que declararlas una por una en el arranque. Para cualquier nombre de politica que no sea
/// un permiso conocido, delega en el proveedor por defecto: asi el <c>[Authorize]</c> simple
/// (que solo exige sesion) y cualquier politica del framework siguen funcionando igual.
/// </summary>
/// <remarks>
/// Se registra como singleton (lo exige el framework de autorizacion). Ver
/// <c>DependencyInjection.Seguridad.cs</c>.
/// </remarks>
public sealed class PermisoPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _porDefecto;

    public PermisoPolicyProvider(IOptions<AuthorizationOptions> options)
        => _porDefecto = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _porDefecto.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _porDefecto.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (Permisos.EsPermiso(policyName))
        {
            var politica = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermisoRequirement(policyName))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(politica);
        }

        return _porDefecto.GetPolicyAsync(policyName);
    }
}
