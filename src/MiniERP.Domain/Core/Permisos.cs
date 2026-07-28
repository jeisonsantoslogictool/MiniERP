namespace MiniERP.Domain.Core;

/// <summary>
/// ANDAMIO PROVISIONAL — este archivo pertenece al nucleo de permisos, que es de Jeison
/// (rama jeison/seguridad, Fase A del addendum Docs/Seguridad-Permisos.md).
/// Se escribio aqui solo para que el gateo de Finanzas compile y se pueda probar antes de
/// que ese nucleo entre a main. Cuando entre, este archivo SE BORRA y queda el de Jeison.
/// Ver Docs/Andamio-Permisos.md.
/// </summary>
/// <remarks>
/// Cada permiso es una accion concreta, no un rol. El rol solo sirve de plantilla al crear
/// un empleado; lo que se evalua al entrar a una pantalla es el permiso. Asi el dueno puede
/// decir "este cajero solo factura" sin inventar un rol nuevo para cada empleado.
/// El texto de cada constante es el que se guarda como claim de tipo <see cref="TipoDeClaim"/>
/// en AspNetUserClaims, y el mismo que se escribe en [Authorize(Policy = ...)].
/// </remarks>
public static class Permisos
{
    /// <summary>Tipo de claim con el que se guarda cada permiso concedido a un usuario.</summary>
    public const string TipoDeClaim = "permiso";

    // ---- Ventas -----------------------------------------------------------------
    public const string VentasVer = "ventas.ver";
    public const string VentasFacturar = "ventas.facturar";
    public const string VentasAnular = "ventas.anular";
    public const string VentasNcf = "ventas.ncf";

    // ---- Inventario -------------------------------------------------------------
    public const string InventarioVer = "inventario.ver";
    public const string InventarioEditar = "inventario.editar";
    public const string InventarioAjustar = "inventario.ajustar";

    // ---- Compras ----------------------------------------------------------------
    public const string ComprasVer = "compras.ver";
    public const string ComprasRecibir = "compras.recibir";
    public const string ComprasPagar = "compras.pagar";
    public const string ComprasDevolver = "compras.devolver";

    // ---- Clientes ---------------------------------------------------------------
    public const string ClientesVer = "clientes.ver";
    public const string ClientesEditar = "clientes.editar";
    public const string ClientesCobrar = "clientes.cobrar";

    // ---- Finanzas ---------------------------------------------------------------

    /// <summary>Consultar los reportes: ingresos, rentabilidad, estado de resultados y flujo.</summary>
    public const string FinanzasVer = "finanzas.ver";

    /// <summary>Registrar, editar o eliminar egresos y sus categorias.</summary>
    public const string FinanzasEgreso = "finanzas.egreso";

    // ---- Sistema ----------------------------------------------------------------
    public const string SistemaUsuarios = "sistema.usuarios";

    /// <summary>
    /// Todos los permisos del sistema. Lo usa el proveedor de politicas para saber que
    /// nombre es un permiso y que nombre no lo es.
    /// </summary>
    public static readonly string[] Todos =
    [
        VentasVer, VentasFacturar, VentasAnular, VentasNcf,
        InventarioVer, InventarioEditar, InventarioAjustar,
        ComprasVer, ComprasRecibir, ComprasPagar, ComprasDevolver,
        ClientesVer, ClientesEditar, ClientesCobrar,
        FinanzasVer, FinanzasEgreso,
        SistemaUsuarios
    ];

    /// <summary>Indica si un nombre de politica corresponde a un permiso del catalogo.</summary>
    public static bool Existe(string nombre) =>
        Array.IndexOf(Todos, nombre) >= 0;
}
