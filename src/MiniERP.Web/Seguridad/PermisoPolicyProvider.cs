using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using MiniERP.Domain.Core;

namespace MiniERP.Web.Seguridad;

/// <summary>
/// ANDAMIO PROVISIONAL — parte del nucleo de permisos, propiedad de Jeison.
/// Se borra cuando entre jeison/seguridad a main. Ver Docs/Andamio-Permisos.md.
///
/// Convierte cada permiso del catalogo en una politica de autorizacion, al vuelo.
/// </summary>
/// <remarks>
/// Sin esto habria que registrar a mano una politica por permiso en el arranque —17 hoy, y
/// una linea mas cada vez que alguien agregue una accion—. Asi basta con agregar la
/// constante en <see cref="Permisos"/> y ya se puede escribir [Authorize(Policy = ...)].
/// Los nombres que no estan en el catalogo se delegan al proveedor por defecto, para no
/// romper las politicas propias de Identity.
/// </remarks>
public sealed class PermisoPolicyProvider : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _porDefecto;
    private readonly ConcurrentDictionary<string, AuthorizationPolicy> _cache = new();

    public PermisoPolicyProvider(IOptions<AuthorizationOptions> opciones) =>
        _porDefecto = new DefaultAuthorizationPolicyProvider(opciones);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() =>
        _porDefecto.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() =>
        _porDefecto.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (!Permisos.Existe(policyName))
            return _porDefecto.GetPolicyAsync(policyName);

        var politica = _cache.GetOrAdd(policyName, nombre =>
            new AuthorizationPolicyBuilder()
                .AddRequirements(new PermisoRequirement(nombre))
                .Build());

        return Task.FromResult<AuthorizationPolicy?>(politica);
    }
}
