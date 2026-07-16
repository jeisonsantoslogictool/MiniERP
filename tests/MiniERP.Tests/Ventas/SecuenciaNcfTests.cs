using MiniERP.Domain.Clientes;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Ventas;

public class SecuenciaNcfTests
{
    private static readonly DateTime Hoy = new(2026, 7, 16);

    private static SecuenciaNcf Secuencia(
        string prefijo = "B02",
        long desde = 1,
        long hasta = 5000,
        long actual = 0,
        int mesesParaVencer = 12) => new()
        {
            TipoComprobante = TipoComprobante.Consumo,
            Prefijo = prefijo,
            Desde = desde,
            Hasta = hasta,
            Actual = actual,
            FechaVencimiento = Hoy.AddMonths(mesesParaVencer),
            Activa = true
        };

    [Fact]
    public void El_ncf_en_papel_lleva_ocho_digitos() =>
        Assert.Equal("B0200000123", Secuencia(prefijo: "B02").Formatear(123));

    [Fact]
    public void El_comprobante_electronico_lleva_diez() =>
        Assert.Equal("E320000000123", Secuencia(prefijo: "E32").Formatear(123));

    [Fact]
    public void El_prefijo_distingue_al_electronico()
    {
        Assert.True(Secuencia(prefijo: "E32").EsElectronica);
        Assert.False(Secuencia(prefijo: "B02").EsElectronica);
    }

    [Fact]
    public void Un_rango_recien_autorizado_puede_emitir() =>
        Assert.True(Secuencia().PuedeEmitir(Hoy));

    [Fact]
    public void Los_disponibles_descuentan_lo_consumido() =>
        Assert.Equal(4900, Secuencia(hasta: 5000, actual: 100).Disponibles);

    [Fact]
    public void Un_rango_agotado_no_emite()
    {
        var secuencia = Secuencia(hasta: 5000, actual: 5000);

        Assert.True(secuencia.Agotada);
        Assert.False(secuencia.PuedeEmitir(Hoy));
        Assert.Equal(0, secuencia.Disponibles);
        Assert.Contains("se agoto", secuencia.MotivoNoDisponible(Hoy));
    }

    [Fact]
    public void Un_rango_vencido_no_emite_aunque_le_queden_numeros()
    {
        var secuencia = Secuencia(actual: 10, mesesParaVencer: -1);

        Assert.True(secuencia.EstaVencida(Hoy));
        Assert.False(secuencia.PuedeEmitir(Hoy));
        Assert.True(secuencia.Disponibles > 0);
        Assert.Contains("vencio", secuencia.MotivoNoDisponible(Hoy));
    }

    [Fact]
    public void El_rango_sirve_hasta_el_ultimo_dia_de_su_vigencia()
    {
        var secuencia = Secuencia();
        secuencia.FechaVencimiento = Hoy;

        Assert.False(secuencia.EstaVencida(Hoy));
        Assert.True(secuencia.PuedeEmitir(Hoy));
    }

    [Fact]
    public void La_hora_no_vence_un_rango_que_vence_hoy()
    {
        var secuencia = Secuencia();
        secuencia.FechaVencimiento = Hoy;

        // Se compara por fecha y no por instante: a las 11 de la noche del dia del
        // vencimiento el comprobante todavia es valido.
        Assert.False(secuencia.EstaVencida(Hoy.AddHours(23)));
    }

    [Fact]
    public void Un_rango_desactivado_no_emite()
    {
        var secuencia = Secuencia();
        secuencia.Activa = false;

        Assert.False(secuencia.PuedeEmitir(Hoy));
        Assert.Contains("desactivada", secuencia.MotivoNoDisponible(Hoy));
    }

    [Fact]
    public void Un_rango_sano_no_da_motivo_de_rechazo() =>
        Assert.Null(Secuencia().MotivoNoDisponible(Hoy));

    [Theory]
    [InlineData(4950, true)]   // quedan 50: justo en el umbral
    [InlineData(4999, true)]   // queda 1
    [InlineData(4949, false)]  // quedan 51: todavia hay margen
    [InlineData(5000, false)]  // agotada: ya no es un aviso, es un bloqueo
    public void El_aviso_salta_cerca_del_final(long actual, bool esperado) =>
        Assert.Equal(esperado, Secuencia(hasta: 5000, actual: actual).PorAgotarse);
}
