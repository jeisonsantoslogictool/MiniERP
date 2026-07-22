using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

public class FlujoCajaTests
{
    [Fact]
    public void Las_entradas_son_ventas_de_contado_mas_cobros()
    {
        var f = new FlujoCaja(VentasContado: 1000, Cobros: 300, Egresos: 0, Pagos: 0);

        Assert.Equal(1300m, f.Entradas);
    }

    [Fact]
    public void Las_salidas_son_egresos_mas_pagos()
    {
        var f = new FlujoCaja(VentasContado: 0, Cobros: 0, Egresos: 200, Pagos: 500);

        Assert.Equal(700m, f.Salidas);
    }

    [Fact]
    public void El_efectivo_neto_es_entradas_menos_salidas()
    {
        var f = new FlujoCaja(VentasContado: 1000, Cobros: 300, Egresos: 200, Pagos: 500);

        Assert.Equal(600m, f.EfectivoNeto); // (1000+300) - (200+500)
    }
}
