namespace MiniERP.Domain.Ventas;

/// <summary>
/// Punto del ciclo de vida en que esta una factura.
/// </summary>
/// <remarks>
/// No hay borrador. Una factura nace emitida: en un mostrador no se guardan ventas a
/// medias, y el NCF se consume al emitir. Lo que esta a medias es el carrito, que vive
/// en la pantalla y no en la base.
/// </remarks>
public enum EstadoFactura
{
    /// <summary>Emitida: consumio su NCF y movio el inventario.</summary>
    Emitida = 1,

    /// <summary>
    /// Anulada. El NCF NO se reutiliza: ante la DGII queda como comprobante anulado,
    /// y el rango sigue avanzando.
    /// </summary>
    Anulada = 2
}
