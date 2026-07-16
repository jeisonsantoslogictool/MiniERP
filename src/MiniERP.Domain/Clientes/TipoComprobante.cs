namespace MiniERP.Domain.Clientes;

/// <summary>
/// Tipo de comprobante fiscal que corresponde al cliente, segun la codificacion
/// de la Direccion General de Impuestos Internos.
/// </summary>
/// <remarks>
/// El valor numerico es el codigo oficial de la DGII y forma parte del NCF, asi que
/// no debe reasignarse. El comprobante por defecto de un minimarket es el de consumo.
/// </remarks>
public enum TipoComprobante
{
    /// <summary>01 — Credito Fiscal. Para clientes que necesitan sustentar ITBIS.</summary>
    CreditoFiscal = 1,

    /// <summary>02 — Consumo. El del cliente de mostrador.</summary>
    Consumo = 2,

    /// <summary>14 — Regimen Especial.</summary>
    RegimenEspecial = 14,

    /// <summary>15 — Gubernamental.</summary>
    Gubernamental = 15
}
