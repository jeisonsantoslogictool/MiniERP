using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Compras;

/// <summary>
/// Registro inmutable de un pago que el comercio hace a un proveedor.
/// </summary>
/// <remarks>
/// Simetrico a <see cref="Clientes.Cobro"/>: el cobro baja lo que el cliente debe,
/// el pago baja lo que el comercio le debe al proveedor. Ambos dejan rastro del
/// balance antes y despues, para que la deuda sea auditable.
/// </remarks>
public class Pago : EntidadBase
{
    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public DateTime Fecha { get; set; } = RelojSimulado.UtcNow;

    /// <summary>Monto pagado, siempre positivo.</summary>
    public decimal Monto { get; set; }

    /// <summary>Balance que tenia el proveedor antes de este pago.</summary>
    public decimal BalanceAnterior { get; set; }

    /// <summary>Balance despues de aplicar el pago.</summary>
    public decimal BalanceResultante { get; set; }

    /// <summary>Nota o referencia del pago: numero de transferencia, cheque, etc.</summary>
    public string? Observacion { get; set; }

    /// <summary>Usuario que registro el pago.</summary>
    public string? UsuarioId { get; set; }
}
