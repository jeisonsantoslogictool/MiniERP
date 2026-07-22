using MiniERP.Application.Finanzas.Dtos;

namespace MiniERP.Application.Finanzas.Services;

public interface IPanelFinanzasService
{
    Task<PanelFinanzasDto> ObtenerAsync(CancellationToken ct = default);
}

/// <summary>
/// Panel de finanzas: ensambla los reportes para el dia y el mes en curso.
/// Lo primero que el dueno mira al llegar.
/// </summary>
public class PanelFinanzasService(
    IReporteIngresosService ingresos,
    IReporteRentabilidadService rentabilidad,
    IEgresoService egresos,
    IEstadoResultadosService estado,
    IFlujoCajaService flujo) : IPanelFinanzasService
{
    public async Task<PanelFinanzasDto> ObtenerAsync(CancellationToken ct = default)
    {
        var hoy = DateTime.UtcNow.Date;
        var inicioMes = new DateTime(hoy.Year, hoy.Month, 1);

        var ventasDia = await ingresos.ObtenerAsync(new FiltroReporteIngresos(hoy, hoy), ct);
        var margenDia = await rentabilidad.ObtenerAsync(new FiltroPeriodo(hoy, hoy), ct);
        var egresosMes = await egresos.ObtenerTotalAsync(new FiltroEgresos(Desde: inicioMes, Hasta: hoy), ct);
        var estadoMes = await estado.ObtenerAsync(new FiltroPeriodo(inicioMes, hoy), ct);
        var flujoMes = await flujo.ObtenerAsync(new FiltroPeriodo(inicioMes, hoy), ct);

        return new PanelFinanzasDto(
            VentasDia: ventasDia.Total,
            MargenDia: margenDia.Total.Margen,
            EgresosMes: egresosMes,
            UtilidadMes: estadoMes.Utilidad,
            EfectivoMes: flujoMes.EfectivoNeto);
    }
}
