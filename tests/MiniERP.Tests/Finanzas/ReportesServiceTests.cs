using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Finanzas.Services;
using MiniERP.Domain.Finanzas;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Finanzas;

/// <summary>
/// Los dos reportes que leen de las ventas: ingresos y rentabilidad.
/// </summary>
/// <remarks>
/// Ambos servicios son delgados —piden el periodo al repositorio y se lo pasan a la
/// calculadora de dominio— asi que lo que hay que comprobar no es la aritmetica, que ya
/// tiene sus pruebas, sino el cable: que el periodo llegue al repositorio tal como se pidio
/// y que la regla de descartar las anuladas siga en pie al pasar por el servicio. El
/// repositorio los trae anulados a proposito, justo para que esa regla se pueda ver.
/// </remarks>
public class ReportesServiceTests
{
    private static readonly FiltroPeriodo Periodo =
        new(new DateTime(2026, 3, 1), new DateTime(2026, 3, 31));

    // ---------- Ingresos ----------

    private static Factura Factura(decimal subtotal, EstadoFactura estado = EstadoFactura.Emitida) => new()
    {
        Condicion = CondicionPago.Contado,
        Subtotal = subtotal,
        Itbis = 0m,
        Total = subtotal,
        Estado = estado
    };

    [Fact]
    public async Task El_reporte_de_ingresos_resume_las_facturas_del_periodo()
    {
        var repo = new IngresosRepositorioFalso([Factura(1000m), Factura(500m)]);
        var servicio = new ReporteIngresosService(repo);

        var resumen = await servicio.ObtenerAsync(new FiltroReporteIngresos(Periodo.Desde, Periodo.Hasta));

        Assert.Equal(2, resumen.Cantidad);
        Assert.Equal(1500m, resumen.Subtotal);
    }

    [Fact]
    public async Task El_reporte_de_ingresos_descarta_las_facturas_anuladas()
    {
        var repo = new IngresosRepositorioFalso(
            [Factura(1000m), Factura(999m, EstadoFactura.Anulada)]);
        var servicio = new ReporteIngresosService(repo);

        var resumen = await servicio.ObtenerAsync(new FiltroReporteIngresos(Periodo.Desde, Periodo.Hasta));

        Assert.Equal(1, resumen.Cantidad);
        Assert.Equal(1000m, resumen.Subtotal);
    }

    [Fact]
    public async Task El_reporte_de_ingresos_pide_el_periodo_que_se_le_dio()
    {
        var repo = new IngresosRepositorioFalso([]);
        var servicio = new ReporteIngresosService(repo);

        await servicio.ObtenerAsync(new FiltroReporteIngresos(Periodo.Desde, Periodo.Hasta));

        Assert.Equal(Periodo.Desde, repo.Desde);
        Assert.Equal(Periodo.Hasta, repo.Hasta);
    }

    [Fact]
    public async Task Un_periodo_sin_facturas_da_un_resumen_en_cero()
    {
        var servicio = new ReporteIngresosService(new IngresosRepositorioFalso([]));

        var resumen = await servicio.ObtenerAsync(new FiltroReporteIngresos(Periodo.Desde, Periodo.Hasta));

        Assert.Equal(0, resumen.Cantidad);
        Assert.Equal(0m, resumen.Total);
    }

    // ---------- Rentabilidad ----------

    private static RenglonVendido Renglon(
        decimal subtotal, decimal costo, bool anulada = false, string producto = "Arroz selecto") =>
        new(anulada, ProductoId: 1, producto, CategoriaId: 2, "Viveres", subtotal, costo);

    [Fact]
    public async Task El_reporte_de_rentabilidad_resume_los_renglones_del_periodo()
    {
        var repo = new RentabilidadRepositorioFalso(
            [Renglon(1000m, 640m), Renglon(500m, 300m, producto: "Habichuela")]);
        var servicio = new ReporteRentabilidadService(repo);

        var resumen = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(1500m, resumen.Total.Subtotal);
        Assert.Equal(940m, resumen.Total.Costo);
        Assert.Equal(560m, resumen.Total.Margen);
        Assert.Equal(2, resumen.PorProducto.Count);
    }

    [Fact]
    public async Task El_reporte_de_rentabilidad_descarta_los_renglones_de_facturas_anuladas()
    {
        var repo = new RentabilidadRepositorioFalso(
            [Renglon(1000m, 640m), Renglon(999m, 500m, anulada: true)]);
        var servicio = new ReporteRentabilidadService(repo);

        var resumen = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(1000m, resumen.Total.Subtotal);
        Assert.Equal(360m, resumen.Total.Margen);
        Assert.Single(resumen.PorProducto);
    }

    [Fact]
    public async Task El_reporte_de_rentabilidad_pide_el_periodo_que_se_le_dio()
    {
        var repo = new RentabilidadRepositorioFalso([]);
        var servicio = new ReporteRentabilidadService(repo);

        await servicio.ObtenerAsync(Periodo);

        Assert.Equal(Periodo.Desde, repo.Desde);
        Assert.Equal(Periodo.Hasta, repo.Hasta);
    }

    [Fact]
    public async Task Un_periodo_sin_ventas_da_una_rentabilidad_en_cero()
    {
        var servicio = new ReporteRentabilidadService(new RentabilidadRepositorioFalso([]));

        var resumen = await servicio.ObtenerAsync(Periodo);

        Assert.Equal(0m, resumen.Total.Margen);
        Assert.Empty(resumen.PorProducto);
        Assert.Empty(resumen.PorCategoria);
    }

    private sealed class IngresosRepositorioFalso(IReadOnlyList<Factura> facturas) : IReporteIngresosRepositorio
    {
        public DateTime Desde { get; private set; }

        public DateTime Hasta { get; private set; }

        public Task<IReadOnlyList<Factura>> ObtenerFacturasDelPeriodoAsync(
            DateTime desde, DateTime hasta, CancellationToken ct = default)
        {
            Desde = desde;
            Hasta = hasta;
            return Task.FromResult(facturas);
        }
    }

    private sealed class RentabilidadRepositorioFalso(IReadOnlyList<RenglonVendido> renglones)
        : IReporteRentabilidadRepositorio
    {
        public DateTime Desde { get; private set; }

        public DateTime Hasta { get; private set; }

        public Task<IReadOnlyList<RenglonVendido>> ObtenerRenglonesDelPeriodoAsync(
            DateTime desde, DateTime hasta, CancellationToken ct = default)
        {
            Desde = desde;
            Hasta = hasta;
            return Task.FromResult(renglones);
        }
    }
}
