using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Compras;

/// <summary>
/// Devolucion de mercancia de una compra recibida al proveedor. No anula la compra:
/// es una operacion nueva que baja la existencia y deja su propio rastro.
/// </summary>
public class DevolucionCompra : EntidadBase
{
    public string Numero { get; set; } = string.Empty;

    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>Por que se devuelve. Obligatorio: una salida discrecional exige explicacion.</summary>
    public string Motivo { get; set; } = string.Empty;

    public EstadoDevolucion Estado { get; set; } = EstadoDevolucion.Borrador;

    /// <summary>Totales congelados en el documento.</summary>
    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }

    public ICollection<LineaDevolucionCompra> Lineas { get; set; } = [];

    public bool EsEditable => Estado == EstadoDevolucion.Borrador;

    /// <summary>Rearma los totales del encabezado desde las lineas.</summary>
    public void Recalcular()
    {
        Subtotal = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Subtotal));
        Itbis = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Itbis));
        Total = Subtotal + Itbis;
    }
}
