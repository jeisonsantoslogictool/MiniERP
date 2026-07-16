using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace MiniERP.Infrastructure.Persistence;

public static class DatabaseInitializerExtensions
{
    /// <summary>
    /// Prepara la base de datos antes de atender la primera peticion: la crea si no
    /// existe, aplica las migraciones pendientes y siembra los datos indispensables.
    /// </summary>
    /// <remarks>
    /// Se ejecuta en su propio ambito porque el inicializador depende de servicios
    /// scoped (el contexto y los administradores de Identity), que no se pueden
    /// resolver desde el contenedor raiz.
    /// </remarks>
    public static async Task InicializarBaseDatosAsync(this IHost host, CancellationToken ct = default)
    {
        using var ambito = host.Services.CreateScope();

        var inicializador = ambito.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        await inicializador.InicializarAsync(ct);
    }
}
