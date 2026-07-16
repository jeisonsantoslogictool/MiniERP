using MiniERP.Domain.Clientes;

namespace MiniERP.Application.Clientes.Dtos;

/// <summary>Fila del listado de clientes.</summary>
public record ClienteListaDto(
    int Id,
    string Codigo,
    string Nombre,
    TipoDocumento TipoDocumento,
    string? NumeroDocumento,
    TipoComprobante TipoComprobante,
    string? Telefono,
    decimal LimiteCredito,
    decimal BalanceActual,
    decimal CreditoDisponible,
    bool TieneCredito,
    bool ExcedeLimite,
    bool Activo);

/// <summary>Datos que captura el formulario de cliente.</summary>
public class ClienteFormDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Ninguno;

    public string? NumeroDocumento { get; set; }

    public TipoComprobante TipoComprobante { get; set; } = TipoComprobante.Consumo;

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Direccion { get; set; }

    public decimal LimiteCredito { get; set; }

    public int DiasCredito { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>
    /// Deuda vigente. Solo lectura: la mueven las ventas a credito y los cobros,
    /// nunca la edicion manual del cliente.
    /// </summary>
    public decimal BalanceActual { get; set; }
}

/// <param name="Texto">Busca en codigo, nombre y documento.</param>
/// <param name="SoloConCredito">Deja solo a los que llevan fiado.</param>
/// <param name="SoloConDeuda">Deja solo a los que deben algo hoy.</param>
public record FiltroClientes(
    string? Texto = null,
    bool SoloConCredito = false,
    bool SoloConDeuda = false,
    bool SoloActivos = true,
    int Pagina = 1,
    int TamanoPagina = 25)
{
    public const int MaxTamanoPagina = 100;

    public int SaltarRegistros => (Math.Max(1, Pagina) - 1) * TamanoEfectivo;

    public int TamanoEfectivo => Math.Clamp(TamanoPagina, 1, MaxTamanoPagina);
}

/// <summary>Resumen de la cartera, para el panel del modulo.</summary>
public record ResumenCarteraDto(
    int TotalClientes,
    int ConCredito,
    int ConDeuda,
    int ExcedenLimite,
    decimal DeudaTotal,
    decimal CreditoOtorgado);
