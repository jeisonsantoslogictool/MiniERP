using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Common;
using MiniERP.Application.Seguridad.Contracts;
using MiniERP.Application.Seguridad.Dtos;
using MiniERP.Domain.Core;
using MiniERP.Infrastructure.Persistence;

namespace MiniERP.Infrastructure.Seguridad;

/// <summary>
/// Administracion de usuarios y permisos sobre Identity. Envuelve el <c>UserManager</c> y
/// traduce entre las entidades de Identity y los DTOs de la capa de aplicacion. Los permisos
/// se guardan como claims del tipo <see cref="Permisos.ClaimType"/>; desactivar es un bloqueo
/// de Identity, no un borrado, para no perder el rastro de quien hizo cada operacion.
/// </summary>
public class GestionUsuariosService(UserManager<ApplicationUser> usuarios) : IGestionUsuariosService
{
    public async Task<IReadOnlyList<UsuarioListaDto>> ListarAsync(CancellationToken ct = default)
    {
        var todos = await usuarios.Users.OrderBy(u => u.Nombre).ToListAsync(ct);

        var filas = new List<UsuarioListaDto>(todos.Count);
        foreach (var u in todos)
        {
            var rol = (await usuarios.GetRolesAsync(u)).FirstOrDefault() ?? string.Empty;
            filas.Add(new UsuarioListaDto(
                u.Id,
                NombreVisible(u),
                u.Email ?? string.Empty,
                rol,
                !await usuarios.IsLockedOutAsync(u)));
        }

        return filas;
    }

    public async Task<UsuarioFormDto?> ObtenerParaEditarAsync(string id, CancellationToken ct = default)
    {
        var u = await usuarios.FindByIdAsync(id);
        if (u is null)
            return null;

        return new UsuarioFormDto
        {
            Id = u.Id,
            Nombre = u.Nombre,
            Email = u.Email ?? string.Empty,
            Rol = (await usuarios.GetRolesAsync(u)).FirstOrDefault() ?? Roles.Cajero,
            Activo = !await usuarios.IsLockedOutAsync(u)
        };
    }

    public async Task<Resultado> CrearAsync(UsuarioFormDto form, CancellationToken ct = default)
    {
        var email = form.Email.Trim();

        if (string.IsNullOrWhiteSpace(email))
            return Resultado.Falla("El correo es obligatorio.");
        if (string.IsNullOrWhiteSpace(form.Nombre))
            return Resultado.Falla("El nombre es obligatorio.");
        if (!Roles.Todos.Contains(form.Rol))
            return Resultado.Falla("El rol seleccionado no existe.");
        if (string.IsNullOrWhiteSpace(form.Clave))
            return Resultado.Falla("La clave inicial es obligatoria.");
        if (await usuarios.FindByEmailAsync(email) is not null)
            return Resultado.Falla($"Ya existe un usuario con el correo '{email}'.");

        var usuario = new ApplicationUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            Nombre = form.Nombre.Trim()
        };

        var creado = await usuarios.CreateAsync(usuario, form.Clave);
        if (!creado.Succeeded)
            return Resultado.Falla(Errores(creado));

        await usuarios.AddToRoleAsync(usuario, form.Rol);

        // El rol es una plantilla: se siembran sus permisos por defecto como claims del
        // usuario, que luego el administrador afina en la pantalla de permisos.
        foreach (var permiso in Permisos.PorDefectoDeRol(form.Rol))
            await usuarios.AddClaimAsync(usuario, new Claim(Permisos.ClaimType, permiso));

        return Resultado.Ok();
    }

    public async Task<Resultado> ActualizarAsync(UsuarioFormDto form, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(form.Id))
            return Resultado.Falla("Usuario no identificado.");
        if (string.IsNullOrWhiteSpace(form.Nombre))
            return Resultado.Falla("El nombre es obligatorio.");
        if (!Roles.Todos.Contains(form.Rol))
            return Resultado.Falla("El rol seleccionado no existe.");

        var usuario = await usuarios.FindByIdAsync(form.Id);
        if (usuario is null)
            return Resultado.Falla("El usuario no existe.");

        var rolesActuales = await usuarios.GetRolesAsync(usuario);
        var eraAdmin = rolesActuales.Contains(Roles.Administrador);
        var seguiraAdmin = form.Rol == Roles.Administrador;

        // Guardrail: no dejar el sistema sin ningun administrador.
        if (eraAdmin && !seguiraAdmin && await EsUltimoAdminActivoAsync(usuario))
            return Resultado.Falla(
                "No puedes quitarle el rol de administrador al unico administrador activo.");

        usuario.Nombre = form.Nombre.Trim();

        if (rolesActuales.Count > 0)
            await usuarios.RemoveFromRolesAsync(usuario, rolesActuales);
        await usuarios.AddToRoleAsync(usuario, form.Rol);

        var actualizado = await usuarios.UpdateAsync(usuario);
        if (!actualizado.Succeeded)
            return Resultado.Falla(Errores(actualizado));

        return Resultado.Ok();
    }

    public async Task<Resultado> CambiarEstadoAsync(string id, bool activar, CancellationToken ct = default)
    {
        var usuario = await usuarios.FindByIdAsync(id);
        if (usuario is null)
            return Resultado.Falla("El usuario no existe.");

        if (!activar && await EsUltimoAdminActivoAsync(usuario))
            return Resultado.Falla("No puedes desactivar al unico administrador activo.");

        if (activar)
        {
            await usuarios.SetLockoutEndDateAsync(usuario, null);
        }
        else
        {
            await usuarios.SetLockoutEnabledAsync(usuario, true);
            await usuarios.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);
        }

        // Fuerza que la sesion vigente se reevalue: el revalidador rechaza la cookie vieja.
        await usuarios.UpdateSecurityStampAsync(usuario);

        return Resultado.Ok();
    }

    public async Task<Resultado<string>> ResetearClaveAsync(string id, CancellationToken ct = default)
    {
        var usuario = await usuarios.FindByIdAsync(id);
        if (usuario is null)
            return Resultado.Falla<string>("El usuario no existe.");

        var clave = GenerarClave();
        var token = await usuarios.GeneratePasswordResetTokenAsync(usuario);
        var resultado = await usuarios.ResetPasswordAsync(usuario, token, clave);

        return resultado.Succeeded
            ? Resultado.Ok(clave)
            : Resultado.Falla<string>(Errores(resultado));
    }

    public async Task<PermisosUsuarioDto?> ObtenerPermisosAsync(string id, CancellationToken ct = default)
    {
        var usuario = await usuarios.FindByIdAsync(id);
        if (usuario is null)
            return null;

        var concedidos = (await usuarios.GetClaimsAsync(usuario))
            .Where(c => c.Type == Permisos.ClaimType)
            .Select(c => c.Value);

        return new PermisosUsuarioDto
        {
            Id = usuario.Id,
            Nombre = NombreVisible(usuario),
            Email = usuario.Email ?? string.Empty,
            Rol = (await usuarios.GetRolesAsync(usuario)).FirstOrDefault() ?? string.Empty,
            EsAdministrador = await usuarios.IsInRoleAsync(usuario, Roles.Administrador),
            Permisos = new HashSet<string>(concedidos, StringComparer.Ordinal)
        };
    }

    public async Task<Resultado> GuardarPermisosAsync(
        string id, IReadOnlyCollection<string> permisos, CancellationToken ct = default)
    {
        var usuario = await usuarios.FindByIdAsync(id);
        if (usuario is null)
            return Resultado.Falla("El usuario no existe.");

        var pedidos = permisos.Where(Permisos.EsPermiso).ToHashSet(StringComparer.Ordinal);
        var actuales = (await usuarios.GetClaimsAsync(usuario))
            .Where(c => c.Type == Permisos.ClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.Ordinal);

        foreach (var permiso in pedidos.Except(actuales))
            await usuarios.AddClaimAsync(usuario, new Claim(Permisos.ClaimType, permiso));

        foreach (var permiso in actuales.Except(pedidos))
            await usuarios.RemoveClaimAsync(usuario, new Claim(Permisos.ClaimType, permiso));

        // Que el cambio surta efecto sin esperar a que el usuario vuelva a entrar.
        await usuarios.UpdateSecurityStampAsync(usuario);

        return Resultado.Ok();
    }

    public IReadOnlyList<string> RolesDisponibles() => Roles.Todos;

    /// <summary>El candidato es el unico administrador activo (nadie mas queda para entrar).</summary>
    private async Task<bool> EsUltimoAdminActivoAsync(ApplicationUser candidato)
    {
        foreach (var admin in await usuarios.GetUsersInRoleAsync(Roles.Administrador))
        {
            if (admin.Id != candidato.Id && !await usuarios.IsLockedOutAsync(admin))
                return false;
        }

        return true;
    }

    private static string NombreVisible(ApplicationUser u) =>
        string.IsNullOrWhiteSpace(u.Nombre) ? (u.Email ?? u.UserName ?? string.Empty) : u.Nombre;

    private static string Errores(IdentityResult resultado) =>
        string.Join(" ", resultado.Errors.Select(e => e.Description));

    /// <summary>Clave aleatoria que cumple las reglas de Identity (mayus, minus, digito, simbolo).</summary>
    private static string GenerarClave()
    {
        const string mayus = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minus = "abcdefghijkmnpqrstuvwxyz";
        const string digitos = "23456789";
        const string simbolos = "!@#$%&*?";
        const string todos = mayus + minus + digitos + simbolos;

        var clave = new List<char>
        {
            mayus[RandomNumberGenerator.GetInt32(mayus.Length)],
            minus[RandomNumberGenerator.GetInt32(minus.Length)],
            digitos[RandomNumberGenerator.GetInt32(digitos.Length)],
            simbolos[RandomNumberGenerator.GetInt32(simbolos.Length)]
        };

        for (var i = clave.Count; i < 14; i++)
            clave.Add(todos[RandomNumberGenerator.GetInt32(todos.Length)]);

        return new string([.. clave.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))]);
    }
}
