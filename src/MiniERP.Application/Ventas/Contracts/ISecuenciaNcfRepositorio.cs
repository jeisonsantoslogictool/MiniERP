using MiniERP.Domain.Clientes;
using MiniERP.Domain.Ventas;

namespace MiniERP.Application.Ventas.Contracts;

public interface ISecuenciaNcfRepositorio
{
    Task<IReadOnlyList<SecuenciaNcf>> ObtenerTodasAsync(CancellationToken ct = default);

    Task<SecuenciaNcf?> ObtenerPorTipoAsync(TipoComprobante tipo, CancellationToken ct = default);

    /// <summary>
    /// Reserva el proximo comprobante del tipo indicado y lo devuelve ya formateado.
    /// Devuelve null si no hay rango disponible: agotado, vencido o inexistente.
    /// </summary>
    /// <remarks>
    /// La implementacion debe ser atomica. Dos cajas facturando a la vez no pueden
    /// recibir el mismo numero: emitir dos comprobantes con el mismo NCF es una
    /// infraccion ante la DGII, no un detalle de implementacion.
    /// </remarks>
    Task<string?> AsignarSiguienteAsync(TipoComprobante tipo, CancellationToken ct = default);

    void Agregar(SecuenciaNcf secuencia);

    Task<int> GuardarAsync(CancellationToken ct = default);
}
