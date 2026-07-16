using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Inventario;

/// <summary>
/// Articulo que el comercio compra y vende.
/// </summary>
/// <remarks>
/// La existencia vive aqui y no en una tabla aparte porque el comercio opera en una
/// sola ubicacion. Cada cambio queda respaldado por un <see cref="MovimientoInventario"/>,
/// de modo que el saldo siempre es reconstruible y las mermas quedan atribuidas.
/// </remarks>
public class Producto : EntidadBase
{
    /// <summary>Codigo interno con que el cajero busca el producto.</summary>
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Codigo de barras impreso en el empaque. Opcional: a granel no lo hay.</summary>
    public string? CodigoBarras { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public int UnidadMedidaId { get; set; }
    public UnidadMedida? UnidadMedida { get; set; }

    /// <summary>Costo de la ultima compra. Lo actualiza la recepcion de mercancia.</summary>
    public decimal Costo { get; set; }

    /// <summary>Precio de venta sin ITBIS.</summary>
    public decimal PrecioVenta { get; set; }

    /// <summary>
    /// Tasa de ITBIS aplicable: 0.18 general, 0.16 reducida, 0 exento.
    /// La canasta basica esta exenta, y un minimarket vende ambos tipos.
    /// </summary>
    public decimal TasaItbis { get; set; } = TasaItbisGeneral;

    /// <summary>Existencia disponible. Solo debe cambiarse via <see cref="AplicarMovimiento"/>.</summary>
    public decimal Existencia { get; set; }

    /// <summary>Umbral que dispara la alerta de reabastecimiento.</summary>
    public decimal ExistenciaMinima { get; set; }

    /// <summary>Los servicios no descuentan inventario.</summary>
    public bool ManejaInventario { get; set; } = true;

    public bool Activo { get; set; } = true;

    public ICollection<MovimientoInventario> Movimientos { get; set; } = [];

    public const decimal TasaItbisGeneral = 0.18m;

    /// <summary>Precio con ITBIS incluido, que es el que ve el cliente en la gondola.</summary>
    public decimal PrecioConItbis => PrecioVenta * (1 + TasaItbis);

    /// <summary>Ganancia bruta por unidad vendida.</summary>
    public decimal MargenUnitario => PrecioVenta - Costo;

    /// <summary>Margen sobre el precio de venta. Cero si el precio aun no se fija.</summary>
    public decimal MargenPorcentaje =>
        PrecioVenta == 0 ? 0 : MargenUnitario / PrecioVenta;

    /// <summary>Producto que alcanzo su umbral y hay que reponer.</summary>
    public bool RequiereReabastecimiento =>
        ManejaInventario && Activo && Existencia <= ExistenciaMinima;

    /// <summary>Se agoto por completo.</summary>
    public bool Agotado => ManejaInventario && Existencia <= 0;

    /// <summary>
    /// Aplica un movimiento sobre la existencia y deja el rastro en el propio movimiento.
    /// Es el unico camino por el que la existencia debe cambiar.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// El movimiento dejaria la existencia en negativo.
    /// </exception>
    public void AplicarMovimiento(MovimientoInventario movimiento)
    {
        ArgumentNullException.ThrowIfNull(movimiento);

        if (!ManejaInventario)
            return;

        var delta = movimiento.EsEntrada ? movimiento.Cantidad : -movimiento.Cantidad;
        var resultante = Existencia + delta;

        if (resultante < 0)
            throw new InvalidOperationException(
                $"El movimiento deja el producto '{Codigo}' en existencia negativa: " +
                $"{Existencia} {delta:+0.###;-0.###} = {resultante}.");

        movimiento.ExistenciaAnterior = Existencia;
        movimiento.ExistenciaResultante = resultante;
        movimiento.ProductoId = Id;

        Existencia = resultante;
    }
}
