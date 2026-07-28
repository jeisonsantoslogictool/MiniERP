using MiniERP.Application.Common;
using MiniERP.Application.Seguridad.Dtos;

namespace MiniERP.Application.Seguridad.Contracts;

/// <summary>
/// Administracion de usuarios y de sus permisos. La implementacion vive en Infrastructure
/// porque envuelve el <c>UserManager</c> de Identity; la interfaz vive aqui para que la capa
/// web dependa de la abstraccion y no de Identity.
/// </summary>
public interface IGestionUsuariosService
{
    Task<IReadOnlyList<UsuarioListaDto>> ListarAsync(CancellationToken ct = default);

    Task<UsuarioFormDto?> ObtenerParaEditarAsync(string id, CancellationToken ct = default);

    Task<Resultado> CrearAsync(UsuarioFormDto form, CancellationToken ct = default);

    Task<Resultado> ActualizarAsync(UsuarioFormDto form, CancellationToken ct = default);

    /// <summary>Activa o desactiva el acceso (bloqueo de Identity). No borra al usuario.</summary>
    Task<Resultado> CambiarEstadoAsync(string id, bool activar, CancellationToken ct = default);

    /// <summary>Genera una clave nueva y la devuelve una sola vez, para entregarla al usuario.</summary>
    Task<Resultado<string>> ResetearClaveAsync(string id, CancellationToken ct = default);

    Task<PermisosUsuarioDto?> ObtenerPermisosAsync(string id, CancellationToken ct = default);

    Task<Resultado> GuardarPermisosAsync(
        string id, IReadOnlyCollection<string> permisos, CancellationToken ct = default);

    /// <summary>Los roles que sirven de plantilla al crear un usuario.</summary>
    IReadOnlyList<string> RolesDisponibles();
}
