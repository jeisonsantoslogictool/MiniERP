using MiniERP.Domain.Clientes;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Compras;

/// <summary>
/// Quien le vende mercancia al comercio.
/// </summary>
/// <remarks>
/// Reutiliza TipoDocumento del modulo de clientes en vez de declarar un enum gemelo:
/// una cedula es una cedula, la emita quien la emita. El balance funciona igual que el
/// del cliente pero al reves: es lo que el comercio debe, no lo que le deben.
/// </remarks>
public class Proveedor : EntidadBase
{
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    public TipoDocumento TipoDocumento { get; set; } = TipoDocumento.Rnc;

    public string? NumeroDocumento { get; set; }

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public string? Direccion { get; set; }

    /// <summary>Persona de contacto en el suplidor.</summary>
    public string? Contacto { get; set; }

    /// <summary>Plazo que el proveedor concede al comercio.</summary>
    public int DiasCredito { get; set; }

    /// <summary>Lo que el comercio le debe. Lo mueven las compras a credito y los pagos.</summary>
    public decimal BalanceActual { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Compra> Compras { get; set; } = [];

    public bool TieneDeuda => BalanceActual > 0;

    /// <summary>
    /// Valida el largo del documento segun su tipo. Un proveedor formal trae RNC;
    /// el que vende en la puerta puede traer solo cedula.
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
