using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Ventas;

/// <summary>
/// Renglon de una factura: que se vendio, cuanto y a que precio.
/// </summary>
/// <remarks>
/// Guarda el costo unitario congelado al momento de vender, no solo el precio. Sin eso
/// no se puede saber cuanto se gano en esta venta: el costo del producto cambia con cada
/// compra por el promedio ponderado, asi que leerlo hoy daria un margen distinto al real.
/// Es lo que hace calculable la rentabilidad del Capitulo V.
/// </remarks>
public class LineaFactura : EntidadBase
{
    public int FacturaId { get; set; }
    public Factura? Factura { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    /// <summary>Descripcion al momento de vender, para que el documento no cambie despues.</summary>
    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal TasaItbis { get; set; }

    /// <summary>Costo del producto al momento de vender. Congelado: es la base del margen.</summary>
    public decimal CostoUnitario { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;

    public decimal Itbis => RetailConstants.RedondearImporte(Subtotal * TasaItbis);

    public decimal Total => Subtotal + Itbis;

    public decimal CostoTotal => Cantidad * CostoUnitario;

    /// <summary>Ganancia bruta del renglon, sin ITBIS: el impuesto no es del comercio.</summary>
    public decimal Margen => Subtotal - CostoTotal;
}
