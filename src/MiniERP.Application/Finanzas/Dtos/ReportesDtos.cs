namespace MiniERP.Application.Finanzas.Dtos;

/// <summary>Rango de fechas de un reporte de finanzas.</summary>
public record FiltroReporteIngresos(DateTime Desde, DateTime Hasta);

/// <summary>Rango de fechas de un reporte. Reusable por los reportes de finanzas.</summary>
public record FiltroPeriodo(DateTime Desde, DateTime Hasta);
