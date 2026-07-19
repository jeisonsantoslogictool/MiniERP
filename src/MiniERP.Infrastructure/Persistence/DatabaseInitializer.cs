using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Core;
using MiniERP.Domain.Finanzas;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Ventas;
using MiniERP.Infrastructure.Settings;

namespace MiniERP.Infrastructure.Persistence;

/// <summary>
/// Deja la base de datos lista para operar al arrancar el sistema: la crea si no existe,
/// aplica las migraciones pendientes y siembra lo indispensable para poder trabajar.
/// </summary>
/// <remarks>
/// Todo el proceso es idempotente: correrlo mil veces produce el mismo resultado que
/// correrlo una. Eso permite que arrancar la aplicacion sea el unico paso de despliegue,
/// tanto en la maquina de un desarrollador como en el comercio piloto.
///
/// Migrar automaticamente al arrancar es adecuado aqui porque hay una sola instancia
/// atendiendo un solo local. Con varias instancias simultaneas, dos podrian intentar
/// migrar a la vez y habria que mover esto a un paso de despliegue aparte.
/// </remarks>
public class DatabaseInitializer(
    MiniErpDbContext contexto,
    UserManager<ApplicationUser> usuarios,
    RoleManager<IdentityRole> roles,
    IOptions<SeedSettings> opciones,
    ILogger<DatabaseInitializer> log)
{
    private readonly SeedSettings _config = opciones.Value;

    public async Task InicializarAsync(CancellationToken ct = default)
    {
        await AplicarMigracionesAsync(ct);
        await SembrarRolesAsync();
        await SembrarAdministradorAsync();
        await SembrarCategoriasAsync(ct);
        await SembrarCategoriasEgresoAsync(ct);
        await SembrarSecuenciasNcfAsync(ct);
    }

    /// <summary>
    /// Siembra rangos de comprobantes para que el sistema pueda facturar el primer dia.
    /// </summary>
    /// <remarks>
    /// Son rangos de arranque, NO autorizaciones de la DGII. El comercio debe reemplazar
    /// estos rangos por los que la DGII le autorice antes de facturar de verdad; emitir
    /// con un rango inventado es una infraccion. Se siembran para que el sistema sea
    /// usable de inmediato en desarrollo y en las pruebas del piloto.
    /// </remarks>
    private async Task SembrarSecuenciasNcfAsync(CancellationToken ct)
    {
        if (await contexto.SecuenciasNcf.AnyAsync(ct))
            return;

        var vence = DateTime.UtcNow.Date.AddYears(1);

        (TipoComprobante Tipo, string Prefijo)[] rangos =
        [
            (TipoComprobante.Consumo, "B02"),
            (TipoComprobante.CreditoFiscal, "B01"),
            (TipoComprobante.RegimenEspecial, "B14"),
            (TipoComprobante.Gubernamental, "B15")
        ];

        contexto.SecuenciasNcf.AddRange(rangos.Select(r => new SecuenciaNcf
        {
            TipoComprobante = r.Tipo,
            Prefijo = r.Prefijo,
            Desde = 1,
            Hasta = 5000,
            Actual = 0,
            FechaVencimiento = vence,
            Activa = true,
            CreadoPor = "sistema"
        }));

        await contexto.SaveChangesAsync(ct);

        log.LogWarning(
            "Se sembraron {Cantidad} secuencias de NCF de arranque (B01, B02, B14, B15), " +
            "validas hasta {Vence:dd/MM/yyyy}. NO son rangos autorizados por la DGII: " +
            "reemplazalos por los tuyos antes de facturar de verdad.",
            rangos.Length, vence);
    }

    /// <summary>
    /// Crea la base si no existe y aplica lo que falte. Si ya esta al dia, no hace nada.
    /// </summary>
    private async Task AplicarMigracionesAsync(CancellationToken ct)
    {
        try
        {
            var pendientes = (await contexto.Database.GetPendingMigrationsAsync(ct)).ToList();

            if (pendientes.Count == 0)
            {
                log.LogInformation("Base de datos al dia. No hay migraciones pendientes.");
                return;
            }

            log.LogInformation(
                "Aplicando {Cantidad} migracion(es) pendiente(s): {Migraciones}",
                pendientes.Count, string.Join(", ", pendientes));

            await contexto.Database.MigrateAsync(ct);

            log.LogInformation("Base de datos actualizada.");
        }
        catch (Exception ex)
        {
            log.LogError(ex,
                "No se pudo preparar la base de datos. Verifica que SQL Server este " +
                "accesible y que la cadena de conexion sea correcta.");
            throw;
        }
    }

    private async Task SembrarRolesAsync()
    {
        foreach (var rol in Roles.Todos)
        {
            if (await roles.RoleExistsAsync(rol))
                continue;

            var resultado = await roles.CreateAsync(new IdentityRole(rol));

            if (!resultado.Succeeded)
            {
                var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
                log.LogError("No se pudo crear el rol {Rol}: {Errores}", rol, errores);
                throw new InvalidOperationException($"No se pudo crear el rol '{rol}': {errores}");
            }

            log.LogInformation("Rol creado: {Rol}", rol);
        }
    }

    /// <summary>
    /// Crea el primer administrador. Sin el, nadie podria entrar a un sistema recien
    /// instalado, porque el registro publico no otorga rol de administrador.
    /// </summary>
    private async Task SembrarAdministradorAsync()
    {
        if (await usuarios.FindByEmailAsync(_config.AdminEmail) is not null)
            return;

        var (clave, fueGenerada) = ResolverClaveAdministrador();

        var admin = new ApplicationUser
        {
            UserName = _config.AdminEmail,
            Email = _config.AdminEmail,
            EmailConfirmed = true
        };

        var resultado = await usuarios.CreateAsync(admin, clave);

        if (!resultado.Succeeded)
        {
            var errores = string.Join("; ", resultado.Errors.Select(e => e.Description));
            log.LogError("No se pudo crear el administrador inicial: {Errores}", errores);
            throw new InvalidOperationException($"No se pudo crear el administrador: {errores}");
        }

        await usuarios.AddToRoleAsync(admin, Roles.Administrador);

        if (fueGenerada)
        {
            // Unica vez que esta clave es visible. Va como advertencia para que no se
            // pierda entre los mensajes informativos del arranque.
            log.LogWarning(
                "\n" +
                "===============================================================\n" +
                " ADMINISTRADOR INICIAL CREADO\n" +
                "   Usuario:    {Email}\n" +
                "   Contrasena: {Clave}\n" +
                "\n" +
                " Esta contrasena se genero al azar y no se vuelve a mostrar.\n" +
                " Guardala y cambiala al entrar por primera vez.\n" +
                "===============================================================",
                _config.AdminEmail, clave);
        }
        else
        {
            log.LogInformation(
                "Administrador inicial creado con la contrasena configurada: {Email}",
                _config.AdminEmail);
        }
    }

    private (string Clave, bool FueGenerada) ResolverClaveAdministrador() =>
        string.IsNullOrWhiteSpace(_config.AdminPassword)
            ? (GenerarClaveSegura(), true)
            : (_config.AdminPassword, false);

    /// <summary>
    /// Genera una clave que cumple las reglas de Identity: mayuscula, minuscula,
    /// digito y simbolo. Los grupos se toman por separado para garantizar que
    /// aparezca al menos uno de cada tipo, no para dejarlo al azar.
    /// </summary>
    private static string GenerarClaveSegura()
    {
        const string mayusculas = "ABCDEFGHJKLMNPQRSTUVWXYZ";
        const string minusculas = "abcdefghijkmnpqrstuvwxyz";
        const string digitos = "23456789";
        const string simbolos = "!@#$%&*?";
        const string todos = mayusculas + minusculas + digitos + simbolos;

        var clave = new List<char>
        {
            Tomar(mayusculas),
            Tomar(minusculas),
            Tomar(digitos),
            Tomar(simbolos)
        };

        for (var i = clave.Count; i < 16; i++)
            clave.Add(Tomar(todos));

        // Sin esto, los cuatro primeros caracteres serian siempre del mismo tipo.
        return new string([.. clave.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue))]);

        static char Tomar(string alfabeto) =>
            alfabeto[RandomNumberGenerator.GetInt32(alfabeto.Length)];
    }

    /// <summary>
    /// Siembra las categorias tipicas de un minimarket. Un producto exige categoria,
    /// asi que sin al menos una el sistema no se puede usar recien instalado.
    /// </summary>
    private async Task SembrarCategoriasAsync(CancellationToken ct)
    {
        if (!_config.SembrarCategorias)
            return;

        if (await contexto.Categorias.AnyAsync(ct))
            return;

        string[] nombres =
        [
            "Viveres",
            "Bebidas",
            "Lacteos",
            "Carnes y Embutidos",
            "Panaderia",
            "Enlatados",
            "Limpieza",
            "Higiene Personal",
            "Snacks y Golosinas",
            "Congelados"
        ];

        contexto.Categorias.AddRange(nombres.Select(n => new Categoria
        {
            Nombre = n,
            Activo = true,
            CreadoPor = "sistema"
        }));

        await contexto.SaveChangesAsync(ct);

        log.LogInformation("Categorias iniciales sembradas: {Cantidad}", nombres.Length);
    }

    /// <summary>
    /// Siembra las categorias tipicas de gasto de un minimarket. El comerciante puede
    /// agregar las suyas; estas son solo el punto de partida para no empezar en blanco.
    /// </summary>
    private async Task SembrarCategoriasEgresoAsync(CancellationToken ct)
    {
        if (!_config.SembrarCategorias)
            return;

        if (await contexto.CategoriasEgreso.AnyAsync(ct))
            return;

        string[] nombres = ["Alquiler", "Luz", "Agua", "Sueldos", "Transporte"];

        contexto.CategoriasEgreso.AddRange(nombres.Select(n => new CategoriaEgreso
        {
            Nombre = n,
            Activo = true,
            CreadoPor = "sistema"
        }));

        await contexto.SaveChangesAsync(ct);

        log.LogInformation("Categorias de egreso iniciales sembradas: {Cantidad}", nombres.Length);
    }
}
