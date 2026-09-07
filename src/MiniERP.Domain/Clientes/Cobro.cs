using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Clientes;

/// <summary>
/// Registro inmutable de un abono que el cliente hace sobre su deuda.
/// </summary>
/// <remarks>
/// Guarda el balance antes y despues, igual que <see cref="Inventario.MovimientoInventario"/>
/// hace con la existencia. Con eso la deuda de cualquier cliente es auditable hacia atras
/// y una discrepancia se puede rastrear hasta el cobro exacto, el usuario y la hora.
/// Es lo que reemplaza al cuaderno de fiados que describe el planteamiento del problema.
/// </remarks>
public class Cobro : EntidadBase
{
    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public DateTime Fecha { get; set; } = RelojSimulado.UtcNow;

    /// <summary>Monto abonado, siempre positivo.</summary>
    public decimal Monto { get; set; }

    /// <summary>Balance que tenia el cliente antes de este cobro.</summary>
    public decimal BalanceAnterior { get; set; }

    /// <summary>Balance despues de aplicar el cobro.</summary>
    public decimal BalanceResultante { get; set; }

    /// <summary>Nota o referencia del cobro: numero de recibo, forma de pago, etc.</summary>
    public string? Observacion { get; set; }

    /// <summary>Usuario que registro el cobro.</summary>
    public string? UsuarioId { get; set; }
}
