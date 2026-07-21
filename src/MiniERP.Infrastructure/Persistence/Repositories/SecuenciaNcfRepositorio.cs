using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Ventas.Contracts;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Ventas;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class SecuenciaNcfRepositorio(MiniErpDbContext contexto) : ISecuenciaNcfRepositorio
{
    public async Task<IReadOnlyList<SecuenciaNcf>> ObtenerTodasAsync(CancellationToken ct = default) =>
        await contexto.SecuenciasNcf
            .AsNoTracking()
            .OrderBy(s => s.Prefijo)
            .ToListAsync(ct);

    public Task<SecuenciaNcf?> ObtenerPorTipoAsync(TipoComprobante tipo, CancellationToken ct = default) =>
        contexto.SecuenciasNcf
            .AsNoTracking()
            .Where(s => s.TipoComprobante == tipo && s.Activa)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct);

    /// <summary>
    /// Reserva el proximo comprobante del tipo indicado y devuelve el numero ya formateado.
    /// </summary>
    /// <remarks>
    /// El incremento y la lectura ocurren en una sola sentencia UPDATE ... OUTPUT, que
    /// SQL Server ejecuta de forma atomica. Leer Actual, sumarle uno y guardar desde C#
    /// abriria una ventana entre la lectura y la escritura: dos cajeros facturando al
    /// mismo tiempo obtendrian el mismo numero y emitirian dos comprobantes con el mismo
    /// NCF. Eso no es un bug cosmetico, es una infraccion fiscal ante la DGII.
    ///
    /// El WHERE tambien filtra por rango disponible y vigencia, de modo que la propia
    /// sentencia rechaza la secuencia agotada o vencida sin condiciones de carrera:
    /// si no afecta ninguna fila, no hay numero que entregar.
    /// </remarks>
    public async Task<string?> AsignarSiguienteAsync(TipoComprobante tipo, CancellationToken ct = default)
    {
        var hoy = DateTime.UtcNow.Date;

        // Se elige la secuencia primero para conocer su prefijo, y el UPDATE se hace
        // contra ese Id concreto. El prefijo no cambia, asi que leerlo antes es seguro;
        // lo que no puede leerse antes es el numero, y por eso el incremento va dentro
        // de la sentencia atomica.
        var secuencia = await contexto.SecuenciasNcf
            .AsNoTracking()
            .Where(s => s.TipoComprobante == tipo
                     && s.Activa
                     && s.Actual < s.Hasta
                     && s.FechaVencimiento >= hoy)
            .OrderBy(s => s.Id)
            .FirstOrDefaultAsync(ct);

        if (secuencia is null)
            return null;

        var asignados = await contexto.Database
            .SqlQuery<long>($@"
                UPDATE SecuenciasNcf
                SET Actual = Actual + 1
                OUTPUT INSERTED.Actual AS Value
                WHERE Id = {secuencia.Id}
                  AND Activa = 1
                  AND Actual < Hasta
                  AND FechaVencimiento >= {hoy}")
            .ToListAsync(ct);

        // Cero filas afectadas: entre la lectura y el UPDATE, otra caja consumio el
        // ultimo numero del rango. No hay comprobante que entregar.
        return asignados.Count == 0
            ? null
            : secuencia.Formatear(asignados[0]);
    }

    public Task<SecuenciaNcf?> ObtenerAsync(int id, CancellationToken ct = default) =>
        contexto.SecuenciasNcf.FirstOrDefaultAsync(s => s.Id == id, ct);

    public void Agregar(SecuenciaNcf secuencia) => contexto.SecuenciasNcf.Add(secuencia);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);
}
