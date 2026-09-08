using MiniERP.Application.Clientes.Contracts;
using MiniERP.Application.Clientes.Dtos;
using MiniERP.Application.Clientes.Services;
using MiniERP.Application.Common;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Application.Compras.Services;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Finanzas.Services;
using MiniERP.Application.Inventario.Contracts;
using MiniERP.Application.Inventario.Dtos;
using MiniERP.Application.Inventario.Services;
using MiniERP.Application.Ventas.Contracts;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Application.Ventas.Services;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Finanzas;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Integracion;

/// <summary>
/// El almacen del comercio: una sola copia de los datos, compartida por los cinco modulos.
/// </summary>
/// <remarks>
/// Que sea uno solo es el punto. En produccion los cinco modulos comparten el mismo
/// DbContext, y por eso la compra que sube el costo promedio y la venta que lo congela
/// estan mirando el MISMO producto. Si cada modulo tuviera aqui su propia copia, la
/// prueba de punta a punta pasaria sin demostrar nada: cada uno cuadraria consigo mismo.
/// Los repositorios falsos de mas abajo entregan siempre la instancia viva, nunca una copia.
/// </remarks>
internal sealed class Almacen
{
    public Dictionary<int, Producto> Productos { get; } = [];
    public Dictionary<int, Cliente> Clientes { get; } = [];
    public Dictionary<int, Proveedor> Proveedores { get; } = [];
    public Dictionary<int, Categoria> Categorias { get; } = [];
    public Dictionary<int, UnidadMedida> Unidades { get; } = [];
    public Dictionary<int, CategoriaEgreso> CategoriasEgreso { get; } = [];
    public List<Compra> Compras { get; } = [];
    public List<Factura> Facturas { get; } = [];
    public List<MovimientoInventario> Movimientos { get; } = [];
    public List<Cobro> Cobros { get; } = [];
    public List<Pago> Pagos { get; } = [];
    public List<Egreso> Egresos { get; } = [];
    public List<SecuenciaNcf> Secuencias { get; } = [];

    private int ultimoId = 1000;

    /// <summary>Hace de IDENTITY: la fila no tiene Id hasta que se guarda.</summary>
    public int NuevoId() => ++ultimoId;

    /// <summary>El kardex de un producto, en el orden en que se escribio.</summary>
    public IReadOnlyList<MovimientoInventario> KardexDe(int productoId) =>
        [.. Movimientos.Where(m => m.ProductoId == productoId)];
}

/// <summary>
/// El sistema completo cableado sobre el almacen en memoria: los servicios de aplicacion
/// reales de los cinco modulos, con dobles solo en la frontera de datos.
/// </summary>
/// <remarks>
/// Lo que se prueba con esto es justamente lo que ninguna prueba de un modulo solo puede
/// ver: que la cadena completa cuadre. El costo promedio que calcula Compras es el que
/// Ventas congela en la linea, y es el que Finanzas usa para el margen. Si alguno de los
/// tres cambia su parte del trato, la aritmetica del reporte deja de dar y esta prueba
/// falla, aunque las pruebas de cada modulo por separado sigan verdes.
///
/// No hay base de datos: se ejercita hasta la frontera del repositorio, que es donde
/// termina la logica de negocio. Lo que queda fuera es el SQL y la transaccion real
/// —incluido el UPDATE atomico que asigna el NCF— y eso se dice en el informe en vez de
/// fingir que esta cubierto.
/// </remarks>
internal sealed class Minimarket
{
    public Almacen Datos { get; } = new();

    public IProductoService Productos { get; }
    public ICompraService Compras { get; }
    public IVentaService Ventas { get; }
    public ICobrosService Cobros { get; }
    public IEgresoService Egresos { get; }
    public IReporteIngresosService Ingresos { get; }
    public IReporteRentabilidadService Rentabilidad { get; }
    public IEstadoResultadosService EstadoResultados { get; }
    public IFlujoCajaService FlujoCaja { get; }

    public Minimarket()
    {
        var productos = new ProductoRepositorioFalso(Datos);
        var categorias = new CategoriaRepositorioFalso(Datos);
        var compras = new CompraRepositorioFalso(Datos);
        var proveedores = new ProveedorRepositorioFalso(Datos);
        var ventas = new VentaRepositorioFalso(Datos);
        var secuencias = new SecuenciaNcfRepositorioFalso(Datos);
        var clientes = new ClienteRepositorioFalso(Datos);
        var cobros = new CobrosRepositorioFalso(Datos);
        var egresos = new EgresoRepositorioFalso(Datos);
        var categoriasEgreso = new CategoriaEgresoRepositorioFalso(Datos);

        Productos = new ProductoService(productos, categorias);
        Compras = new CompraService(compras, proveedores);
        Ventas = new VentaService(ventas, secuencias, clientes);
        Cobros = new CobrosService(cobros, clientes);
        Egresos = new EgresoService(egresos, categoriasEgreso);

        Ingresos = new ReporteIngresosService(new IngresosRepositorioFalso(Datos));
        Rentabilidad = new ReporteRentabilidadService(new RentabilidadRepositorioFalso(Datos));
        EstadoResultados = new EstadoResultadosService(Ingresos, Rentabilidad, Egresos);
        FlujoCaja = new FlujoCajaService(Ingresos, Egresos, new CajaRepositorioFalso(Datos));
    }

    // ---------- Datos maestros ----------
    // Se siembran directamente porque son catalogo, no operacion: su alta ya tiene sus
    // propias pruebas y meterla aqui solo alargaria el escenario sin demostrar nada nuevo.

    public UnidadMedida SembrarUnidad(string codigo, bool permiteDecimales, int decimales = 0)
    {
        var unidad = new UnidadMedida
        {
            Id = Datos.NuevoId(),
            Codigo = codigo,
            Nombre = codigo,
            PermiteDecimales = permiteDecimales,
            CantidadDecimales = decimales
        };

        Datos.Unidades[unidad.Id] = unidad;
        return unidad;
    }

    public Categoria SembrarCategoria(string nombre)
    {
        var categoria = new Categoria { Id = Datos.NuevoId(), Nombre = nombre };
        Datos.Categorias[categoria.Id] = categoria;
        return categoria;
    }

    public Cliente SembrarCliente(string nombre, decimal limiteCredito)
    {
        var cliente = new Cliente
        {
            Id = Datos.NuevoId(),
            Codigo = "CLI-001",
            Nombre = nombre,
            LimiteCredito = limiteCredito,
            DiasCredito = 30
        };

        Datos.Clientes[cliente.Id] = cliente;
        return cliente;
    }

    public Proveedor SembrarProveedor(string nombre)
    {
        var proveedor = new Proveedor { Id = Datos.NuevoId(), Codigo = "PRV-001", Nombre = nombre };
        Datos.Proveedores[proveedor.Id] = proveedor;
        return proveedor;
    }

    public CategoriaEgreso SembrarCategoriaEgreso(string nombre)
    {
        var categoria = new CategoriaEgreso { Id = Datos.NuevoId(), Nombre = nombre };
        Datos.CategoriasEgreso[categoria.Id] = categoria;
        return categoria;
    }

    /// <summary>Un rango de NCF vigente, como el que la DGII autoriza al comercio.</summary>
    public SecuenciaNcf SembrarSecuencia(TipoComprobante tipo, string prefijo)
    {
        var secuencia = new SecuenciaNcf
        {
            Id = Datos.NuevoId(),
            TipoComprobante = tipo,
            Prefijo = prefijo,
            Desde = 1,
            Hasta = 5000,
            Actual = 0,
            FechaVencimiento = DateTime.UtcNow.Date.AddYears(1),
            Activa = true
        };

        Datos.Secuencias.Add(secuencia);
        return secuencia;
    }

    // ---------- Dobles de la frontera de datos ----------

    private sealed class ProductoRepositorioFalso(Almacen datos) : IProductoRepositorio
    {
        public Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Productos.GetValueOrDefault(id));

        public Task<UnidadMedida?> ObtenerUnidadAsync(int unidadMedidaId, CancellationToken ct = default) =>
            Task.FromResult(datos.Unidades.GetValueOrDefault(unidadMedidaId));

        public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
            Task.FromResult(datos.Productos.Values.Any(p => p.Codigo == codigo && p.Id != excluirId));

        public Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int? excluirId = null, CancellationToken ct = default) =>
            Task.FromResult(datos.Productos.Values.Any(p => p.CodigoBarras == codigoBarras && p.Id != excluirId));

        public void Agregar(Producto producto)
        {
            producto.Id = datos.NuevoId();
            datos.Productos[producto.Id] = producto;
        }

        public void AgregarMovimiento(MovimientoInventario movimiento) => datos.Movimientos.Add(movimiento);

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<IReadOnlyList<MovimientoDto>> ObtenerKardexAsync(
            int productoId, int cantidad = 50, CancellationToken ct = default)
        {
            IReadOnlyList<MovimientoDto> kardex =
            [
                .. datos.KardexDe(productoId)
                    .Select(m => new MovimientoDto(
                        m.Fecha, m.Tipo, m.Cantidad, m.EsEntrada,
                        m.ExistenciaAnterior, m.ExistenciaResultante, m.Motivo, m.UsuarioId))
            ];

            return Task.FromResult(kardex);
        }

        public Task<Producto?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default) =>
            Task.FromResult(datos.Productos.Values.FirstOrDefault(p => p.Codigo == codigo));

        public Task<PaginaDe<ProductoListaDto>> BuscarAsync(FiltroProductos filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductoListaDto>> ObtenerParaReabastecerAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UnidadMedidaDto>> ObtenerUnidadesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class CategoriaRepositorioFalso(Almacen datos) : ICategoriaRepositorio
    {
        public Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Categorias.GetValueOrDefault(id));

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<IReadOnlyList<Categoria>> ObtenerTodasAsync(
            bool soloActivas = false, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<int> ContarProductosAsync(int categoriaId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Agregar(Categoria categoria) => throw new NotSupportedException();

        public void Eliminar(Categoria categoria) => throw new NotSupportedException();
    }

    private sealed class CompraRepositorioFalso(Almacen datos) : ICompraRepositorio
    {
        public Task<Compra?> ObtenerConLineasAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Compras.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosDeAsync(
            Compra compra, CancellationToken ct = default)
        {
            // Instancias vivas: el dominio les recalcula el costo y les mueve la existencia.
            IReadOnlyDictionary<int, Producto> productos = compra.Lineas
                .Select(l => l.ProductoId)
                .Distinct()
                .Where(datos.Productos.ContainsKey)
                .ToDictionary(id => id, id => datos.Productos[id]);

            return Task.FromResult(productos);
        }

        public Task<string> SugerirNumeroAsync(CancellationToken ct = default) =>
            Task.FromResult($"COM-{datos.Compras.Count + 1:D4}");

        public void Agregar(Compra compra)
        {
            compra.Id = datos.NuevoId();
            datos.Compras.Add(compra);
        }

        public void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos) =>
            datos.Movimientos.AddRange(movimientos);

        public void EliminarLineas(IEnumerable<LineaCompra> lineas)
        {
            foreach (var compra in datos.Compras)
                foreach (var linea in lineas.ToList())
                    compra.Lineas.Remove(linea);
        }

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<PaginaDe<CompraListaDto>> BuscarAsync(FiltroCompras filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<CompraFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductoParaCompraDto>> BuscarProductosAsync(
            string texto, int maximo = 15, CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class ProveedorRepositorioFalso(Almacen datos) : IProveedorRepositorio
    {
        public Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Proveedores.GetValueOrDefault(id));

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<PaginaDe<ProveedorListaDto>> BuscarAsync(
            FiltroProveedores filtro, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteDocumentoAsync(string numeroDocumento, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> SugerirCodigoAsync(CancellationToken ct = default) => throw new NotSupportedException();

        public void Agregar(Proveedor proveedor) => throw new NotSupportedException();

        public Task<IReadOnlyList<CuentasPorPagarDto>> ObtenerCuentasPorPagarAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class VentaRepositorioFalso(Almacen datos) : IVentaRepositorio
    {
        public Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosAsync(
            IEnumerable<int> ids, CancellationToken ct = default)
        {
            IReadOnlyDictionary<int, Producto> productos = ids
                .Distinct()
                .Where(datos.Productos.ContainsKey)
                .ToDictionary(id => id, id => datos.Productos[id]);

            return Task.FromResult(productos);
        }

        public Task<string> SugerirNumeroAsync(CancellationToken ct = default) =>
            Task.FromResult($"FAC-{datos.Facturas.Count + 1:D4}");

        public void Agregar(Factura factura) => datos.Facturas.Add(factura);

        public void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos) =>
            datos.Movimientos.AddRange(movimientos);

        /// <summary>La fila no tiene Id hasta que se guarda: de eso depende el enlace del movimiento.</summary>
        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            foreach (var factura in datos.Facturas.Where(f => f.Id == 0))
                factura.Id = datos.NuevoId();

            return Task.FromResult(1);
        }

        public async Task<T> EnTransaccionAsync<T>(Func<Task<T>> operacion, CancellationToken ct = default) =>
            await operacion();

        public Task<Factura?> ObtenerConLineasAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Facturas.FirstOrDefault(f => f.Id == id));

        public Task<IReadOnlyList<ProductoParaVentaDto>> BuscarProductosAsync(
            string texto, int maximo = 15, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<ProductoParaVentaDto?> ObtenerPorCodigoBarrasAsync(
            string codigoBarras, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ClienteParaVentaDto>> BuscarClientesAsync(
            string texto, int maximo = 10, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PaginaDe<FacturaListaDto>> BuscarAsync(FiltroFacturas filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<FacturaDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    /// <summary>
    /// Emula la asignacion atomica del NCF. En produccion es un UPDATE ... OUTPUT en SQL
    /// Server, justamente para que dos cajas no puedan sacar el mismo numero; aqui el lock
    /// cumple el mismo papel para que el escenario avance, sin pretender demostrar la
    /// atomicidad real, que solo se puede comprobar contra la base.
    /// </summary>
    private sealed class SecuenciaNcfRepositorioFalso(Almacen datos) : ISecuenciaNcfRepositorio
    {
        private readonly Lock candado = new();

        public Task<string?> AsignarSiguienteAsync(TipoComprobante tipo, CancellationToken ct = default)
        {
            lock (candado)
            {
                var hoy = DateTime.UtcNow;

                var secuencia = datos.Secuencias
                    .Where(s => s.TipoComprobante == tipo && s.PuedeEmitir(hoy))
                    .OrderBy(s => s.Id)
                    .FirstOrDefault();

                if (secuencia is null)
                    return Task.FromResult<string?>(null);

                secuencia.Actual++;
                return Task.FromResult<string?>(secuencia.Formatear(secuencia.Actual));
            }
        }

        public Task<SecuenciaNcf?> ObtenerPorTipoAsync(TipoComprobante tipo, CancellationToken ct = default) =>
            Task.FromResult(datos.Secuencias.FirstOrDefault(s => s.TipoComprobante == tipo));

        public Task<IReadOnlyList<SecuenciaNcf>> ObtenerTodasAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SecuenciaNcf>>([.. datos.Secuencias]);

        public Task<SecuenciaNcf?> ObtenerAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Secuencias.FirstOrDefault(s => s.Id == id));

        public void Agregar(SecuenciaNcf secuencia) => datos.Secuencias.Add(secuencia);

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);
    }

    private sealed class ClienteRepositorioFalso(Almacen datos) : IClienteRepositorio
    {
        public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Clientes.GetValueOrDefault(id));

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<PaginaDe<ClienteListaDto>> BuscarAsync(FiltroClientes filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<ResumenCarteraDto> ObtenerResumenAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteDocumentoAsync(string numeroDocumento, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> SugerirCodigoAsync(CancellationToken ct = default) => throw new NotSupportedException();

        public void Agregar(Cliente cliente) => throw new NotSupportedException();

        public Task<IReadOnlyList<TransaccionEstadoCuentaDto>> ObtenerEstadoCuentaAsync(
            int clienteId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<CuentasPorCobrarDto>> ObtenerCuentasPorCobrarAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class CobrosRepositorioFalso(Almacen datos) : ICobrosRepositorio
    {
        public void Agregar(Cobro cobro)
        {
            cobro.Id = datos.NuevoId();
            datos.Cobros.Add(cobro);
        }

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<Cobro?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Cobros.FirstOrDefault(c => c.Id == id));

        public Task<IReadOnlyList<Cobro>> ObtenerPorClienteAsync(int clienteId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Cobro>>([.. datos.Cobros.Where(c => c.ClienteId == clienteId)]);
    }

    private sealed class EgresoRepositorioFalso(Almacen datos) : IEgresoRepositorio
    {
        public Task<Egreso?> ObtenerAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.Egresos.FirstOrDefault(e => e.Id == id));

        public Task<decimal> ObtenerTotalAsync(FiltroEgresos filtro, CancellationToken ct = default) =>
            Task.FromResult(datos.Egresos
                .Where(e => (filtro.Desde is null || e.Fecha >= filtro.Desde)
                         && (filtro.Hasta is null || e.Fecha <= filtro.Hasta)
                         && (filtro.CategoriaEgresoId is null || e.CategoriaEgresoId == filtro.CategoriaEgresoId))
                .Sum(e => e.Monto));

        public void Agregar(Egreso egreso)
        {
            egreso.Id = datos.NuevoId();
            datos.Egresos.Add(egreso);
        }

        public void Eliminar(Egreso egreso) => datos.Egresos.Remove(egreso);

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<PaginaDe<EgresoListaDto>> BuscarAsync(FiltroEgresos filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<EgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class CategoriaEgresoRepositorioFalso(Almacen datos) : ICategoriaEgresoRepositorio
    {
        public Task<CategoriaEgreso?> ObtenerAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(datos.CategoriasEgreso.GetValueOrDefault(id));

        public Task<int> GuardarAsync(CancellationToken ct = default) => Task.FromResult(1);

        public Task<PaginaDe<CategoriaEgresoListaDto>> BuscarAsync(
            FiltroCategoriasEgreso filtro, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Agregar(CategoriaEgreso categoria) => throw new NotSupportedException();
    }

    // ---------- Lectura para los reportes de finanzas ----------

    private sealed class IngresosRepositorioFalso(Almacen datos) : IReporteIngresosRepositorio
    {
        public Task<IReadOnlyList<Factura>> ObtenerFacturasDelPeriodoAsync(
            DateTime desde, DateTime hasta, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Factura>>(
                [.. datos.Facturas.Where(f => f.Fecha >= desde && f.Fecha <= hasta)]);
    }

    /// <summary>
    /// Proyecta los renglones vendidos como lo hace el repositorio real: el costo sale de
    /// la linea de factura (congelado al vender), nunca del producto.
    /// </summary>
    private sealed class RentabilidadRepositorioFalso(Almacen datos) : IReporteRentabilidadRepositorio
    {
        public Task<IReadOnlyList<RenglonVendido>> ObtenerRenglonesDelPeriodoAsync(
            DateTime desde, DateTime hasta, CancellationToken ct = default)
        {
            IReadOnlyList<RenglonVendido> renglones =
            [
                .. from factura in datos.Facturas
                   where factura.Fecha >= desde && factura.Fecha <= hasta
                   from linea in factura.Lineas
                   let producto = datos.Productos.GetValueOrDefault(linea.ProductoId)
                   let categoria = producto is null
                       ? null
                       : datos.Categorias.GetValueOrDefault(producto.CategoriaId)
                   select new RenglonVendido(
                       factura.EstaAnulada,
                       linea.ProductoId, linea.Descripcion,
                       producto?.CategoriaId ?? 0, categoria?.Nombre ?? string.Empty,
                       linea.Subtotal, linea.CostoTotal)
            ];

            return Task.FromResult(renglones);
        }
    }

    private sealed class CajaRepositorioFalso(Almacen datos) : IFlujoCajaRepositorio
    {
        public Task<decimal> SumarCobrosAsync(DateTime desde, DateTime hasta, CancellationToken ct = default) =>
            Task.FromResult(datos.Cobros.Where(c => c.Fecha >= desde && c.Fecha <= hasta).Sum(c => c.Monto));

        public Task<decimal> SumarPagosAsync(DateTime desde, DateTime hasta, CancellationToken ct = default) =>
            Task.FromResult(datos.Pagos.Where(p => p.Fecha >= desde && p.Fecha <= hasta).Sum(p => p.Monto));
    }
}
