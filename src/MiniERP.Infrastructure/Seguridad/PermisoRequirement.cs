using Microsoft.AspNetCore.Authorization;

namespace MiniERP.Infrastructure.Seguridad;

/// <summary>
/// Exige que el usuario tenga concedido un permiso concreto del catalogo
/// (<see cref="MiniERP.Domain.Core.Permisos"/>).
/// </summary>
public sealed class PermisoRequirement(string permiso) : IAuthorizationRequirement
{
    /// <summary>El permiso exigido, p. ej. <c>ventas.facturar</c>.</summary>
    public string Permiso { get; } = permiso;
}
