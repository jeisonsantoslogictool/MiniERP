using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Compras;

/// <summary>
/// Compra de mercancia a un proveedor. Nace como orden y, al recibirse, mueve el inventario.
/// </summary>
public class Compra : EntidadBase
{
    public string Numero { get; set; } = string.Empty;

    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public DateTime Fecha { get; set; } = RelojSimulado.UtcNow;

    public DateTime? FechaRecepcion { get; set; }

    /// <summary>
    /// NCF que emite el proveedor. Es lo que sustenta el ITBIS pagado ante la DGII,
    /// asi que se guarda aunque el reporte 606 quede fuera del alcance.
    /// </summary>
    public string? NcfProveedor { get; set; }

    public EstadoCompra Estado { get; set; } = EstadoCompra.Borrador;

    public CondicionPago Condicion { get; set; } = CondicionPago.Contado;

    /// <summary>Totales congelados en el documento, para no rearmarlos desde las lineas en cada reporte.</summary>
    public decimal Subtotal { get; set; }

    public decimal Itbis { get; set; }

    public decimal Total { get; set; }

    public string? Observacion { get; set; }

    public ICollection<LineaCompra> Lineas { get; set; } = [];

    public bool EsEditable => Estado == EstadoCompra.Borrador;

    public bool EstaRecibida => Estado == EstadoCompra.Recibida;

    /// <summary>
    /// Rearma los totales del encabezado desde las lineas. Se llama antes de guardar.
    /// </summary>
    public void Recalcular()
    {
        Subtotal = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Subtotal));
        Itbis = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Itbis));
        Total = Subtotal + Itbis;
    }

    /// <summary>
    /// Recibe la mercancia: genera los movimientos de entrada, actualiza el costo de
    /// cada producto y congela el documento.
    /// </summary>
    /// <param name="productos">Los productos de las lineas, indexados por su Id.</param>
    /// <returns>Los movimientos generados, para que la capa de datos los persista.</returns>
    /// <exception cref="InvalidOperationException">
    /// La compra ya se recibio o se anulo, no tiene lineas, o falta algun producto.
    /// </exception>
    public IReadOnlyList<MovimientoInventario> Recibir(
        IReadOnlyDictionary<int, Producto> productos,
        string? usuarioId)
    {
        ArgumentNullException.ThrowIfNull(productos);

        if (Estado != EstadoCompra.Borrador)
            throw new InvalidOperationException(
                $"La compra {Numero} esta {Estado.ToString().ToLowerInvariant()} y no se puede recibir.");

        if (Lineas.Count == 0)
            throw new InvalidOperationException($"La compra {Numero} no tiene lineas.");

        var movimientos = new List<MovimientoInventario>(Lineas.Count);

        foreach (var linea in Lineas)
        {
            if (!productos.TryGetValue(linea.ProductoId, out var producto))
                throw new InvalidOperationException(
                    $"Falta el producto de la linea '{linea.Descripcion}'.");

            // El costo se actualiza antes de mover la existencia: el promedio ponderado
            // necesita saber cuanto habia y a que costo, y AplicarMovimiento ya cambio eso.
            producto.Costo = CalcularCostoPromedio(producto, linea);

            var movimiento = new MovimientoInventario
            {
                Tipo = TipoMovimiento.Entrada,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                Motivo = $"Compra {Numero}",
                ReferenciaTipo = "COMPRA",
                ReferenciaId = Id,
                UsuarioId = usuarioId,
                CreadoPor = usuarioId
            };

            producto.AplicarMovimiento(movimiento);
            movimientos.Add(movimiento);
        }

        Estado = EstadoCompra.Recibida;
        FechaRecepcion = RelojSimulado.UtcNow;

        return movimientos;
    }

    /// <summary>
    /// Costo promedio ponderado tras recibir una linea.
    /// </summary>
    /// <remarks>
    /// Se prefiere al ultimo costo porque es el que responde de verdad a la pregunta
    /// del planteamiento del problema: cual es el costo real de la mercancia. Con
    /// ultimo costo, comprar diez a 30 y luego diez a 40 valoraria las veinte a 40,
    /// inflando el costo y escondiendo el margen que se gano en las primeras diez.
    ///
    /// Un producto que no maneja inventario, o cuya existencia esta en cero o en
    /// negativo, no tiene con que promediar: toma el costo recibido tal cual.
    /// </remarks>
    private static decimal CalcularCostoPromedio(Producto producto, LineaCompra linea)
    {
        if (!producto.ManejaInventario || producto.Existencia <= 0)
            return linea.CostoUnitario;

        var valorActual = producto.Existencia * producto.Costo;
        var valorEntrante = linea.Cantidad * linea.CostoUnitario;
        var unidadesTotales = producto.Existencia + linea.Cantidad;

        if (unidadesTotales <= 0)
            return linea.CostoUnitario;

        return Math.Round(
            (valorActual + valorEntrante) / unidadesTotales,
            RetailConstants.DecimalesPrecio,
            RetailConstants.ModoRedondeo);
    }

    /// <summary>
    /// Anula una orden que aun no se ha recibido.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Ya se recibio: el inventario se movio y hay que devolver, no anular.
    /// </exception>
    public void Anular()
    {
        if (Estado == EstadoCompra.Recibida)
            throw new InvalidOperationException(
                $"La compra {Numero} ya se recibio y el inventario se movio. " +
                "Registra una devolucion al proveedor en vez de anularla.");

        Estado = EstadoCompra.Anulada;
    }
}
