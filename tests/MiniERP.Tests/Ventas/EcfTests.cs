using MiniERP.Domain.Clientes;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Ventas;

/// <summary>
/// Reglas del comprobante electronico. La simulacion se prueba igual que si fuera real:
/// lo que se simula es la firma, no el formato ni el correlativo.
/// </summary>
public class EcfTests
{
    private static readonly DateTime Emision = new(2026, 7, 21);

    [Theory]
    [InlineData(TipoComprobante.CreditoFiscal, "E31")]
    [InlineData(TipoComprobante.Consumo, "E32")]
    [InlineData(TipoComprobante.RegimenEspecial, "E44")]
    [InlineData(TipoComprobante.Gubernamental, "E45")]
    public void El_prefijo_electronico_no_es_el_del_papel(TipoComprobante tipo, string esperado) =>
        Assert.Equal(esperado, ComprobanteElectronico.PrefijoDe(tipo));

    [Fact]
    public void El_codigo_del_tipo_va_sin_la_letra() =>
        Assert.Equal("32", ComprobanteElectronico.CodigoTipoDe(TipoComprobante.Consumo));

    [Fact]
    public void El_encf_conserva_el_correlativo_y_lo_lleva_a_diez_digitos() =>
        Assert.Equal(
            "E320000000123",
            ComprobanteElectronico.DerivarENcf("B0200000123", TipoComprobante.Consumo));

    [Fact]
    public void Los_digitos_del_prefijo_no_se_cuelan_en_el_correlativo()
    {
        // El 02 de B02 tambien es digito. Filtrar "los digitos del NCF" daria
        // E320200000002 en vez de E320000000002, y el comprobante quedaria en otro numero.
        var eNcf = ComprobanteElectronico.DerivarENcf("B0200000002", TipoComprobante.Consumo);

        Assert.Equal("E320000000002", eNcf);
        Assert.Equal(13, eNcf.Length);
    }

    [Fact]
    public void Un_credito_fiscal_no_se_convierte_en_consumo() =>
        Assert.Equal(
            "E310000000123",
            ComprobanteElectronico.DerivarENcf("B0100000123", TipoComprobante.CreditoFiscal));

    [Fact]
    public void Un_ncf_sin_correlativo_se_rechaza() =>
        Assert.Throws<InvalidOperationException>(
            () => ComprobanteElectronico.DerivarENcf("B02", TipoComprobante.Consumo));

    [Fact]
    public void Un_correlativo_que_no_es_numero_se_rechaza() =>
        Assert.Throws<InvalidOperationException>(
            () => ComprobanteElectronico.DerivarENcf("B02ABCDEFGH", TipoComprobante.Consumo));

    [Fact]
    public void Sin_ncf_no_hay_comprobante_electronico() =>
        Assert.Throws<ArgumentException>(
            () => ComprobanteElectronico.DerivarENcf("   ", TipoComprobante.Consumo));

    [Fact]
    public void El_codigo_de_seguridad_lleva_seis_caracteres() =>
        Assert.Equal(6, CodigoSeguridadEcf.Calcular("130123456", "E320000000123", 114.00m, Emision).Length);

    [Fact]
    public void El_mismo_comprobante_da_siempre_el_mismo_codigo()
    {
        // Determinista como lo seria una firma: si cambiara en cada consulta, el codigo
        // impreso en la factura de ayer no coincidiria con el de hoy.
        var primero = CodigoSeguridadEcf.Calcular("130123456", "E320000000123", 114.00m, Emision);
        var segundo = CodigoSeguridadEcf.Calcular("130123456", "E320000000123", 114.00m, Emision);

        Assert.Equal(primero, segundo);
    }

    [Theory]
    [InlineData("130999999", "E320000000123", 114.00, 2026, 7, 21)]  // otro emisor
    [InlineData("130123456", "E320000000124", 114.00, 2026, 7, 21)]  // otro comprobante
    [InlineData("130123456", "E320000000123", 115.00, 2026, 7, 21)]  // otro monto
    [InlineData("130123456", "E320000000123", 114.00, 2026, 7, 22)]  // otra fecha
    public void Cambiar_cualquier_campo_firmado_cambia_el_codigo(
        string rnc, string eNcf, decimal total, int ano, int mes, int dia)
    {
        var original = CodigoSeguridadEcf.Calcular("130123456", "E320000000123", 114.00m, Emision);
        var alterado = CodigoSeguridadEcf.Calcular(rnc, eNcf, total, new DateTime(ano, mes, dia));

        Assert.NotEqual(original, alterado);
    }

    [Fact]
    public void El_codigo_solo_lleva_letras_y_numeros()
    {
        // Viaja como parametro de la URL del QR: un '+' o un '/' del base64 se escaparia
        // y el codigo impreso no coincidiria con el que la DGII recibe.
        for (var i = 1; i <= 50; i++)
        {
            var codigo = CodigoSeguridadEcf.Calcular("130123456", $"E32{i:D10}", i * 3.5m, Emision);

            Assert.All(codigo, c => Assert.True(char.IsAsciiLetterOrDigit(c)));
        }
    }
}
