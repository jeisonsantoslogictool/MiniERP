using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Gasto operativo que NO pasa por el inventario: alquiler, luz, agua, sueldos, transporte.
/// No es una compra ni un pago a proveedor. Se registra ya pagado.
/// </summary>
public class Egreso : EntidadBase
{
    public DateTime Fecha { get; set; } = RelojSimulado.UtcNow;

    public int CategoriaEgresoId { get; set; }
    public CategoriaEgreso? CategoriaEgreso { get; set; }

    public decimal Monto { get; set; }

    /// <summary>Nota opcional: "Sueldo de Maria, primera quincena".</summary>
    public string? Descripcion { get; set; }

    /// <summary>Usuario que registro el gasto (responsable). Como Pago.UsuarioId.</summary>
    public string? UsuarioId { get; set; }

    /// <summary>
    /// Registra un gasto construyendolo con sus datos. La descripcion vacia queda nula,
    /// y el usuario responsable se guarda en UsuarioId y en CreadoPor.
    /// </summary>
    public static Egreso Registrar(
        decimal monto, DateTime fecha, int categoriaEgresoId,
        string? descripcion, string? usuarioId)
    {
        if (monto <= 0)
            throw new InvalidOperationException("El monto del egreso debe ser mayor que cero.");

        if (categoriaEgresoId == 0)
            throw new InvalidOperationException("El egreso debe tener una categoría.");

        return new Egreso
        {
            Monto = monto,
            Fecha = fecha,
            CategoriaEgresoId = categoriaEgresoId,
            Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
            UsuarioId = usuarioId,
            CreadoPor = usuarioId
        };
    }
}
