using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Application.Finanzas.Services;

public interface IEstadoResultadosService
{
    Task<EstadoResultados> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default);
}

/// <summary>
/// Estado de resultados: reune ingresos, costo de lo vendido y egresos del periodo.
/// No entran cobros ni pagos.
/// </summary>
public class EstadoResultadosService(
    IReporteIngresosService ingresos,
    IReporteRentabilidadService rentabilidad,
    IEgresoService egresos) : IEstadoResultadosService
{
    public async Task<EstadoResultados> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default)
    {
        var ing = await ingresos.ObtenerAsync(new FiltroReporteIngresos(filtro.Desde, filtro.Hasta), ct);
        var rent = await rentabilidad.ObtenerAsync(filtro, ct);
        var egr = await egresos.ObtenerTotalAsync(new FiltroEgresos(Desde: filtro.Desde, Hasta: filtro.Hasta), ct);

        return new EstadoResultados(ing.Subtotal, rent.Total.Costo, egr);
    }
}
