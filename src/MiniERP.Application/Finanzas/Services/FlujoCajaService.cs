using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Application.Finanzas.Services;

public interface IFlujoCajaService
{
    Task<FlujoCaja> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default);
}

/// <summary>
/// Flujo de caja: efectivo que entra (ventas de contado + cobros) menos el que sale
/// (egresos + pagos a proveedor).
/// </summary>
public class FlujoCajaService(
    IReporteIngresosService ingresos,
    IEgresoService egresos,
    IFlujoCajaRepositorio caja) : IFlujoCajaService
{
    public async Task<FlujoCaja> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default)
    {
        var ing = await ingresos.ObtenerAsync(new FiltroReporteIngresos(filtro.Desde, filtro.Hasta), ct);
        var egr = await egresos.ObtenerTotalAsync(new FiltroEgresos(Desde: filtro.Desde, Hasta: filtro.Hasta), ct);
        var cobros = await caja.SumarCobrosAsync(filtro.Desde, filtro.Hasta, ct);
        var pagos = await caja.SumarPagosAsync(filtro.Desde, filtro.Hasta, ct);

        // El efectivo de contado incluye el ITBIS: es dinero que de verdad entro a la gaveta.
        return new FlujoCaja(ing.Contado.Total, cobros, egr, pagos);
    }
}
