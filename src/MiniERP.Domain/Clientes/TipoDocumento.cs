namespace MiniERP.Domain.Clientes;

/// <summary>
/// Documento de identificacion del cliente.
/// </summary>
public enum TipoDocumento
{
    /// <summary>Sin identificar. El cliente de contado que solo pide su factura de consumo.</summary>
    Ninguno = 0,

    /// <summary>Cedula de identidad y electoral: 11 digitos.</summary>
    Cedula = 1,

    /// <summary>Registro Nacional del Contribuyente: 9 digitos.</summary>
    Rnc = 2,

    /// <summary>Pasaporte, para extranjeros.</summary>
    Pasaporte = 3
}
