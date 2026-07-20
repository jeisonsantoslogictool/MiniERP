using MiniERP.Domain.Clientes;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Application.Ventas.Dtos;

/// <summary>Producto tal como lo necesita el cajero al buscar.</summary>
public record ProductoParaVentaDto(
    int Id,
    string Codigo,
    string? CodigoBarras,
    string Descripcion,
    string UnidadMedida,
    bool PermiteDecimales,
    decimal PrecioVenta,
    decimal TasaItbis,
    decimal Existencia,
    bool ManejaInventario)
{
    public decimal PrecioConItbis => PrecioVenta * (1 + TasaItbis);
}

/// <summary>Cliente tal como lo necesita el cajero al elegirlo.</summary>
public record ClienteParaVentaDto(
    int Id,
    string Codigo,
    string Nombre,
    string? NumeroDocumento,
    TipoComprobante TipoComprobante,
    decimal LimiteCredito,
    decimal BalanceActual,
    decimal CreditoDisponible,
    bool TieneCredito);

/// <summary>Renglon del carrito. Vive en la pantalla hasta que se cobra.</summary>
public class LineaVentaDto
{
    public int ProductoId { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string UnidadMedida { get; set; } = string.Empty;
    public bool PermiteDecimales { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal TasaItbis { get; set; }
    public decimal ExistenciaDisponible { get; set; }

    public decimal Subtotal => Cantidad * PrecioUnitario;
    public decimal Itbis => RetailConstants.RedondearImporte(Subtotal * TasaItbis);
    public decimal Total => Subtotal + Itbis;
}

/// <summary>El carrito completo, listo para cobrar.</summary>
public class VentaFormDto
{
    public int? ClienteId { get; set; }
    public string? ClienteNombre { get; set; }
    public CondicionPago Condicion { get; set; } = CondicionPago.Contado;
    public decimal MontoRecibido { get; set; }

    public List<LineaVentaDto> Lineas { get; set; } = [];

    public decimal Subtotal => RetailConstants.RedondearImporte(Lineas.Sum(l => l.Subtotal));
    public decimal Itbis => RetailConstants.RedondearImporte(Lineas.Sum(l => l.Itbis));
    public decimal Total => Subtotal + Itbis;

    public decimal Cambio => Condicion == CondicionPago.Contado && MontoRecibido > Total
        ? RetailConstants.RedondearImporte(MontoRecibido - Total)
        : 0;

    public decimal Faltante => Condicion == CondicionPago.Contado && MontoRecibido < Total
        ? RetailConstants.RedondearImporte(Total - MontoRecibido)
        : 0;

    public bool HayLineas => Lineas.Count > 0;
}

/// <summary>Lo que la pantalla muestra tras cobrar.</summary>
public record FacturaEmitidaDto(int Id, string Numero, string Ncf, decimal Total, decimal Cambio);

public record FacturaListaDto(
    int Id,
    string Numero,
    string? Ncf,
    string ClienteNombre,
    DateTime Fecha,
    EstadoFactura Estado,
    CondicionPago Condicion,
    decimal Total,
    decimal Margen,
    string? UsuarioId);

/// <summary>La factura completa, para verla o imprimirla.</summary>
public record FacturaDetalleDto(
    int Id,
    string Numero,
    string? Ncf,
    TipoComprobante TipoComprobante,
    string ClienteNombre,
    string? ClienteDocumento,
    DateTime Fecha,
    EstadoFactura Estado,
    CondicionPago Condicion,
    decimal Subtotal,
    decimal Itbis,
    decimal Total,
    decimal MontoRecibido,
    decimal Cambio,
    string? UsuarioId,
    string? MotivoAnulacion,
    IReadOnlyList<LineaFacturaDetalleDto> Lineas);

public record LineaFacturaDetalleDto(
    string Codigo,
    string Descripcion,
    string UnidadMedida,
    bool PermiteDecimales,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal TasaItbis,
    decimal Subtotal,
    decimal Itbis,
    decimal Total);

public record FiltroFacturas(
    string? Texto = null,
    EstadoFactura? Estado = null,
    DateTime? Desde = null,
    DateTime? Hasta = null,
    int Pagina = 1,
    int TamanoPagina = 25)
{
    public int SaltarRegistros => (Math.Max(1, Pagina) - 1) * TamanoEfectivo;
    public int TamanoEfectivo => Math.Clamp(TamanoPagina, 1, 100);
}

/// <summary>Una secuencia de NCF tal como la muestra la pantalla de administración.</summary>
public record SecuenciaNcfDto(
    int Id,
    TipoComprobante Tipo,
    string Prefijo,
    long Desde,
    long Hasta,
    long Actual,
    long Disponibles,
    DateTime FechaVencimiento,
    bool Activa,
    bool Agotada,
    bool Vencida,
    bool PorAgotarse,
    bool EsElectronica);

/// <summary>Datos para registrar un rango de NCF autorizado por la DGII.</summary>
public class RegistrarSecuenciaDto
{
    public TipoComprobante Tipo { get; set; } = TipoComprobante.Consumo;
    public string Prefijo { get; set; } = string.Empty;
    public long Desde { get; set; } = 1;
    public long Hasta { get; set; }
    public DateTime FechaVencimiento { get; set; } = DateTime.UtcNow.Date.AddYears(1);
}
