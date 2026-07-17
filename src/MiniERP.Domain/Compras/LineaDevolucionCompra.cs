using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Compras;

/// <summary>
/// Renglon de una devolucion: que producto, cuanto y a que costo (el de la compra origen).
/// </summary>
public class LineaDevolucionCompra : EntidadBase
{
    public int DevolucionCompraId { get; set; }
    public DevolucionCompra? Devolucion { get; set; }

    /// <summary>Linea de la compra que se devuelve. Ancla el costo y el tope devolvible.</summary>
    public int LineaCompraId { get; set; }
    public LineaCompra? LineaCompra { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    /// <summary>Costo congelado desde la linea de compra: lo que el proveedor acredita.</summary>
    public decimal CostoUnitario { get; set; }

    public decimal TasaItbis { get; set; }

    public decimal Subtotal => Cantidad * CostoUnitario;

    public decimal Itbis => Math.Round(Subtotal * TasaItbis, RetailConstants.DecimalesImporte, MidpointRounding.AwayFromZero);

    public decimal Total => Subtotal + Itbis;
}
