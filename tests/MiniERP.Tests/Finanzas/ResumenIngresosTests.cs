using MiniERP.Domain.Finanzas;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Finanzas;

public class ResumenIngresosTests
{
    private static Factura Factura(
        CondicionPago condicion, decimal subtotal, decimal itbis,
        EstadoFactura estado = EstadoFactura.Emitida) => new()
    {
        Condicion = condicion,
        Subtotal = subtotal,
        Itbis = itbis,
        Total = subtotal + itbis,
        Estado = estado
    };

    [Fact]
    public void Un_periodo_vacio_da_ceros()
    {
        var resumen = ResumenIngresos.Calcular([]);

        Assert.Equal(0, resumen.Cantidad);
        Assert.Equal(0m, resumen.Subtotal);
        Assert.Equal(0m, resumen.Itbis);
        Assert.Equal(0m, resumen.Total);
    }

    [Fact]
    public void Separa_contado_de_credito()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, subtotal: 1000, itbis: 180),
            Factura(CondicionPago.Credito, subtotal: 500, itbis: 90),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(1000m, resumen.Contado.Subtotal);
        Assert.Equal(500m, resumen.Credito.Subtotal);
    }

    [Fact]
    public void Suma_subtotal_itbis_y_total()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, 1000, 180),
            Factura(CondicionPago.Contado, 500, 90),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(1500m, resumen.Contado.Subtotal);
        Assert.Equal(270m, resumen.Contado.Itbis);
        Assert.Equal(1770m, resumen.Contado.Total);
    }

    [Fact]
    public void Cuenta_las_facturas_por_condicion()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, 100, 18),
            Factura(CondicionPago.Contado, 200, 36),
            Factura(CondicionPago.Credito, 300, 54),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(2, resumen.Contado.Cantidad);
        Assert.Equal(1, resumen.Credito.Cantidad);
    }

    [Fact]
    public void Los_totales_generales_suman_contado_y_credito()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, 1000, 180),
            Factura(CondicionPago.Credito, 500, 90),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(1500m, resumen.Subtotal);
        Assert.Equal(270m, resumen.Itbis);
        Assert.Equal(1770m, resumen.Total);
        Assert.Equal(2, resumen.Cantidad);
    }

    [Fact]
    public void Excluye_las_facturas_anuladas()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, 1000, 180),
            Factura(CondicionPago.Contado, 999, 179, estado: EstadoFactura.Anulada),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(1, resumen.Contado.Cantidad);
        Assert.Equal(1000m, resumen.Contado.Subtotal);
    }
}
