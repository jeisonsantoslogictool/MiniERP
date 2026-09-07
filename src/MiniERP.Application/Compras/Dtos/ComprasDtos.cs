using MiniERP.Domain.Clientes;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Shared;

namespace MiniERP.Application.Compras.Dtos;

// ---------- Proveedores ----------

public record ProveedorListaDto(
    int Id,
    string Codigo,
    string Nombre,
    TipoDocumento TipoDocumento,
    string? NumeroDocumento,
    string? Telefono,
    string? Contacto,
    int DiasCredito,
    decimal BalanceActual,
    bool Activo);

public class ProveedorFormDto
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Rnc;
    public string? NumeroDocumento { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Direccion { get; set; }
    public string? Contacto { get; set; }
    public int DiasCredito { get; set; }
    public bool Activo { get; set; } = true;

    /// <summary>Lo que el comercio le debe. Solo lectura: lo mueven las compras y los pagos.</summary>
    public decimal BalanceActual { get; set; }
}

public record FiltroProveedores(
    string? Texto = null,
    bool SoloActivos = true,
    int Pagina = 1,
    int TamanoPagina = 25)
{
    public int SaltarRegistros => (Math.Max(1, Pagina) - 1) * TamanoEfectivo;
    public int TamanoEfectivo => Math.Clamp(TamanoPagina, 1, 100);
}

// ---------- Compras ----------

public record CompraListaDto(
    int Id,
    string Numero,
    string Proveedor,
    DateTime Fecha,
    DateTime? FechaRecepcion,
    string? NcfProveedor,
    EstadoCompra Estado,
    CondicionPago Condicion,
    decimal Subtotal,
    decimal Itbis,
    decimal Total,
    int CantidadLineas);

public class CompraFormDto
{
    public int Id { get; set; }
    public string Numero { get; set; } = string.Empty;
    public int ProveedorId { get; set; }
    public string? ProveedorNombre { get; set; }
    public DateTime Fecha { get; set; } = RelojSimulado.UtcNow;
    public string? NcfProveedor { get; set; }
    public EstadoCompra Estado { get; set; } = EstadoCompra.Borrador;
    public CondicionPago Condicion { get; set; } = CondicionPago.Contado;
    public string? Observacion { get; set; }

    public List<LineaCompraFormDto> Lineas { get; set; } = [];

    public bool EsEditable => Estado == EstadoCompra.Borrador;

    public decimal Subtotal => Lineas.Sum(l => l.Subtotal);
    public decimal Itbis => Lineas.Sum(l => l.Itbis);
    public decimal Total => Subtotal + Itbis;
}

public class LineaCompraFormDto
{
    public int Id { get; set; }
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public bool PermiteDecimales { get; set; }
    public decimal Cantidad { get; set; }
    public decimal CostoUnitario { get; set; }
    public decimal TasaItbis { get; set; }

    public decimal Subtotal => Cantidad * CostoUnitario;
    public decimal Itbis => RetailConstants.RedondearImporte(Subtotal * TasaItbis);
    public decimal Total => Subtotal + Itbis;
}

public record FiltroCompras(
    string? Texto = null,
    int? ProveedorId = null,
    EstadoCompra? Estado = null,
    int Pagina = 1,
    int TamanoPagina = 25)
{
    public int SaltarRegistros => (Math.Max(1, Pagina) - 1) * TamanoEfectivo;
    public int TamanoEfectivo => Math.Clamp(TamanoPagina, 1, 100);
}

/// <summary>Producto tal como lo necesita el buscador de la orden de compra.</summary>
public record ProductoParaCompraDto(
    int Id,
    string Codigo,
    string Descripcion,
    string UnidadMedida,
    bool PermiteDecimales,
    decimal Costo,
    decimal TasaItbis,
    decimal Existencia);
