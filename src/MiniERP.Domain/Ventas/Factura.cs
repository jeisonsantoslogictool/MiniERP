using MiniERP.Domain.Clientes;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Ventas;

/// <summary>
/// Venta al cliente. Al emitirse consume su NCF y descuenta del inventario.
/// </summary>
public class Factura : EntidadBase
{
    public string Numero { get; set; } = string.Empty;

    /// <summary>Comprobante fiscal asignado al emitir. Nunca se reasigna ni se reutiliza.</summary>
    public string? Ncf { get; set; }

    public TipoComprobante TipoComprobante { get; set; } = TipoComprobante.Consumo;

    /// <summary>Null cuando es el cliente de mostrador que no se registra.</summary>
    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    /// <summary>
    /// Nombre y documento congelados al facturar. Si el cliente se muda o corrige su
    /// RNC manana, la factura de hoy debe seguir mostrando lo que se imprimio.
    /// </summary>
    public string ClienteNombre { get; set; } = "Cliente de contado";

    public string? ClienteDocumento { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public EstadoFactura Estado { get; set; } = EstadoFactura.Emitida;

    public CondicionPago Condicion { get; set; } = CondicionPago.Contado;

    public decimal Subtotal { get; set; }

    public decimal Itbis { get; set; }

    public decimal Total { get; set; }

    /// <summary>Costo de lo vendido, congelado. Es lo que hace calculable la rentabilidad.</summary>
    public decimal CostoTotal { get; set; }

    public decimal MontoRecibido { get; set; }

    public decimal Cambio { get; set; }

    /// <summary>Cajero que la emitio.</summary>
    public string? UsuarioId { get; set; }

    public string? MotivoAnulacion { get; set; }

    public ICollection<LineaFactura> Lineas { get; set; } = [];

    public bool EstaAnulada => Estado == EstadoFactura.Anulada;

    /// <summary>Ganancia bruta de la venta, sin ITBIS.</summary>
    public decimal Margen => Subtotal - CostoTotal;

    public decimal MargenPorcentaje => Subtotal == 0 ? 0 : Margen / Subtotal;

    /// <summary>
    /// Emite la factura: congela costos, descuenta del inventario y asigna el comprobante.
    /// </summary>
    /// <param name="productos">Los productos de las lineas, indexados por su Id.</param>
    /// <param name="ncf">Comprobante ya reservado de forma atomica por la capa de datos.</param>
    /// <returns>Los movimientos de salida generados, para que la capa de datos los persista.</returns>
    /// <exception cref="InvalidOperationException">
    /// Sin lineas, falta un producto, o no hay existencia suficiente.
    /// </exception>
    public IReadOnlyList<MovimientoInventario> Emitir(
        IReadOnlyDictionary<int, Producto> productos,
        string ncf,
        string? usuarioId)
    {
        ArgumentNullException.ThrowIfNull(productos);
        ArgumentException.ThrowIfNullOrWhiteSpace(ncf);

        if (Lineas.Count == 0)
            throw new InvalidOperationException("La factura no tiene lineas.");

        var movimientos = new List<MovimientoInventario>(Lineas.Count);

        foreach (var linea in Lineas)
        {
            if (!productos.TryGetValue(linea.ProductoId, out var producto))
                throw new InvalidOperationException(
                    $"Falta el producto de la linea '{linea.Descripcion}'.");

            // El costo se congela antes de mover nada: es lo que se usara para el margen,
            // y debe ser el que el producto tenia al momento exacto de la venta.
            linea.CostoUnitario = producto.Costo;

            // Un servicio (recarga, fotocopia) se factura y lleva costo, pero no tiene
            // kardex. AplicarMovimiento lo ignoraria sin asignarle ProductoId, y persistir
            // ese movimiento huerfano viola la clave foranea y tumba la venta entera (D-01).
            if (!producto.ManejaInventario)
                continue;

            var movimiento = new MovimientoInventario
            {
                Tipo = TipoMovimiento.Salida,
                Cantidad = linea.Cantidad,
                CostoUnitario = producto.Costo,
                Motivo = $"Factura {ncf}",
                ReferenciaTipo = "FACTURA",
                // ReferenciaId se queda vacio aqui a proposito. Emitir corre sobre una
                // factura que todavia no se ha guardado, asi que su Id vale 0: ponerlo
                // ahora escribiria un cero en cada movimiento y ninguno podria rastrearse
                // hasta su documento. Lo enlaza el caso de uso al guardar, cuando el Id ya
                // existe. En Recibir y en Anular si se asigna, porque ahi el documento ya
                // esta en la base.
                UsuarioId = usuarioId,
                CreadoPor = usuarioId
            };

            // Lanza si no hay existencia. Al ocurrir dentro del bucle, ninguna linea
            // posterior se aplica; la transaccion de la capa de datos revierte las previas.
            producto.AplicarMovimiento(movimiento);
            movimientos.Add(movimiento);
        }

        Recalcular();

        Ncf = ncf;
        Estado = EstadoFactura.Emitida;
        UsuarioId = usuarioId;

        return movimientos;
    }

    /// <summary>
    /// Rearma los totales desde las lineas. El cambio solo tiene sentido al contado.
    /// </summary>
    public void Recalcular()
    {
        Subtotal = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Subtotal));
        Itbis = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Itbis));
        Total = Subtotal + Itbis;
        CostoTotal = RetailConstants.RedondearImporte(Lineas.Sum(l => l.CostoTotal));

        Cambio = Condicion == CondicionPago.Contado && MontoRecibido > Total
            ? RetailConstants.RedondearImporte(MontoRecibido - Total)
            : 0;
    }

    /// <summary>
    /// Anula la factura y devuelve los movimientos que reponen el inventario.
    /// </summary>
    /// <remarks>
    /// El NCF no se libera: ante la DGII queda como comprobante anulado y el rango sigue
    /// avanzando. Reutilizarlo produciria dos documentos con el mismo numero.
    ///
    /// La reposicion entra como devolucion de cliente y no como ajuste, para que el kardex
    /// diga por que volvio la mercancia.
    /// </remarks>
    public IReadOnlyList<MovimientoInventario> Anular(
        IReadOnlyDictionary<int, Producto> productos,
        string motivo,
        string? usuarioId)
    {
        ArgumentNullException.ThrowIfNull(productos);

        if (Estado == EstadoFactura.Anulada)
            throw new InvalidOperationException($"La factura {Ncf} ya esta anulada.");

        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Anular una factura exige un motivo.");

        var movimientos = new List<MovimientoInventario>(Lineas.Count);

        foreach (var linea in Lineas)
        {
            if (!productos.TryGetValue(linea.ProductoId, out var producto))
                throw new InvalidOperationException(
                    $"Falta el producto de la linea '{linea.Descripcion}'.");

            // Un servicio no salio del inventario al emitir, asi que no hay nada que reponer.
            if (!producto.ManejaInventario)
                continue;

            var movimiento = new MovimientoInventario
            {
                Tipo = TipoMovimiento.DevolucionCliente,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                Motivo = $"Anulacion de {Ncf}: {motivo}",
                ReferenciaTipo = "ANULACION",
                ReferenciaId = Id,
                UsuarioId = usuarioId,
                CreadoPor = usuarioId
            };

            producto.AplicarMovimiento(movimiento);
            movimientos.Add(movimiento);
        }

        Estado = EstadoFactura.Anulada;
        MotivoAnulacion = motivo.Trim();
        FechaModificacion = DateTime.UtcNow;
        ModificadoPor = usuarioId;

        return movimientos;
    }
}
