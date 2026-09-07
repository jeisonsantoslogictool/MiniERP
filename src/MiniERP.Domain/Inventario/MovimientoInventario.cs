using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Inventario;

/// <summary>
/// Registro inmutable de un cambio de existencia.
/// </summary>
/// <remarks>
/// Guarda la existencia antes y despues, no solo la cantidad. Con eso el saldo de
/// cualquier producto es auditable hacia atras y una discrepancia se puede rastrear
/// hasta el movimiento exacto, el usuario y la hora en que ocurrio. Es la respuesta
/// directa a las mermas no detectadas y a la falta de rastro que describe el problema.
/// </remarks>
public class MovimientoInventario : EntidadBase
{
    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public DateTime Fecha { get; set; } = RelojSimulado.UtcNow;

    public TipoMovimiento Tipo { get; set; }

    /// <summary>Cantidad movida, siempre positiva. El signo lo determina <see cref="Tipo"/>.</summary>
    public decimal Cantidad { get; set; }

    /// <summary>Costo unitario al momento del movimiento, para valorar el inventario.</summary>
    public decimal CostoUnitario { get; set; }

    public decimal ExistenciaAnterior { get; set; }

    public decimal ExistenciaResultante { get; set; }

    /// <summary>Por que se hizo. Obligatorio en mermas y ajustes.</summary>
    public string? Motivo { get; set; }

    /// <summary>Documento que origino el movimiento: "FACTURA", "COMPRA", "AJUSTE".</summary>
    public string? ReferenciaTipo { get; set; }

    /// <summary>Identificador del documento origen.</summary>
    public int? ReferenciaId { get; set; }

    /// <summary>Usuario responsable. Sin esto la merma vuelve a ser anonima.</summary>
    public string? UsuarioId { get; set; }

    /// <summary>Movimientos que suman a la existencia.</summary>
    public bool EsEntrada => Tipo is TipoMovimiento.Entrada
        or TipoMovimiento.AjustePositivo
        or TipoMovimiento.DevolucionCliente;

    /// <summary>Valor monetario del movimiento al costo.</summary>
    public decimal ValorCosto => Cantidad * CostoUnitario;

    /// <summary>Tipos que exigen explicacion escrita, por ser discrecionales.</summary>
    public bool RequiereMotivo => Tipo is TipoMovimiento.Merma
        or TipoMovimiento.AjustePositivo
        or TipoMovimiento.AjusteNegativo;
}
