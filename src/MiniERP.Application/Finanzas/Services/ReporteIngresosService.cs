using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Application.Finanzas.Services;

public interface IReporteIngresosService
{
    Task<ResumenIngresos> ObtenerAsync(FiltroReporteIngresos filtro, CancellationToken ct = default);
}

/// <summary>
/// Reporte de ingresos: pide las facturas del periodo y las resume con la calculadora
/// de dominio.
/// </summary>
public class ReporteIngresosService(IReporteIngresosRepositorio facturas) : IReporteIngresosService
{
    public async Task<ResumenIngresos> ObtenerAsync(FiltroReporteIngresos filtro, CancellationToken ct = default)
    {
        var delPeriodo = await facturas.ObtenerFacturasDelPeriodoAsync(filtro.Desde, filtro.Hasta, ct);

        return ResumenIngresos.Calcular(delPeriodo);
    }
}
