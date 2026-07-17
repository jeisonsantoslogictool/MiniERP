namespace MiniERP.Domain.Compras;

/// <summary>
/// Punto del ciclo de vida en que esta una devolucion a proveedor.
/// </summary>
public enum EstadoDevolucion
{
    /// <summary>Se esta capturando. Unico estado editable; aun no movio inventario.</summary>
    Borrador = 1,

    /// <summary>La mercancia salio. El documento queda congelado y el inventario ya bajo.</summary>
    Confirmada = 2
}
