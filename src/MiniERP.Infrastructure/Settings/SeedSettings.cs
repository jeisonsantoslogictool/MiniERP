namespace MiniERP.Infrastructure.Settings;

/// <summary>
/// Datos del administrador inicial que se crea la primera vez que arranca el sistema.
/// </summary>
/// <remarks>
/// La contrasena no tiene valor por defecto a proposito: una contrasena escrita en el
/// repositorio es la misma en la maquina de cada quien y en el comercio piloto. Si no
/// se configura, el sistema genera una aleatoria y la escribe una sola vez en la consola.
///
/// Para fijar una propia, sin que llegue al repositorio:
///   dotnet user-secrets set "Seed:AdminPassword" "TuClave" --project src/MiniERP.Web
/// </remarks>
public class SeedSettings
{
    public const string SectionName = "Seed";

    public string AdminEmail { get; set; } = "admin@minierp.local";

    public string? AdminPassword { get; set; }

    public string AdminNombre { get; set; } = "Administrador";

    /// <summary>
    /// Siembra el catalogo de categorias tipico de un minimarket. Desactivalo si el
    /// comercio prefiere partir de cero.
    /// </summary>
    public bool SembrarCategorias { get; set; } = true;
}
