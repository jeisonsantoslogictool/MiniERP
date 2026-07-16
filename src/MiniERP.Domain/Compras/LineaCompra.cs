using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Compras;

/// <summary>
/// Renglon de una compra: que producto, cuanto y a que costo.
/// </summary>
/// <remarks>
/// La tasa de ITBIS se copia del producto al capturar la linea en vez de leerse de el
/// al mostrar. Si manana el producto cambia de gravado a exento, la compra de hoy debe
/// seguir mostrando el impuesto que realmente se pago.
/// </remarks>
public class LineaCompra : EntidadBase
{
    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    /// <summary>Descripcion al momento de comprar, para que el documento no cambie despues.</summary>
    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }

    public decimal TasaItbis { get; set; }

    public decimal Subtotal => Cantidad * CostoUnitario;

    public decimal Itbis => Math.Round(Subtotal * TasaItbis, RetailConstants.DecimalesImporte, MidpointRounding.AwayFromZero);

    public decimal Total => Subtotal + Itbis;
}
