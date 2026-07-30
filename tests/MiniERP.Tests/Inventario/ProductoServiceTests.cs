using MiniERP.Application.Common;
using MiniERP.Application.Inventario.Contracts;
using MiniERP.Application.Inventario.Dtos;
using MiniERP.Application.Inventario.Services;
using MiniERP.Domain.Inventario;

namespace MiniERP.Tests.Inventario;

/// <summary>
/// Que guarda y que no guarda el caso de uso de productos.
/// </summary>
/// <remarks>
/// El costo de un producto es el promedio ponderado que recalcula
/// <c>Compra.CalcularCostoPromedio</c> cada vez que se recibe mercancia, y el margen de las
/// ventas siguientes se mide contra el. Editar un producto para corregirle la descripcion no
/// puede pisarlo: eso moveria la base del margen sin ningun asiento que lo explique. Es la
/// misma razon por la que la existencia solo cambia por movimiento, y estas pruebas fijan
/// las dos reglas para que no se pierdan en una edicion futura.
/// </remarks>
public class ProductoServiceTests
{
    private const decimal CostoPromedio = 30.8729m;

    private static Producto Existente() => new()
    {
        Id = 7,
        Codigo = "ARR-LB",
        Descripcion = "Arroz selecto",
        CategoriaId = 1,
        UnidadMedidaId = 1,
        Costo = CostoPromedio,
        PrecioVenta = 38m,
        Existencia = 29.5m,
        ExistenciaMinima = 5m
    };

    /// <summary>El formulario tal como vuelve de la pantalla, con el costo ya manipulado.</summary>
    private static ProductoFormDto Formulario(Producto producto, decimal costoTecleado) => new()
    {
        Id = producto.Id,
        Codigo = producto.Codigo,
        Descripcion = producto.Descripcion,
        CategoriaId = producto.CategoriaId,
        UnidadMedidaId = producto.UnidadMedidaId,
        Costo = costoTecleado,
        PrecioVenta = producto.PrecioVenta,
        TasaItbis = producto.TasaItbis,
        ExistenciaMinima = producto.ExistenciaMinima,
        ManejaInventario = producto.ManejaInventario,
        Activo = producto.Activo
    };

    private static ProductoService Servicio(Producto? existente, out ProductoRepositorioFalso repo)
    {
        repo = new ProductoRepositorioFalso(existente);
        return new ProductoService(repo, new CategoriaRepositorioFalso());
    }

    [Fact]
    public async Task Editar_un_producto_no_pisa_el_costo_promedio()
    {
        var producto = Existente();
        var servicio = Servicio(producto, out _);

        var resultado = await servicio.ActualizarAsync(Formulario(producto, costoTecleado: 1m), "jeison");

        Assert.True(resultado.Exito);
        Assert.Equal(CostoPromedio, producto.Costo);
    }

    [Fact]
    public async Task Editar_un_producto_no_toca_la_existencia()
    {
        var producto = Existente();
        var servicio = Servicio(producto, out _);
        var form = Formulario(producto, costoTecleado: CostoPromedio);
        form.ExistenciaInicial = 999m;

        await servicio.ActualizarAsync(form, "jeison");

        Assert.Equal(29.5m, producto.Existencia);
    }

    [Fact]
    public async Task Editar_un_producto_si_guarda_lo_que_el_usuario_corrige()
    {
        var producto = Existente();
        var servicio = Servicio(producto, out var repo);
        var form = Formulario(producto, costoTecleado: 1m);
        form.Descripcion = "Arroz selecto premium";
        form.PrecioVenta = 42m;
        form.ExistenciaMinima = 8m;

        var resultado = await servicio.ActualizarAsync(form, "jeison");

        Assert.True(resultado.Exito);
        Assert.Equal("Arroz selecto premium", producto.Descripcion);
        Assert.Equal(42m, producto.PrecioVenta);
        Assert.Equal(8m, producto.ExistenciaMinima);
        Assert.Equal("jeison", producto.ModificadoPor);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task Al_crear_si_se_toma_el_costo_del_formulario_porque_es_el_costo_inicial()
    {
        var servicio = Servicio(existente: null, out var repo);
        var form = new ProductoFormDto
        {
            Codigo = "PAN-UN",
            Descripcion = "Pan de agua",
            CategoriaId = 1,
            UnidadMedidaId = 1,
            Costo = 18.50m,
            PrecioVenta = 25m
        };

        var resultado = await servicio.CrearAsync(form, "jeison");

        Assert.True(resultado.Exito);
        Assert.Equal(18.50m, repo.Agregado!.Costo);
    }

    /// <summary>
    /// La existencia inicial entra como movimiento de apertura valorado al costo inicial:
    /// asi el costo del primer dia tambien tiene un asiento detras.
    /// </summary>
    [Fact]
    public async Task La_existencia_inicial_entra_como_movimiento_valorado_al_costo_inicial()
    {
        var servicio = Servicio(existente: null, out var repo);
        var form = new ProductoFormDto
        {
            Codigo = "PAN-UN",
            Descripcion = "Pan de agua",
            CategoriaId = 1,
            UnidadMedidaId = 1,
            Costo = 18.50m,
            PrecioVenta = 25m,
            ExistenciaInicial = 12m
        };

        await servicio.CrearAsync(form, "jeison");

        var apertura = Assert.Single(repo.Movimientos);
        Assert.Equal(TipoMovimiento.Entrada, apertura.Tipo);
        Assert.Equal(12m, apertura.Cantidad);
        Assert.Equal(18.50m, apertura.CostoUnitario);
        Assert.Equal(12m, repo.Agregado!.Existencia);
    }

    /// <summary>
    /// Solo lo que el caso de uso de productos consulta de verdad. Lo demas lanza a proposito:
    /// si una prueba futura lo necesita, es que el servicio cambio y hay que mirarlo.
    /// </summary>
    private sealed class ProductoRepositorioFalso(Producto? existente) : IProductoRepositorio
    {
        public Producto? Agregado { get; private set; }

        public List<MovimientoInventario> Movimientos { get; } = [];

        public int Guardados { get; private set; }

        public Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(existente?.Id == id ? existente : Agregado?.Id == id ? Agregado : null);

        public Task<UnidadMedida?> ObtenerUnidadAsync(int unidadMedidaId, CancellationToken ct = default) =>
            Task.FromResult<UnidadMedida?>(new UnidadMedida
            {
                Id = unidadMedidaId,
                Codigo = "LB",
                Nombre = "Libra",
                PermiteDecimales = true,
                CantidadDecimales = 3
            });

        public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
            Task.FromResult(false);

        public Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int? excluirId = null, CancellationToken ct = default) =>
            Task.FromResult(false);

        public void Agregar(Producto producto)
        {
            producto.Id = 99;
            Agregado = producto;
        }

        public void AgregarMovimiento(MovimientoInventario movimiento) => Movimientos.Add(movimiento);

        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }

        public Task<Producto?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<PaginaDe<ProductoListaDto>> BuscarAsync(FiltroProductos filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<ProductoListaDto>> ObtenerParaReabastecerAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<UnidadMedidaDto>> ObtenerUnidadesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<MovimientoDto>> ObtenerKardexAsync(
            int productoId, int cantidad = 50, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class CategoriaRepositorioFalso : ICategoriaRepositorio
    {
        public Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult<Categoria?>(new Categoria { Id = id, Nombre = "Viveres" });

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

        public Task<int> GuardarAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}
