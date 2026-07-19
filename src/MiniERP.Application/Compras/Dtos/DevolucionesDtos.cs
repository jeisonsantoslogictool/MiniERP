using MiniERP.Domain.Compras;
using MiniERP.Domain.Shared;

namespace MiniERP.Application.Compras.Dtos;

// ---------- Devoluciones a proveedor ----------

public record DevolucionCompraListaDto(
    int Id,
    string Numero,
    string Proveedor,
    string CompraNumero,
    DateTime Fecha,
    EstadoDevolucion Estado,
    string Motivo,
    decimal Total,
    int CantidadLineas);

public class DevolucionCompraFormDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;

    public int CompraId { get; set; }
    public string CompraNumero { get; set; } = string.Empty;

    public int ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    public string Motivo { get; set; } = string.Empty;

    public EstadoDevolucion Estado { get; set; } = EstadoDevolucion.Borrador;

    public List<LineaDevolucionFormDto> Lineas { get; set; } = [];

    public bool EsEditable => Estado == EstadoDevolucion.Borrador;

    public decimal Subtotal => Lineas.Sum(l => l.Subtotal);
    public decimal Itbis => Lineas.Sum(l => l.Itbis);
    public decimal Total => Subtotal + Itbis;
}

public class LineaDevolucionFormDto
{
    public int Id { get; set; }

    /// <summary>Linea de la compra que se devuelve. Ancla el costo y el tope devolvible.</summary>
    public int LineaCompraId { get; set; }

    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public bool PermiteDecimales { get; set; }

    /// <summary>Cuanto se compro en esta linea.</summary>
    public decimal CantidadComprada { get; set; }

    /// <summary>Cuanto queda por devolver: comprado menos lo ya devuelto. Solo lectura en la UI.</summary>
    public decimal Devolvible { get; set; }

    /// <summary>Cuanto se esta devolviendo ahora.</summary>
    public decimal Cantidad { get; set; }

    public decimal CostoUnitario { get; set; }
    public decimal TasaItbis { get; set; }

    public decimal Subtotal => Cantidad * CostoUnitario;
    public decimal Itbis => RetailConstants.RedondearImporte(Subtotal * TasaItbis);
    public decimal Total => Subtotal + Itbis;
}

public record FiltroDevoluciones(
    string? Texto = null,
    int? ProveedorId = null,
    EstadoDevolucion? Estado = null,
    int Pagina = 1,
    int TamanoPagina = 25)
{
    public int SaltarRegistros => (Math.Max(1, Pagina) - 1) * TamanoEfectivo;
    public int TamanoEfectivo => Math.Clamp(TamanoPagina, 1, 100);
}
