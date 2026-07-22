namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Flujo de caja de un periodo: cuanto efectivo entra y sale de la gaveta.
/// Distinto del estado de resultados: mide efectivo, no ganancia.
/// </summary>
public record FlujoCaja(decimal VentasContado, decimal Cobros, decimal Egresos, decimal Pagos)
{
    public decimal Entradas => VentasContado + Cobros;

    public decimal Salidas => Egresos + Pagos;

    public decimal EfectivoNeto => Entradas - Salidas;
}
