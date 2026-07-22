namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Estado de resultados de un periodo: cuanto gana el negocio.
/// No entran cobros ni pagos: el costo de la mercancia ya se conto al vender.
/// </summary>
public record EstadoResultados(decimal Ingresos, decimal CostoVendido, decimal Egresos)
{
    public decimal MargenBruto => Ingresos - CostoVendido;

    public decimal Utilidad => MargenBruto - Egresos;
}
