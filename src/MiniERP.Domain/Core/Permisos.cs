namespace MiniERP.Domain.Core;

/// <summary>
/// Catalogo de permisos del sistema. Un permiso es un derecho fino sobre una accion de un
/// modulo (p. ej. <c>ventas.facturar</c>). Se guardan como claims del usuario (tipo
/// <see cref="ClaimType"/>) y las politicas de autorizacion se escriben contra estas
/// constantes. Los roles de <see cref="Roles"/> son plantillas que pre-marcan un juego de
/// permisos al crear el usuario; luego se afinan por usuario.
/// </summary>
public static class Permisos
{
    /// <summary>Tipo de claim con que se almacena cada permiso concedido a un usuario.</summary>
    public const string ClaimType = "permiso";

    // Ventas
    public const string VentasFacturar = "ventas.facturar";
    public const string VentasAnular = "ventas.anular";
    public const string VentasVer = "ventas.ver";
    public const string VentasNcf = "ventas.ncf";

    // Inventario
    public const string InventarioVer = "inventario.ver";
    public const string InventarioEditar = "inventario.editar";
    public const string InventarioAjustar = "inventario.ajustar";

    // Compras
    public const string ComprasVer = "compras.ver";
    public const string ComprasRecibir = "compras.recibir";
    public const string ComprasPagar = "compras.pagar";
    public const string ComprasDevolver = "compras.devolver";

    // Clientes
    public const string ClientesVer = "clientes.ver";
    public const string ClientesEditar = "clientes.editar";
    public const string ClientesCobrar = "clientes.cobrar";

    // Finanzas
    public const string FinanzasVer = "finanzas.ver";
    public const string FinanzasEgreso = "finanzas.egreso";

    // Sistema
    public const string SistemaUsuarios = "sistema.usuarios";

    /// <summary>Un permiso, con la etiqueta que ve el administrador en la matriz.</summary>
    public sealed record Definicion(string Clave, string Etiqueta);

    /// <summary>Un modulo y los permisos que agrupa, para dibujar la matriz por secciones.</summary>
    public sealed record Modulo(string Nombre, IReadOnlyList<Definicion> Permisos);

    /// <summary>
    /// El catalogo completo, agrupado por modulo. Es la unica fuente de verdad: la pantalla
    /// de permisos, la siembra de plantillas y la validacion se leen de aqui.
    /// </summary>
    public static readonly IReadOnlyList<Modulo> Catalogo =
    [
        new Modulo("Ventas",
        [
            new Definicion(VentasFacturar, "Facturar (POS)"),
            new Definicion(VentasVer, "Ver facturas emitidas"),
            new Definicion(VentasAnular, "Anular facturas"),
            new Definicion(VentasNcf, "Administrar secuencias NCF"),
        ]),
        new Modulo("Inventario",
        [
            new Definicion(InventarioVer, "Ver inventario"),
            new Definicion(InventarioEditar, "Editar productos y categorias"),
            new Definicion(InventarioAjustar, "Ajustar existencia y mermas"),
        ]),
        new Modulo("Compras",
        [
            new Definicion(ComprasVer, "Ver compras y proveedores"),
            new Definicion(ComprasRecibir, "Recibir compra"),
            new Definicion(ComprasPagar, "Pagar a proveedor"),
            new Definicion(ComprasDevolver, "Devolver a proveedor"),
        ]),
        new Modulo("Clientes",
        [
            new Definicion(ClientesVer, "Ver clientes"),
            new Definicion(ClientesEditar, "Editar clientes"),
            new Definicion(ClientesCobrar, "Registrar cobro"),
        ]),
        new Modulo("Finanzas",
        [
            new Definicion(FinanzasVer, "Ver reportes"),
            new Definicion(FinanzasEgreso, "Registrar egreso"),
        ]),
        new Modulo("Sistema",
        [
            new Definicion(SistemaUsuarios, "Administrar usuarios y permisos"),
        ]),
    ];

    /// <summary>Todos los permisos del catalogo, en una sola lista.</summary>
    public static readonly IReadOnlyList<string> Todos =
        Catalogo.SelectMany(m => m.Permisos.Select(p => p.Clave)).ToArray();

    private static readonly HashSet<string> Conjunto = new(Todos, StringComparer.Ordinal);

    /// <summary>
    /// ¿La cadena es un permiso conocido del catalogo? Lo usa el policy provider para decidir
    /// si construye una politica de permiso o delega en el proveedor por defecto (asi el
    /// <c>[Authorize]</c> simple, que solo exige sesion, sigue funcionando igual).
    /// </summary>
    public static bool EsPermiso(string valor) => Conjunto.Contains(valor);

    /// <summary>
    /// Juego de permisos con que arranca cada rol (plantilla). Se aplica al crear el usuario
    /// y luego se afina por usuario. El <see cref="Roles.Administrador"/> tiene acceso total
    /// por regla —no por claims— pero se lista completo por si algun dia se quisiera sembrar.
    /// </summary>
    public static IReadOnlyList<string> PorDefectoDeRol(string rol) => rol switch
    {
        Roles.Administrador => Todos,
        Roles.Cajero => [VentasFacturar, VentasVer, InventarioVer, ClientesVer],
        Roles.Almacen => [InventarioVer, InventarioEditar, InventarioAjustar, ComprasVer, ComprasRecibir, ComprasDevolver],
        Roles.Supervisor => [VentasVer, InventarioVer, ComprasVer, ClientesVer, FinanzasVer],
        _ => [],
    };
}
