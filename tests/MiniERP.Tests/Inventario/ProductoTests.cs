using MiniERP.Domain.Inventario;

namespace MiniERP.Tests.Inventario;

public class ProductoTests
{
    private static Producto Producto(decimal existencia = 10, decimal minima = 3) => new()
    {
        Id = 1,
        Codigo = "ARR-001",
        Descripcion = "Arroz selecto",
        Existencia = existencia,
        ExistenciaMinima = minima,
        Costo = 30m,
        PrecioVenta = 40m
    };

    private static MovimientoInventario Movimiento(TipoMovimiento tipo, decimal cantidad) =>
        new() { Tipo = tipo, Cantidad = cantidad, CostoUnitario = 30m };

    [Fact]
    public void Entrada_suma_a_la_existencia()
    {
        var producto = Producto(existencia: 10);

        producto.AplicarMovimiento(Movimiento(TipoMovimiento.Entrada, 5));

        Assert.Equal(15, producto.Existencia);
    }

    [Fact]
    public void Salida_resta_de_la_existencia()
    {
        var producto = Producto(existencia: 10);

        producto.AplicarMovimiento(Movimiento(TipoMovimiento.Salida, 3));

        Assert.Equal(7, producto.Existencia);
    }

    [Fact]
    public void Merma_resta_igual_que_una_salida()
    {
        var producto = Producto(existencia: 10);

        producto.AplicarMovimiento(Movimiento(TipoMovimiento.Merma, 2));

        Assert.Equal(8, producto.Existencia);
    }

    [Fact]
    public void Movimiento_guarda_el_saldo_antes_y_despues_para_poder_auditarlo()
    {
        var producto = Producto(existencia: 10);
        var movimiento = Movimiento(TipoMovimiento.Salida, 4);

        producto.AplicarMovimiento(movimiento);

        Assert.Equal(10, movimiento.ExistenciaAnterior);
        Assert.Equal(6, movimiento.ExistenciaResultante);
        Assert.Equal(producto.Id, movimiento.ProductoId);
    }

    [Fact]
    public void Existencia_soporta_decimales_porque_se_vende_por_peso()
    {
        var producto = Producto(existencia: 10.5m);

        producto.AplicarMovimiento(Movimiento(TipoMovimiento.Salida, 2.375m));

        Assert.Equal(8.125m, producto.Existencia);
    }

    [Fact]
    public void No_se_puede_vender_mas_de_lo_que_hay()
    {
        var producto = Producto(existencia: 5);

        var ex = Assert.Throws<InvalidOperationException>(
            () => producto.AplicarMovimiento(Movimiento(TipoMovimiento.Salida, 6)));

        Assert.Contains("ARR-001", ex.Message);
        Assert.Equal(5, producto.Existencia);
    }

    [Fact]
    public void Un_servicio_no_mueve_inventario()
    {
        var producto = Producto(existencia: 0);
        producto.ManejaInventario = false;

        producto.AplicarMovimiento(Movimiento(TipoMovimiento.Salida, 100));

        Assert.Equal(0, producto.Existencia);
    }

    [Theory]
    [InlineData(10, 3, false)]
    [InlineData(3, 3, true)]   // el umbral cuenta como alcanzado
    [InlineData(1, 3, true)]
    [InlineData(0, 3, true)]
    public void La_alerta_de_reabastecimiento_salta_al_tocar_el_minimo(
        decimal existencia, decimal minima, bool esperado) =>
        Assert.Equal(esperado, Producto(existencia, minima).RequiereReabastecimiento);

    [Fact]
    public void Un_producto_inactivo_no_genera_alerta()
    {
        var producto = Producto(existencia: 0);
        producto.Activo = false;

        Assert.False(producto.RequiereReabastecimiento);
    }

    [Fact]
    public void El_precio_al_publico_incluye_el_itbis()
    {
        var producto = Producto();
        producto.PrecioVenta = 100m;
        producto.TasaItbis = 0.18m;

        Assert.Equal(118m, producto.PrecioConItbis);
    }

    [Fact]
    public void Un_producto_exento_se_vende_sin_recargo()
    {
        var producto = Producto();
        producto.PrecioVenta = 100m;
        producto.TasaItbis = 0m;

        Assert.Equal(100m, producto.PrecioConItbis);
    }

    [Fact]
    public void El_margen_se_calcula_sobre_el_precio_de_venta()
    {
        var producto = Producto();
        producto.Costo = 30m;
        producto.PrecioVenta = 40m;

        Assert.Equal(10m, producto.MargenUnitario);
        Assert.Equal(0.25m, producto.MargenPorcentaje);
    }

    [Fact]
    public void Un_producto_sin_precio_no_divide_entre_cero()
    {
        var producto = Producto();
        producto.PrecioVenta = 0m;

        Assert.Equal(0m, producto.MargenPorcentaje);
    }
}
