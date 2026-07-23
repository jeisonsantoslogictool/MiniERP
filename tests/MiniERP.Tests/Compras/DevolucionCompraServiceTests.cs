using MiniERP.Application.Common;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Application.Compras.Services;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;

namespace MiniERP.Tests.Compras;

public class DevolucionCompraServiceTests
{
    [Fact]
    public async Task Confirmar_actualiza_inventario_balance_y_guarda_una_sola_vez()
    {
        var producto = new Producto
        {
            Id = 10,
            Codigo = "PRO-001",
            Existencia = 8,
            Costo = 40,
            ManejaInventario = true
        };
        var proveedor = new Proveedor { Id = 20, BalanceActual = 500 };
        var devolucion = new DevolucionCompra
        {
            Id = 30,
            Numero = "DEV-0030",
            CompraId = 40,
            ProveedorId = proveedor.Id,
            Proveedor = proveedor,
            Motivo = "Producto dañado"
        };
        devolucion.Lineas.Add(new LineaDevolucionCompra
        {
            LineaCompraId = 50,
            ProductoId = producto.Id,
            Descripcion = "Producto",
            Cantidad = 2,
            CostoUnitario = 100
        });
        devolucion.Recalcular();

        var repositorio = new RepositorioFalso(devolucion, producto, comprado: 5);
        var servicio = new DevolucionCompraService(repositorio);

        var resultado = await servicio.ConfirmarAsync(devolucion.Id, "dionis@test");

        Assert.False(resultado.Fallo);
        Assert.Equal(6, producto.Existencia);
        Assert.Equal(300, proveedor.BalanceActual);
        Assert.Equal(500, devolucion.BalanceAnterior);
        Assert.Equal(300, devolucion.BalanceResultante);
        Assert.Equal(EstadoDevolucion.Confirmada, devolucion.Estado);
        Assert.Single(repositorio.Movimientos);
        Assert.Equal(1, repositorio.VecesGuardado);
    }

    private sealed class RepositorioFalso(
        DevolucionCompra devolucion,
        Producto producto,
        decimal comprado) : IDevolucionCompraRepositorio
    {
        public List<MovimientoInventario> Movimientos { get; } = [];
        public int VecesGuardado { get; private set; }

        public Task<DevolucionCompra?> ObtenerConLineasAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<DevolucionCompra?>(id == devolucion.Id ? devolucion : null);

        public Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosDeAsync(
            DevolucionCompra documento, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<int, Producto>>(
                new Dictionary<int, Producto> { [producto.Id] = producto });

        public Task<IReadOnlyDictionary<int, decimal>> ObtenerCompradoPorLineaCompraAsync(
            int compraId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<int, decimal>>(
                new Dictionary<int, decimal> { [devolucion.Lineas.Single().LineaCompraId] = comprado });

        public Task<IReadOnlyDictionary<int, decimal>> ObtenerDevueltoPorLineaCompraAsync(
            int compraId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<int, decimal>>(
                new Dictionary<int, decimal>());

        public void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos) =>
            Movimientos.AddRange(movimientos);

        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            VecesGuardado++;
            return Task.FromResult(1);
        }

        public Task<PaginaDe<DevolucionCompraListaDto>> BuscarAsync(
            FiltroDevoluciones filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<DevolucionCompraFormDto?> ObtenerParaVerAsync(
            int id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<DevolucionCompraFormDto?> ObtenerCompraParaDevolverAsync(
            int compraId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> SugerirNumeroAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Agregar(DevolucionCompra documento) =>
            throw new NotSupportedException();

        public void EliminarLineas(IEnumerable<LineaDevolucionCompra> lineas) =>
            throw new NotSupportedException();
    }
}
