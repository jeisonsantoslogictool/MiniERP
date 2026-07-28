using MiniERP.Domain.Core;

namespace MiniERP.Application.Seguridad.Dtos;

/// <summary>Fila del listado de usuarios.</summary>
public record UsuarioListaDto(
    string Id,
    string Nombre,
    string Email,
    string Rol,
    bool Activo);

/// <summary>Datos que captura el formulario de alta y edicion de usuario.</summary>
public class UsuarioFormDto
{
    /// <summary>Nulo o vacio = usuario nuevo.</summary>
    public string? Id { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    /// <summary>Rol que sirve de plantilla de permisos al crear el usuario.</summary>
    public string Rol { get; set; } = Roles.Cajero;

    /// <summary>Clave inicial. Solo se usa al crear; en la edicion se ignora.</summary>
    public string? Clave { get; set; }

    public bool Activo { get; set; } = true;

    public bool EsNuevo => string.IsNullOrEmpty(Id);
}

/// <summary>Estado de los permisos de un usuario, para dibujar y editar la matriz.</summary>
public class PermisosUsuarioDto
{
    public string Id { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Rol { get; set; } = string.Empty;

    /// <summary>Si es administrador tiene todo por regla, y la matriz queda informativa.</summary>
    public bool EsAdministrador { get; set; }

    /// <summary>Permisos concedidos hoy (claves del catalogo).</summary>
    public HashSet<string> Permisos { get; set; } = new(StringComparer.Ordinal);
}
