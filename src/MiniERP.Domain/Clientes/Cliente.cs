using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Clientes;

/// <summary>
/// Comprador registrado del comercio.
/// </summary>
/// <remarks>
/// El credito es el motivo principal por el que un minimarket registra a un cliente:
/// el que paga de contado rara vez se registra. Por eso el limite y el balance viven
/// aqui, y sustituyen al cuaderno de fiados que describe el planteamiento del problema.
/// </remarks>
public class Cliente : EntidadBase
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Ninguno;

    /// <summary>Numero del documento, sin guiones. Requerido para credito fiscal.</summary>
    public string? NumeroDocumento { get; set; }

    /// <summary>Comprobante que se le emite por defecto.</summary>
    public TipoComprobante TipoComprobante { get; set; } = TipoComprobante.Consumo;

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Direccion { get; set; }

    /// <summary>Techo de deuda. En cero, el cliente es solo de contado.</summary>
    public decimal LimiteCredito { get; set; }

    /// <summary>Deuda vigente. La mueven las ventas a credito y los cobros.</summary>
    public decimal BalanceActual { get; set; }

    /// <summary>Plazo de pago acordado.</summary>
    public int DiasCredito { get; set; }

    public bool Activo { get; set; } = true;

    /// <summary>Cliente habilitado para llevar fiado.</summary>
    public bool TieneCredito => LimiteCredito > 0;

    /// <summary>Cuanto puede fiar todavia. Nunca negativo.</summary>
    public decimal CreditoDisponible => Math.Max(0, LimiteCredito - BalanceActual);

    /// <summary>Se paso del techo, normalmente por un aumento de limite ya consumido.</summary>
    public bool ExcedeLimite => BalanceActual > LimiteCredito;

    /// <summary>
    /// El credito fiscal debe sustentarse con RNC: sin el, la DGII no acepta el comprobante.
    /// </summary>
    public bool ComprobanteEstaSustentado =>
        TipoComprobante != TipoComprobante.CreditoFiscal
        || (TipoDocumento == TipoDocumento.Rnc && !string.IsNullOrWhiteSpace(NumeroDocumento));

    /// <summary>
    /// Indica si el cliente puede asumir un cargo a credito por el monto dado.
    /// </summary>
    public bool PuedeAsumirCredito(decimal monto) =>
        Activo && TieneCredito && monto > 0 && monto <= CreditoDisponible;

    /// <summary>
    /// Valida el largo del documento segun su tipo. No verifica contra la DGII:
    /// la validacion en linea del RNC queda fuera del alcance del proyecto.
    /// </summary>
    public bool DocumentoTieneFormatoValido()
    {
        if (TipoDocumento == TipoDocumento.Ninguno)
            return string.IsNullOrWhiteSpace(NumeroDocumento);

        if (string.IsNullOrWhiteSpace(NumeroDocumento))
            return false;

        var numero = NumeroDocumento.Replace("-", string.Empty).Trim();

        return TipoDocumento switch
        {
            TipoDocumento.Cedula => numero.Length == 11 && numero.All(char.IsDigit),
            TipoDocumento.Rnc => numero.Length == 9 && numero.All(char.IsDigit),
            TipoDocumento.Pasaporte => numero.Length is >= 5 and <= 20,
            _ => false
        };
    }
}
