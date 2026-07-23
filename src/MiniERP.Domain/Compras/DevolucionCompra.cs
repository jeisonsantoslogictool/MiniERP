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

    /// <summary>
    /// Rastro del saldo del proveedor al confirmar. Un resultado negativo representa
    /// un crédito a favor del comercio cuando la mercancía ya se había pagado.
    /// </summary>
    public decimal BalanceAnterior { get; set; }
    public decimal BalanceResultante { get; set; }

    public ICollection<LineaDevolucionCompra> Lineas { get; set; } = [];

    public bool EsEditable => Estado == EstadoDevolucion.Borrador;

    /// <summary>Rearma los totales del encabezado desde las lineas.</summary>
    public void Recalcular()
    {
        Subtotal = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Subtotal));
        Itbis = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Itbis));
        Total = Subtotal + Itbis;
    }

    /// <summary>
    /// Confirma la devolucion: genera los movimientos de salida (que bajan la existencia)
    /// y congela el documento.
    /// </summary>
    /// <param name="productos">Productos de las lineas, indexados por Id, con seguimiento.</param>
    /// <param name="devolvibleMaximoPorLineaCompra">
    /// Cuanto queda por devolver de cada linea de compra (comprado menos ya devuelto
    /// confirmado), indexado por <c>LineaCompraId</c>. Lo calcula el servicio.
    /// </param>
    /// <param name="usuarioId">Responsable de la devolucion.</param>
    /// <returns>Los movimientos generados, para que la capa de datos los persista.</returns>
    public IReadOnlyList<MovimientoInventario> Confirmar(
        IReadOnlyDictionary<int, Producto> productos,
        IReadOnlyDictionary<int, decimal> devolvibleMaximoPorLineaCompra,
        string? usuarioId)
    {
        ArgumentNullException.ThrowIfNull(productos);
        ArgumentNullException.ThrowIfNull(devolvibleMaximoPorLineaCompra);

        if (Estado != EstadoDevolucion.Borrador)
            throw new InvalidOperationException(
                $"La devolución {Numero} está {Estado.ToString().ToLowerInvariant()} y no se puede confirmar.");

        if (Lineas.Count == 0)
            throw new InvalidOperationException($"La devolución {Numero} no tiene líneas.");

        if (string.IsNullOrWhiteSpace(Motivo))
            throw new InvalidOperationException($"La devolución {Numero} exige un motivo.");

        // Se valida todo antes de mover el inventario: si algo falla, no se toco nada.
        foreach (var linea in Lineas)
        {
            if (!productos.ContainsKey(linea.ProductoId))
                throw new InvalidOperationException(
                    $"Falta el producto de la línea '{linea.Descripcion}'.");

            if (!devolvibleMaximoPorLineaCompra.TryGetValue(linea.LineaCompraId, out var maximo))
                throw new InvalidOperationException(
                    $"La línea '{linea.Descripcion}' no corresponde a la compra que se devuelve.");

            if (linea.Cantidad > maximo)
                throw new InvalidOperationException(
                    $"No puedes devolver {linea.Cantidad} de '{linea.Descripcion}': solo quedan {maximo} por devolver.");
        }

        var movimientos = new List<MovimientoInventario>(Lineas.Count);

        foreach (var linea in Lineas)
        {
            var producto = productos[linea.ProductoId];

            var movimiento = new MovimientoInventario
            {
                Tipo = TipoMovimiento.DevolucionProveedor,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                Motivo = $"Devolución {Numero}: {Motivo}",
                ReferenciaTipo = "DEVOLUCION_COMPRA",
                ReferenciaId = Id,
                UsuarioId = usuarioId,
                CreadoPor = usuarioId
            };

            producto.AplicarMovimiento(movimiento);
            movimientos.Add(movimiento);
        }

        Estado = EstadoDevolucion.Confirmada;

        return movimientos;
    }
}
