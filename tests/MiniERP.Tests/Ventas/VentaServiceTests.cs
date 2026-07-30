using MiniERP.Application.Clientes.Contracts;
using MiniERP.Application.Clientes.Dtos;
using MiniERP.Application.Common;
using MiniERP.Application.Ventas.Contracts;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Application.Ventas.Services;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Ventas;

/// <summary>
/// La emision como caso de uso completo: lo que queda escrito despues de vender.
/// </summary>
/// <remarks>
/// Existe por un defecto que las pruebas de dominio no podian ver. Emitir construye los
/// movimientos de salida sobre una factura que todavia no se ha guardado, cuando su Id
/// vale 0; la prueba de dominio le pasaba una factura con Id ya puesto, asi que el enlace
/// parecia correcto y en la base todos los movimientos de venta salian con ReferenciaId
/// en cero. Sin ese enlace, la pregunta "que factura bajo el arroz el martes" solo se
/// responde parseando el texto del motivo.
///
/// El repositorio falso simula lo unico que hace falta para verlo: que el Id de la factura
/// no existe hasta que se guarda.
/// </remarks>
public class VentaServiceTests
{
    private const int IdAsignadoAlGuardar = 41;

    private static VentaFormDto Carrito(decimal cantidad = 3m) => new()
    {
        Condicion = CondicionPago.Contado,
        MontoRecibido = 1000m,
        Lineas =
        [
            new LineaVentaDto
            {
                ProductoId = 1,
                Codigo = "ARR-LB",
                Descripcion = "Arroz selecto",
                UnidadMedida = "LB",
                PermiteDecimales = true,
                Cantidad = cantidad,
                PrecioUnitario = 38m,
                TasaItbis = 0m
            }
        ]
    };

    private static Producto Producto(decimal existencia = 29.5m) => new()
    {
        Id = 1,
        Codigo = "ARR-LB",
        Descripcion = "Arroz selecto",
        Existencia = existencia,
        Costo = 30.8729m,
        PrecioVenta = 38m,
        ManejaInventario = true
    };

    private static (VentaService Servicio, VentaRepositorioFalso Repo) Armar(Producto? producto = null)
    {
        var repo = new VentaRepositorioFalso(producto ?? Producto());
        return (new VentaService(repo, new SecuenciaNcfRepositorioFalso(), new ClienteRepositorioFalso()), repo);
    }

    [Fact]
    public async Task Cada_movimiento_de_la_venta_apunta_a_la_factura_que_lo_origino()
    {
        var (servicio, repo) = Armar();

        var resultado = await servicio.EmitirAsync(Carrito(), "cajero");

        Assert.True(resultado.Exito);

        var movimiento = Assert.Single(repo.Movimientos);
        Assert.Equal("FACTURA", movimiento.ReferenciaTipo);
        Assert.Equal(IdAsignadoAlGuardar, movimiento.ReferenciaId);
        Assert.NotEqual(0, movimiento.ReferenciaId);
    }

    [Fact]
    public async Task El_movimiento_se_guarda_despues_de_la_factura_para_poder_enlazarlo()
    {
        var (servicio, repo) = Armar();

        await servicio.EmitirAsync(Carrito(), "cajero");

        // Si los movimientos se agregaran antes del primer guardado, entrarian con el Id
        // todavia en cero: el orden es parte del arreglo, no un detalle.
        Assert.Equal(1, repo.GuardadoEnQueSeAgregaronLosMovimientos);
        Assert.Equal(2, repo.Guardados);
    }

    [Fact]
    public async Task La_venta_sigue_descontando_el_inventario_y_congelando_el_costo()
    {
        var producto = Producto(existencia: 29.5m);
        var (servicio, repo) = Armar(producto);

        var resultado = await servicio.EmitirAsync(Carrito(cantidad: 3m), "cajero");

        Assert.True(resultado.Exito);
        Assert.Equal(26.5m, producto.Existencia);

        var linea = Assert.Single(repo.Guardada!.Lineas);

        // El costo unitario se congela con toda su precision —es un promedio ponderado—,
        // pero el total de la factura es dinero y se redondea: 3 x 30.8729 = 92.6187 -> 92.62.
        Assert.Equal(30.8729m, linea.CostoUnitario);
        Assert.Equal(92.62m, repo.Guardada.CostoTotal);
    }

    [Fact]
    public async Task Sin_existencia_no_se_guarda_nada_y_el_comprobante_no_se_consume()
    {
        var (servicio, repo) = Armar(Producto(existencia: 1m));

        var resultado = await servicio.EmitirAsync(Carrito(cantidad: 3m), "cajero");

        Assert.True(resultado.Fallo);
        Assert.Empty(repo.Movimientos);
        Assert.Equal(0, repo.Guardados);
    }

    /// <summary>
    /// Lo unico que este falso simula de verdad es el Id: no existe hasta que se guarda,
    /// que es exactamente donde estaba el defecto.
    /// </summary>
    private sealed class VentaRepositorioFalso(Producto producto) : IVentaRepositorio
    {
        public Factura? Guardada { get; private set; }

        public List<MovimientoInventario> Movimientos { get; } = [];

        public int Guardados { get; private set; }

        public int GuardadoEnQueSeAgregaronLosMovimientos { get; private set; }

        public Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosAsync(
            IEnumerable<int> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<int, Producto>>(
                new Dictionary<int, Producto> { [producto.Id] = producto });

        public Task<string> SugerirNumeroAsync(CancellationToken ct = default) =>
            Task.FromResult("F-000041");

        public void Agregar(Factura factura) => Guardada = factura;

        public void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos)
        {
            GuardadoEnQueSeAgregaronLosMovimientos = Guardados;
            Movimientos.AddRange(movimientos);
        }

        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;

            // Es lo que hace la base al insertar la fila, y el motivo del defecto:
            // antes de esto la factura no tiene Id que un movimiento pueda citar.
            if (Guardada is { Id: 0 })
                Guardada.Id = IdAsignadoAlGuardar;

            return Task.FromResult(1);
        }

        public async Task<T> EnTransaccionAsync<T>(Func<Task<T>> operacion, CancellationToken ct = default) =>
            await operacion();

        public Task<IReadOnlyList<ProductoParaVentaDto>> BuscarProductosAsync(
            string texto, int maximo = 15, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<ProductoParaVentaDto?> ObtenerPorCodigoBarrasAsync(
            string codigoBarras, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ClienteParaVentaDto>> BuscarClientesAsync(
            string texto, int maximo = 10, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PaginaDe<FacturaListaDto>> BuscarAsync(
            FiltroFacturas filtro, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<FacturaDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Factura?> ObtenerConLineasAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class SecuenciaNcfRepositorioFalso : ISecuenciaNcfRepositorio
    {
        public Task<string?> AsignarSiguienteAsync(TipoComprobante tipo, CancellationToken ct = default) =>
            Task.FromResult<string?>("B0200000001");

        public Task<SecuenciaNcf?> ObtenerPorTipoAsync(TipoComprobante tipo, CancellationToken ct = default) =>
            Task.FromResult<SecuenciaNcf?>(null);

        public Task<IReadOnlyList<SecuenciaNcf>> ObtenerTodasAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<SecuenciaNcf?> ObtenerAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Agregar(SecuenciaNcf secuencia) => throw new NotSupportedException();

        public Task<int> GuardarAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }

    private sealed class ClienteRepositorioFalso : IClienteRepositorio
    {
        public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<Cliente?>(null);

        public Task<PaginaDe<ClienteListaDto>> BuscarAsync(
            FiltroClientes filtro, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<ResumenCarteraDto> ObtenerResumenAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteDocumentoAsync(
            string numeroDocumento, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> SugerirCodigoAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Agregar(Cliente cliente) => throw new NotSupportedException();

        public Task<int> GuardarAsync(CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<TransaccionEstadoCuentaDto>> ObtenerEstadoCuentaAsync(
            int clienteId, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<CuentasPorCobrarDto>> ObtenerCuentasPorCobrarAsync(
            CancellationToken ct = default) => throw new NotSupportedException();
    }
}
