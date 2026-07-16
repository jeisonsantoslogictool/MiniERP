namespace MiniERP.Domain.Compras;

/// <summary>
/// Punto del ciclo de vida en que esta una compra.
/// </summary>
public enum EstadoCompra
{
    /// <summary>
    /// Orden de compra: se pidio pero la mercancia no ha llegado. Es el unico estado
    /// en que el documento se puede modificar.
    /// </summary>
    Borrador = 1,

    /// <summary>
    /// La mercancia entro. El documento queda congelado y el inventario ya se movio.
    /// </summary>
    Recibida = 2,

    /// <summary>
    /// Se anulo antes de recibirla. Una compra ya recibida no se anula: se devuelve,
    /// porque el inventario ya se movio y borrar el documento dejaria el saldo sin origen.
    /// </summary>
    Anulada = 3
}
