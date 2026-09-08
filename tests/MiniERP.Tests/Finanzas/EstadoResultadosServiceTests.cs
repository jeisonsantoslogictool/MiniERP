using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Finanzas.Services;

namespace MiniERP.Tests.Finanzas;

/// <summary>
/// Como se arma el estado de resultados: de donde sale cada cifra y cual se queda fuera.
/// </summary>
/// <remarks>
/// El estado de resultados mide ganancia, no efectivo. Por eso solo reune tres cosas
/// —ingresos, costo de lo vendido y egresos operativos— y deja fuera los cobros y los pagos.
/// Un pago a proveedor parece un gasto y no lo es: el costo de esa mercancia ya se conto
/// cuando se vendio. Sumarlo aqui cuenta el mismo costo dos veces y convierte una ganancia
/// en una perdida, sin que nada falle. Es el error mas caro del proyecto porque produce
/// numeros creibles; estas pruebas existen para que no vuelva a entrar.
/// </remarks>
public class EstadoResultadosServiceTests
{
    private static readonly FiltroPeriodo Periodo =
        new(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

    /// <summary>
    /// El escenario del arroz, tal como esta escrito en la documentacion del proyecto:
    /// se compra por 640, se vende en 1,000 y se le paga al proveedor. La utilidad es 360.
    /// Si el pago al proveedor entrara como egreso, darian -280.
    /// </summary>
    [Fact]
    public async Task El_pago_al_proveedor_no_baja_la_utilidad()
    {
        var servicio = new EstadoResultadosService(
            new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m)),
            new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(subtotal: 1000m, costo: 640m)),
            new EgresoServicioFalso(total: 0m));

        var estado = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(360m, estado.Utilidad);
    }

    /// <summary>
    /// El ITBIS cobrado no es ingreso del comercio: ese dinero es de la DGII. Por eso la
    /// linea de ingresos toma el subtotal del resumen y no su total.
    /// </summary>
    [Fact]
    public async Task Los_ingresos_son_el_subtotal_sin_itbis()
    {
        var servicio = new EstadoResultadosService(
            new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m, contadoItbis: 180m)),
            new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(subtotal: 1000m, costo: 640m)),
            new EgresoServicioFalso(total: 0m));

        var estado = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(1000m, estado.Ingresos);
    }

    [Fact]
    public async Task Las_ventas_a_credito_tambien_son_ingreso_aunque_no_se_hayan_cobrado()
    {
        var servicio = new EstadoResultadosService(
            new ReporteIngresosServicioFalso(
                Cifras.Ingresos(contadoSubtotal: 400m, creditoSubtotal: 600m)),
            new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(subtotal: 1000m, costo: 640m)),
            new EgresoServicioFalso(total: 0m));

        var estado = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(1000m, estado.Ingresos);
    }

    /// <summary>
    /// El costo sale del reporte de rentabilidad, que lo lee de <c>LineaFactura.CostoUnitario</c>:
    /// el costo congelado al vender, no el costo actual del producto.
    /// </summary>
    [Fact]
    public async Task El_costo_vendido_sale_del_reporte_de_rentabilidad()
    {
        var servicio = new EstadoResultadosService(
            new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m)),
            new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(subtotal: 1000m, costo: 640m)),
            new EgresoServicioFalso(total: 0m));

        var estado = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(640m, estado.CostoVendido);
        Assert.Equal(360m, estado.MargenBruto);
    }

    [Fact]
    public async Task Los_egresos_operativos_si_bajan_la_utilidad()
    {
        var servicio = new EstadoResultadosService(
            new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m)),
            new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(subtotal: 1000m, costo: 640m)),
            new EgresoServicioFalso(total: 100m));

        var estado = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(100m, estado.Egresos);
        Assert.Equal(260m, estado.Utilidad);
    }

    [Fact]
    public async Task La_utilidad_puede_ser_negativa_cuando_los_egresos_se_comen_el_margen()
    {
        var servicio = new EstadoResultadosService(
            new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m)),
            new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(subtotal: 1000m, costo: 640m)),
            new EgresoServicioFalso(total: 500m));

        var estado = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(-140m, estado.Utilidad);
    }

    /// <summary>
    /// Los tres colaboradores tienen que mirar el mismo periodo: si uno recibiera otro rango,
    /// el reporte mezclaria el margen de un mes con los gastos de otro.
    /// </summary>
    [Fact]
    public async Task El_periodo_llega_igual_a_los_tres_reportes()
    {
        var ingresos = new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m));
        var rentabilidad = new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(1000m, 640m));
        var egresos = new EgresoServicioFalso(total: 0m);
        var servicio = new EstadoResultadosService(ingresos, rentabilidad, egresos);

        await servicio.ObtenerAsync(Periodo);

        Assert.Equal(Periodo.Desde, ingresos.FiltroRecibido!.Desde);
        Assert.Equal(Periodo.Hasta, ingresos.FiltroRecibido!.Hasta);
        Assert.Equal(Periodo.Desde, rentabilidad.FiltroRecibido!.Desde);
        Assert.Equal(Periodo.Hasta, rentabilidad.FiltroRecibido!.Hasta);
        Assert.Equal(Periodo.Desde, egresos.FiltroRecibido!.Desde);
        Assert.Equal(Periodo.Hasta, egresos.FiltroRecibido!.Hasta);
    }

    /// <summary>
    /// El filtro de egresos no se acota a una categoria: el estado de resultados los quiere
    /// todos. Fijar esto evita que una edicion futura le cuele un filtro y deje gastos fuera.
    /// </summary>
    [Fact]
    public async Task Los_egresos_del_periodo_entran_todos_sin_filtrar_por_categoria()
    {
        var egresos = new EgresoServicioFalso(total: 100m);
        var servicio = new EstadoResultadosService(
            new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m)),
            new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(1000m, 640m)),
            egresos);

        await servicio.ObtenerAsync(Periodo);

        Assert.Null(egresos.FiltroRecibido!.CategoriaEgresoId);
    }

    [Fact]
    public async Task Un_periodo_sin_movimiento_da_ceros()
    {
        var servicio = new EstadoResultadosService(
            new ReporteIngresosServicioFalso(Cifras.Ingresos()),
            new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(subtotal: 0m, costo: 0m)),
            new EgresoServicioFalso(total: 0m));

        var estado = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(0m, estado.Ingresos);
        Assert.Equal(0m, estado.CostoVendido);
        Assert.Equal(0m, estado.Egresos);
        Assert.Equal(0m, estado.Utilidad);
    }
}
