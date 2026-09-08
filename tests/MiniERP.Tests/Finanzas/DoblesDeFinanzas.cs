using MiniERP.Application.Common;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Finanzas.Services;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

/// <summary>
/// Cifras de reporte armadas a mano, para darle a un servicio de ensamblaje una entrada fija.
/// </summary>
internal static class Cifras
{
    public static ResumenIngresos Ingresos(
        decimal contadoSubtotal = 0m, decimal contadoItbis = 0m,
        decimal creditoSubtotal = 0m, decimal creditoItbis = 0m) =>
        new(new ResumenIngresosPorCondicion(
                contadoSubtotal == 0m ? 0 : 1, contadoSubtotal, contadoItbis, contadoSubtotal + contadoItbis),
            new ResumenIngresosPorCondicion(
                creditoSubtotal == 0m ? 0 : 1, creditoSubtotal, creditoItbis, creditoSubtotal + creditoItbis));

    public static ResumenRentabilidad Rentabilidad(decimal subtotal, decimal costo) =>
        new(new RentabilidadTotal(subtotal, costo, subtotal - costo,
                subtotal == 0m ? 0m : (subtotal - costo) / subtotal),
            [], []);
}

/// <summary>
/// Dobles de los servicios de finanzas, compartidos por las pruebas de los reportes que
/// se ensamblan a partir de otros: estado de resultados, flujo de caja y panel.
/// </summary>
/// <remarks>
/// Viven aparte porque los tres los usan. Demostrar que el estado de resultados ignora los
/// cobros y los pagos exige poder fijarle los ingresos y el costo, y repetir estos dobles en
/// cada archivo dejaria tres copias que se desincronizan a la primera firma que cambie.
/// Cada uno anota el filtro que recibio, para poder comprobar que el periodo llega intacto
/// a quien lo consulta. Lo que el servicio bajo prueba no usa lanza a proposito: si una
/// prueba futura lo necesita, es que el servicio cambio y hay que mirarlo.
/// </remarks>
internal sealed class ReporteIngresosServicioFalso(ResumenIngresos resumen) : IReporteIngresosService
{
    public FiltroReporteIngresos? FiltroRecibido { get; private set; }

    public Task<ResumenIngresos> ObtenerAsync(FiltroReporteIngresos filtro, CancellationToken ct = default)
    {
        FiltroRecibido = filtro;
        return Task.FromResult(resumen);
    }
}

internal sealed class ReporteRentabilidadServicioFalso(ResumenRentabilidad resumen) : IReporteRentabilidadService
{
    public FiltroPeriodo? FiltroRecibido { get; private set; }

    public Task<ResumenRentabilidad> ObtenerAsync(FiltroPeriodo filtro, CancellationToken ct = default)
    {
        FiltroRecibido = filtro;
        return Task.FromResult(resumen);
    }
}

/// <summary>Solo responde el total del periodo; es lo unico que los reportes le piden.</summary>
internal sealed class EgresoServicioFalso(decimal total) : IEgresoService
{
    public FiltroEgresos? FiltroRecibido { get; private set; }

    public Task<decimal> ObtenerTotalAsync(FiltroEgresos filtro, CancellationToken ct = default)
    {
        FiltroRecibido = filtro;
        return Task.FromResult(total);
    }

    public Task<PaginaDe<EgresoListaDto>> BuscarAsync(FiltroEgresos filtro, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<EgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Resultado<int>> GuardarAsync(EgresoFormDto form, string? usuarioId, CancellationToken ct = default) =>
        throw new NotSupportedException();

    public Task<Resultado> EliminarAsync(int id, CancellationToken ct = default) =>
        throw new NotSupportedException();
}

/// <summary>El efectivo que movieron los terceros: cobros a clientes y pagos a proveedor.</summary>
internal sealed class FlujoCajaRepositorioFalso(decimal cobros, decimal pagos) : IFlujoCajaRepositorio
{
    public Task<decimal> SumarCobrosAsync(DateTime desde, DateTime hasta, CancellationToken ct = default) =>
        Task.FromResult(cobros);

    public Task<decimal> SumarPagosAsync(DateTime desde, DateTime hasta, CancellationToken ct = default) =>
        Task.FromResult(pagos);
}
