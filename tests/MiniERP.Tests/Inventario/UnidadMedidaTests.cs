using MiniERP.Domain.Inventario;

namespace MiniERP.Tests.Inventario;

public class UnidadMedidaTests
{
    private static UnidadMedida Unidad() =>
        new() { Codigo = "UND", Nombre = "Unidad", PermiteDecimales = false, CantidadDecimales = 0 };

    private static UnidadMedida Libra() =>
        new() { Codigo = "LB", Nombre = "Libra", PermiteDecimales = true, CantidadDecimales = 3 };

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void Unidad_acepta_cantidades_enteras(decimal cantidad) =>
        Assert.True(Unidad().CantidadEsValida(cantidad));

    [Theory]
    [InlineData(2.5)]
    [InlineData(0.1)]
    public void Unidad_rechaza_decimales(decimal cantidad) =>
        Assert.False(Unidad().CantidadEsValida(cantidad));

    [Theory]
    [InlineData(2.5)]
    [InlineData(0.125)]
    [InlineData(3)]
    public void Libra_acepta_hasta_tres_decimales(decimal cantidad) =>
        Assert.True(Libra().CantidadEsValida(cantidad));

    [Fact]
    public void Libra_rechaza_mas_precision_de_la_que_pesa_una_balanza() =>
        Assert.False(Libra().CantidadEsValida(2.5001m));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Ninguna_unidad_acepta_cantidades_no_positivas(decimal cantidad)
    {
        Assert.False(Unidad().CantidadEsValida(cantidad));
        Assert.False(Libra().CantidadEsValida(cantidad));
    }
}
