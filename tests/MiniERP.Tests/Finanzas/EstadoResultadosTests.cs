using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

public class EstadoResultadosTests
{
    [Fact]
    public void El_margen_bruto_es_ingresos_menos_costo()
    {
        var er = new EstadoResultados(Ingresos: 1000, CostoVendido: 600, Egresos: 0);

        Assert.Equal(400m, er.MargenBruto);
    }

    [Fact]
    public void La_utilidad_resta_los_egresos()
    {
        var er = new EstadoResultados(Ingresos: 1000, CostoVendido: 600, Egresos: 250);

        Assert.Equal(150m, er.Utilidad); // 1000 - 600 - 250
    }

    [Fact]
    public void La_utilidad_puede_ser_negativa()
    {
        var er = new EstadoResultados(Ingresos: 1000, CostoVendido: 600, Egresos: 500);

        Assert.Equal(-100m, er.Utilidad);
    }
}
