using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Finanzas.Services;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

/// <summary>
/// El panel de finanzas: cinco cifras, cada una de su reporte y de su periodo.
/// </summary>
/// <remarks>
/// El panel no calcula nada; solo pide y coloca. El riesgo aqui es de cableado: que una
/// cifra salga del reporte equivocado, o que el margen del dia se mida contra los gastos
/// del mes. Por eso las pruebas le dan a cada reporte un numero distinto y comprueban en
/// que casilla cae, y revisan que el periodo del dia sea un dia y el del mes empiece el 1.
/// </remarks>
public class PanelFinanzasServiceTests
{
    private const decimal VentasDelDia = 1180m;
    private const decimal MargenDelDia = 360m;
    private const decimal EgresosDelMes = 250m;
    private const decimal UtilidadDelMes = 4100m;
    private const decimal EfectivoDelMes = 2750m;

    private static PanelFinanzasService Servicio(
        out ReporteIngresosServicioFalso ingresos,
        out ReporteRentabilidadServicioFalso rentabilidad,
        out EgresoServicioFalso egresos,
        out EstadoResultadosServicioFalso estado,
        out FlujoCajaServicioFalso flujo)
    {
        ingresos = new ReporteIngresosServicioFalso(Cifras.Ingresos(contadoSubtotal: 1000m, contadoItbis: 180m));
        rentabilidad = new ReporteRentabilidadServicioFalso(Cifras.Rentabilidad(subtotal: 1000m, costo: 640m));
        egresos = new EgresoServicioFalso(EgresosDelMes);
        estado = new EstadoResultadosServicioFalso(new EstadoResultados(
            Ingresos: 10000m, CostoVendido: 5650m, Egresos: 250m));
        flujo = new FlujoCajaServicioFalso(new FlujoCaja(
            VentasContado: 3000m, Cobros: 500m, Egresos: 250m, Pagos: 500m));

        return new PanelFinanzasService(ingresos, rentabilidad, egresos, estado, flujo);
    }

    [Fact]
    public async Task Cada_cifra_sale_de_su_reporte()
    {
        var servicio = Servicio(out _, out _, out _, out _, out _);

        var panel = await servicio.ObtenerAsync();

        Assert.Equal(VentasDelDia, panel.VentasDia);
        Assert.Equal(MargenDelDia, panel.MargenDia);
        Assert.Equal(EgresosDelMes, panel.EgresosMes);
        Assert.Equal(UtilidadDelMes, panel.UtilidadMes);
        Assert.Equal(EfectivoDelMes, panel.EfectivoMes);
    }

    /// <summary>
    /// La venta del dia se muestra con ITBIS: es lo que el dueno espera cuadrar contra la
    /// gaveta al cerrar. El margen, en cambio, ya viene neto de impuesto.
    /// </summary>
    [Fact]
    public async Task Las_ventas_del_dia_incluyen_el_itbis_cobrado()
    {
        var servicio = Servicio(out _, out _, out _, out _, out _);

        var panel = await servicio.ObtenerAsync();

        Assert.Equal(1180m, panel.VentasDia);
    }

    [Fact]
    public async Task Las_cifras_del_dia_miran_un_solo_dia()
    {
        var servicio = Servicio(out var ingresos, out var rentabilidad, out _, out _, out _);

        await servicio.ObtenerAsync();

        Assert.Equal(ingresos.FiltroRecibido!.Desde, ingresos.FiltroRecibido!.Hasta);
        Assert.Equal(rentabilidad.FiltroRecibido!.Desde, rentabilidad.FiltroRecibido!.Hasta);
    }

    [Fact]
    public async Task Las_cifras_del_mes_empiezan_el_dia_primero()
    {
        var servicio = Servicio(out _, out _, out var egresos, out var estado, out var flujo);

        await servicio.ObtenerAsync();

        Assert.Equal(1, egresos.FiltroRecibido!.Desde!.Value.Day);
        Assert.Equal(1, estado.FiltroRecibido!.Desde.Day);
        Assert.Equal(1, flujo.FiltroRecibido!.Desde.Day);
    }

    /// <summary>
    /// El mes en curso termina hoy, no a fin de mes: el panel no muestra dias que no han
    /// ocurrido.
    /// </summary>
    [Fact]
    public async Task El_mes_en_curso_termina_hoy()
    {
        var servicio = Servicio(out var ingresos, out _, out _, out var estado, out _);

        await servicio.ObtenerAsync();

        Assert.Equal(ingresos.FiltroRecibido!.Hasta, estado.FiltroRecibido!.Hasta);
        Assert.Equal(estado.FiltroRecibido!.Desde.Month, estado.FiltroRecibido!.Hasta.Month);
    }

    private sealed class EstadoResultadosServicioFalso(EstadoResultados estado) : IEstadoResultadosService
    {
        public FiltroPeriodo? FiltroRecibido { get; private set; }

        public Task<EstadoResultados> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default)
        {
            FiltroRecibido = filtro;
            return Task.FromResult(estado);
        }
    }

    private sealed class FlujoCajaServicioFalso(FlujoCaja flujo) : IFlujoCajaService
    {
        public FiltroPeriodo? FiltroRecibido { get; private set; }

        public Task<FlujoCaja> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default)
        {
            FiltroRecibido = filtro;
            return Task.FromResult(flujo);
        }
    }
}
