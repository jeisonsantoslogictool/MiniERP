using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace MiniERP.Infrastructure.Persistence;

/// <summary>
/// Usuario de la aplicacion. Extiende <see cref="IdentityUser"/> con el nombre para mostrar
/// que administra el dueno desde la pantalla de usuarios. Los permisos no viven aqui: son
/// claims del usuario (ver <see cref="MiniERP.Domain.Core.Permisos"/>).
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>Nombre para mostrar del empleado. Lo asigna y edita el administrador.</summary>
    [MaxLength(120)]
    public string Nombre { get; set; } = string.Empty;
}
