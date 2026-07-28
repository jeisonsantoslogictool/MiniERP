using Microsoft.AspNetCore.Authorization;

namespace MiniERP.Web.Seguridad;

/// <summary>
/// ANDAMIO PROVISIONAL — parte del nucleo de permisos, propiedad de Jeison.
/// Se borra cuando entre jeison/seguridad a main. Ver Docs/Andamio-Permisos.md.
///
/// La exigencia que se evalua cuando una pantalla pide un permiso: "el usuario tiene
/// que traer este permiso encima".
/// </summary>
public sealed class PermisoRequirement(string permiso) : IAuthorizationRequirement
{
    /// <summary>El permiso exigido, tal como esta en <see cref="MiniERP.Domain.Core.Permisos"/>.</summary>
    public string Permiso { get; } = permiso;
}
