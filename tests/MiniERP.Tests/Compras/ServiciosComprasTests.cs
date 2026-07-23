using MiniERP.Application.Common;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Application.Compras.Services;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Tests.Compras;

public class ServiciosComprasTests
{
    [Fact]
    public async Task Crear_proveedor_normaliza_y_comienza_sin_balance()
    {
        var repo = new ProveedorRepoFalso();
        var servicio = new ProveedorService(repo);

        var resultado = await servicio.CrearAsync(new ProveedorFormDto
        {
            Codigo = " prv-900 ",
            Nombre = " Proveedor prueba ",
            TipoDocumento = TipoDocumento.Rnc,
            NumeroDocumento = "131-12345-6",
            BalanceActual = 999,
            DiasCredito = 30
        }, "dionis@test");

        Assert.False(resultado.Fallo);
        Assert.Equal("PRV-900", repo.Proveedor!.Codigo);
        Assert.Equal("131123456", repo.Proveedor.NumeroDocumento);
        Assert.Equal(0, repo.Proveedor.BalanceActual);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task Actualizar_proveedor_no_modifica_balance()
    {
        var repo = new ProveedorRepoFalso
        {
            Proveedor = new Proveedor { Id = 2, Codigo = "PRV-2", Nombre = "A", BalanceActual = 700 }
        };
        var servicio = new ProveedorService(repo);

        var resultado = await servicio.ActualizarAsync(new ProveedorFormDto
        {
            Id = 2,
            Codigo = "PRV-2",
            Nombre = "B",
            TipoDocumento = TipoDocumento.Ninguno,
            BalanceActual = 0
        }, "dionis@test");

        Assert.False(resultado.Fallo);
        Assert.Equal("B", repo.Proveedor.Nombre);
        Assert.Equal(700, repo.Proveedor.BalanceActual);
    }

    [Fact]
    public async Task Recibir_compra_credito_actualiza_inventario_balance_y_guarda_una_vez()
    {
        var producto = new Producto
        {
            Id = 3,
            Codigo = "PRO-3",
            Existencia = 5,
            Costo = 80,
            ManejaInventario = true
        };
        var proveedor = new Proveedor { Id = 4, BalanceActual = 100 };
        var compra = new Compra
        {
            Id = 5,
            Numero = "COM-5",
            ProveedorId = proveedor.Id,
            Condicion = CondicionPago.Credito
        };
        compra.Lineas.Add(new LineaCompra
        {
            ProductoId = producto.Id,
            Descripcion = "Producto",
            Cantidad = 2,
            CostoUnitario = 100,
            TasaItbis = 0.18m
        });
        compra.Recalcular();

        var compras = new CompraRepoFalso(compra, producto);
        var proveedores = new ProveedorRepoFalso { Proveedor = proveedor };
        var servicio = new CompraService(compras, proveedores);

        var resultado = await servicio.RecibirAsync(compra.Id, "dionis@test");

        Assert.False(resultado.Fallo);
        Assert.Equal(7, producto.Existencia);
        Assert.Equal(336, proveedor.BalanceActual);
        Assert.Equal(EstadoCompra.Recibida, compra.Estado);
        Assert.Single(compras.Movimientos);
        Assert.Equal(1, compras.Guardados);
    }

    [Fact]
    public async Task Registrar_pago_deja_rastro_y_rechaza_exceso()
    {
        var proveedores = new ProveedorRepoFalso
        {
            Proveedor = new Proveedor { Id = 6, BalanceActual = 400 }
        };
        var pagos = new PagoRepoFalso();
        var servicio = new PagosService(pagos, proveedores);

        var correcto = await servicio.RegistrarPagoAsync(new PagoFormDto
        {
            ProveedorId = 6,
            Monto = 150,
            Observacion = " transferencia "
        }, "dionis@test");

        Assert.False(correcto.Fallo);
        Assert.Equal(250, proveedores.Proveedor.BalanceActual);
        Assert.Equal(400, pagos.Pago!.BalanceAnterior);
        Assert.Equal(250, pagos.Pago.BalanceResultante);
        Assert.Equal("transferencia", pagos.Pago.Observacion);
        Assert.Equal(1, pagos.Guardados);

        var excesivo = await servicio.RegistrarPagoAsync(
            new PagoFormDto { ProveedorId = 6, Monto = 251 }, "dionis@test");

        Assert.True(excesivo.Fallo);
        Assert.Equal(250, proveedores.Proveedor.BalanceActual);
        Assert.Equal(1, pagos.Guardados);
    }

    private sealed class ProveedorRepoFalso : IProveedorRepositorio
    {
        public Proveedor? Proveedor { get; set; }
        public bool CodigoExiste { get; set; }
        public int Guardados { get; private set; }

        public Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Proveedor?.Id == id ? Proveedor : null);
        public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
            Task.FromResult(CodigoExiste);
        public Task<bool> ExisteDocumentoAsync(string numeroDocumento, int? excluirId = null, CancellationToken ct = default) =>
            Task.FromResult(false);
        public Task<string> SugerirCodigoAsync(CancellationToken ct = default) => Task.FromResult("PRV-0001");
        public void Agregar(Proveedor proveedor)
        {
            Proveedor = proveedor;
            Proveedor.Id = 1;
        }
        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }
        public Task<PaginaDe<ProveedorListaDto>> BuscarAsync(FiltroProveedores filtro, CancellationToken ct = default) =>
            Task.FromResult(new PaginaDe<ProveedorListaDto>([], 0, 1, 25));
        public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<OpcionDto>>([]);
        public Task<IReadOnlyList<CuentasPorPagarDto>> ObtenerCuentasPorPagarAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CuentasPorPagarDto>>([]);
    }

    private sealed class CompraRepoFalso(Compra compra, Producto producto) : ICompraRepositorio
    {
        public List<MovimientoInventario> Movimientos { get; } = [];
        public int Guardados { get; private set; }
        public Task<Compra?> ObtenerConLineasAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<Compra?>(id == compra.Id ? compra : null);
        public Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosDeAsync(
            Compra documento, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<int, Producto>>(
                new Dictionary<int, Producto> { [producto.Id] = producto });
        public void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos) =>
            Movimientos.AddRange(movimientos);
        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }
        public Task<PaginaDe<CompraListaDto>> BuscarAsync(FiltroCompras filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<CompraFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<string> SugerirNumeroAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
        public Task<IReadOnlyList<ProductoParaCompraDto>> BuscarProductosAsync(
            string texto, int maximo = 15, CancellationToken ct = default) =>
            throw new NotSupportedException();
        public void Agregar(Compra documento) => throw new NotSupportedException();
        public void EliminarLineas(IEnumerable<LineaCompra> lineas) => throw new NotSupportedException();
    }

    private sealed class PagoRepoFalso : IPagosRepositorio
    {
        public Pago? Pago { get; private set; }
        public int Guardados { get; private set; }
        public Task<Pago?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Pago?.Id == id ? Pago : null);
        public Task<IReadOnlyList<Pago>> ObtenerPorProveedorAsync(
            int proveedorId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Pago>>(Pago is null ? [] : [Pago]);
        public void Agregar(Pago pago)
        {
            Pago = pago;
            Pago.Id = 1;
        }
        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }
    }
}
