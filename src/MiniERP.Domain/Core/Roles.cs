namespace MiniERP.Domain.Core;

/// <summary>
/// Roles del sistema. Se siembran al arrancar y no se crean desde la interfaz:
/// las politicas de acceso de cada modulo se escriben contra estos nombres.
/// </summary>
public static class Roles
{
    /// <summary>Acceso total, incluida la administracion de usuarios.</summary>
    public const string Administrador = "Administrador";

    /// <summary>Punto de venta y consulta de clientes y productos.</summary>
    public const string Cajero = "Cajero";

    /// <summary>Inventario y compras: recibe mercancia, ajusta y registra mermas.</summary>
    public const string Almacen = "Almacen";

    /// <summary>Consulta de finanzas y reportes, sin capacidad de modificar.</summary>
    public const string Supervisor = "Supervisor";

    public static readonly string[] Todos =
        [Administrador, Cajero, Almacen, Supervisor];
}
