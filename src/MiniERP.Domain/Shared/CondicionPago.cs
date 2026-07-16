namespace MiniERP.Domain.Shared;

/// <summary>
/// Forma en que se salda un documento. La comparten compras y ventas.
/// </summary>
public enum CondicionPago
{
    /// <summary>Se paga al momento.</summary>
    Contado = 1,

    /// <summary>Queda pendiente y afecta el balance del cliente o del proveedor.</summary>
    Credito = 2
}
