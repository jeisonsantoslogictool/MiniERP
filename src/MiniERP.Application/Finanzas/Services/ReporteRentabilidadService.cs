using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Application.Finanzas.Services;

public interface IReporteRentabilidadService
{
    Task<ResumenRentabilidad> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default);
}

/// <summary>
/// Reporte de rentabilidad: pide los renglones vendidos del periodo y los resume con la
/// calculadora de dominio.
/// </summary>
public class ReporteRentabilidadService(IReporteRentabilidadRepositorio renglones) : IReporteRentabilidadService
{
    public async Task<ResumenRentabilidad> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default)
    {
        var delPeriodo = await renglones.ObtenerRenglonesDelPeriodoAsync(filtro.Desde, filtro.Hasta, ct);

        return ResumenRentabilidad.Calcular(delPeriodo);
    }
}
