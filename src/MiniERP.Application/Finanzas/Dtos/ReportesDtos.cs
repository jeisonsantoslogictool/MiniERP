namespace MiniERP.Application.Finanzas.Dtos;

/// <summary>Rango de fechas de un reporte de finanzas.</summary>
public record FiltroReporteIngresos(DateTime Desde, DateTime Hasta);

/// <summary>Rango de fechas de un reporte. Reusable por los reportes de finanzas.</summary>
public record FiltroPeriodo(DateTime Desde, DateTime Hasta);

/// <summary>Cifras del panel de finanzas: lo primero que el dueno mira al llegar.</summary>
public record PanelFinanzasDto(
    decimal VentasDia,
    decimal MargenDia,
    decimal EgresosMes,
    decimal UtilidadMes,
    decimal EfectivoMes);
