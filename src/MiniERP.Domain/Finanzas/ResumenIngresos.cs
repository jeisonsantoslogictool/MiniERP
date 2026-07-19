using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Resumen de ventas de un periodo, separando contado de credito. Es un valor calculado,
/// no un registro: se arma leyendo las facturas y no se guarda.
/// </summary>
public record ResumenIngresos(
    ResumenIngresosPorCondicion Contado,
    ResumenIngresosPorCondicion Credito)
{
    public int Cantidad => Contado.Cantidad + Credito.Cantidad;
    public decimal Subtotal => Contado.Subtotal + Credito.Subtotal;
    public decimal Itbis => Contado.Itbis + Credito.Itbis;
    public decimal Total => Contado.Total + Credito.Total;

    /// <summary>
    /// Arma el resumen desde las facturas del periodo, separando por condicion de pago.
    /// </summary>
    public static ResumenIngresos Calcular(IEnumerable<Factura> facturas)
    {
        ArgumentNullException.ThrowIfNull(facturas);

        // Una venta anulada no es un ingreso: se descarta antes de sumar.
        var emitidas = facturas.Where(f => !f.EstaAnulada).ToList();

        return new ResumenIngresos(
            Resumir(emitidas.Where(f => f.Condicion == CondicionPago.Contado)),
            Resumir(emitidas.Where(f => f.Condicion == CondicionPago.Credito)));
    }

    private static ResumenIngresosPorCondicion Resumir(IEnumerable<Factura> facturas)
    {
        var lista = facturas.ToList();

        return new ResumenIngresosPorCondicion(
            lista.Count,
            RetailConstants.RedondearImporte(lista.Sum(f => f.Subtotal)),
            RetailConstants.RedondearImporte(lista.Sum(f => f.Itbis)),
            RetailConstants.RedondearImporte(lista.Sum(f => f.Total)));
    }
}

public record ResumenIngresosPorCondicion(int Cantidad, decimal Subtotal, decimal Itbis, decimal Total);
