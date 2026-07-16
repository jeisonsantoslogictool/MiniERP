namespace MiniERP.Domain.Inventario;

/// <summary>
/// Razon por la que cambia la existencia de un producto.
/// </summary>
public enum TipoMovimiento
{
    /// <summary>Recepcion de mercancia de un proveedor.</summary>
    Entrada = 1,

    /// <summary>Venta al cliente.</summary>
    Salida = 2,

    /// <summary>El conteo fisico encontro mas de lo registrado.</summary>
    AjustePositivo = 3,

    /// <summary>El conteo fisico encontro menos de lo registrado.</summary>
    AjusteNegativo = 4,

    /// <summary>
    /// Perdida por vencimiento, dano o robo. Se separa del ajuste negativo a proposito:
    /// las mermas no detectadas son uno de los problemas que este sistema busca resolver,
    /// y mezclarlas con los ajustes las volveria invisibles otra vez.
    /// </summary>
    Merma = 5,

    /// <summary>El cliente devuelve mercancia.</summary>
    DevolucionCliente = 6,

    /// <summary>Se devuelve mercancia al proveedor.</summary>
    DevolucionProveedor = 7
}
