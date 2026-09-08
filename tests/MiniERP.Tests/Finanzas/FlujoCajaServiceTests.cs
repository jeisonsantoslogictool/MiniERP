using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Finanzas.Services;

namespace MiniERP.Tests.Finanzas;

/// <summary>
/// Como se arma el flujo de caja: que entra a la gaveta y que sale.
/// </summary>
/// <remarks>
/// Es la contraparte del estado de resultados y se lee al reves. Aqui si entran los cobros
/// y los pagos —son efectivo que se movio— y no entra la venta a credito, que todavia no se
/// ha cobrado. Un negocio puede tener utilidad y no tener efectivo, o al reves; por eso el
/// anteproyecto pide los dos reportes por separado y estas pruebas fijan la diferencia.
/// </remarks>
public class FlujoCajaServiceTests
{
    private static readonly FiltroPeriodo Periodo =
        new(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

    private static FlujoCajaService Servicio(
        decimal contadoSubtotal = 0m, decimal contadoItbis = 0m, decimal creditoSubtotal = 0m,
        decimal egresos = 0m, decimal cobros = 0m, decimal pagos = 0m) =>
        new(new ReporteIngresosServicioFalso(
                Cifras.Ingresos(contadoSubtotal, contadoItbis, creditoSubtotal)),
            new EgresoServicioFalso(egresos),
            new FlujoCajaRepositorioFalso(cobros, pagos));

    /// <summary>
    /// El efectivo de contado incluye el ITBIS. No es ingreso del comercio —hay que
    /// entregarlo a la DGII— pero mientras tanto esta en la gaveta, y el flujo mide eso.
    /// </summary>
    [Fact]
    public async Task Las_ventas_de_contado_entran_con_su_itbis()
    {
        var servicio = Servicio(contadoSubtotal: 1000m, contadoItbis: 180m);

        var flujo = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(1180m, flujo.VentasContado);
    }

    [Fact]
    public async Task Las_ventas_a_credito_no_entran_porque_todavia_no_se_han_cobrado()
    {
        var servicio = Servicio(contadoSubtotal: 400m, creditoSubtotal: 600m);

        var flujo = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(400m, flujo.VentasContado);
        Assert.Equal(400m, flujo.Entradas);
    }

    [Fact]
    public async Task El_cobro_a_un_cliente_es_entrada_de_efectivo()
    {
        var servicio = Servicio(contadoSubtotal: 400m, cobros: 600m);

        var flujo = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(600m, flujo.Cobros);
        Assert.Equal(1000m, flujo.Entradas);
    }

    /// <summary>
    /// Aqui el pago al proveedor si cuenta, y por eso no puede contar en el estado de
    /// resultados: el mismo desembolso pertenece a un reporte y no al otro.
    /// </summary>
    [Fact]
    public async Task El_pago_al_proveedor_si_es_salida_de_efectivo()
    {
        var servicio = Servicio(contadoSubtotal: 1000m, pagos: 640m);

        var flujo = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(640m, flujo.Pagos);
        Assert.Equal(640m, flujo.Salidas);
        Assert.Equal(360m, flujo.EfectivoNeto);
    }

    [Fact]
    public async Task Los_egresos_operativos_tambien_salen_de_la_gaveta()
    {
        var servicio = Servicio(contadoSubtotal: 1000m, egresos: 250m, pagos: 640m);

        var flujo = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(250m, flujo.Egresos);
        Assert.Equal(890m, flujo.Salidas);
        Assert.Equal(110m, flujo.EfectivoNeto);
    }

    /// <summary>
    /// Vender fiado y pagarle al proveedor deja el negocio sin efectivo aunque haya ganado:
    /// es justo el caso que el reporte tiene que poder mostrar.
    /// </summary>
    [Fact]
    public async Task Vender_todo_a_credito_y_pagar_al_proveedor_deja_el_efectivo_negativo()
    {
        var servicio = Servicio(creditoSubtotal: 1000m, pagos: 640m);

        var flujo = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(0m, flujo.Entradas);
        Assert.Equal(-640m, flujo.EfectivoNeto);
    }

    [Fact]
    public async Task El_periodo_llega_igual_a_los_ingresos_y_a_los_egresos()
    {
        var ingresos = new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m));
        var egresos = new EgresoServicioFalso(total: 0m);
        var servicio = new FlujoCajaService(ingresos, egresos, new FlujoCajaRepositorioFalso(0m, 0m));

        await servicio.ObtenerAsync(Periodo);

        Assert.Equal(Periodo.Desde, ingresos.FiltroRecibido!.Desde);
        Assert.Equal(Periodo.Hasta, ingresos.FiltroRecibido!.Hasta);
        Assert.Equal(Periodo.Desde, egresos.FiltroRecibido!.Desde);
        Assert.Equal(Periodo.Hasta, egresos.FiltroRecibido!.Hasta);
    }

    [Fact]
    public async Task Un_periodo_sin_movimiento_da_ceros()
    {
        var servicio = Servicio();

        var flujo = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(0m, flujo.Entradas);
        Assert.Equal(0m, flujo.Salidas);
        Assert.Equal(0m, flujo.EfectivoNeto);
    }
}
