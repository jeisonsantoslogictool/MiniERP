using MiniERP.Domain.Clientes;

namespace MiniERP.Tests.Clientes;

public class ClienteTests
{
    private static Cliente Cliente(decimal limite = 0, decimal balance = 0) => new()
    {
        Codigo = "CLI-001",
        Nombre = "Colmado La Esquina",
        LimiteCredito = limite,
        BalanceActual = balance
    };

    [Fact]
    public void Sin_limite_el_cliente_es_solo_de_contado()
    {
        var cliente = Cliente(limite: 0);

        Assert.False(cliente.TieneCredito);
        Assert.False(cliente.PuedeAsumirCredito(1));
    }

    [Fact]
    public void El_credito_disponible_descuenta_lo_que_ya_debe() =>
        Assert.Equal(3000m, Cliente(limite: 5000, balance: 2000).CreditoDisponible);

    [Fact]
    public void El_credito_disponible_nunca_es_negativo() =>
        Assert.Equal(0m, Cliente(limite: 5000, balance: 7000).CreditoDisponible);

    [Fact]
    public void Se_detecta_al_cliente_que_paso_su_limite() =>
        Assert.True(Cliente(limite: 5000, balance: 7000).ExcedeLimite);

    [Theory]
    [InlineData(2000, true)]
    [InlineData(3000, true)]   // justo el disponible
    [InlineData(3001, false)]  // un peso mas y no pasa
    public void El_cargo_a_credito_se_topa_en_el_disponible(decimal monto, bool esperado) =>
        Assert.Equal(esperado, Cliente(limite: 5000, balance: 2000).PuedeAsumirCredito(monto));

    [Fact]
    public void Un_cliente_inactivo_no_puede_fiar()
    {
        var cliente = Cliente(limite: 5000);
        cliente.Activo = false;

        Assert.False(cliente.PuedeAsumirCredito(100));
    }

    [Theory]
    [InlineData(TipoDocumento.Cedula, "00112345678", true)]
    [InlineData(TipoDocumento.Cedula, "001-1234567-8", true)]  // con guiones tambien
    [InlineData(TipoDocumento.Cedula, "123456789", false)]     // largo de RNC
    [InlineData(TipoDocumento.Rnc, "131123456", true)]
    [InlineData(TipoDocumento.Rnc, "1311234567", false)]
    [InlineData(TipoDocumento.Rnc, "13112345A", false)]        // letras no
    public void El_documento_se_valida_por_largo(
        TipoDocumento tipo, string numero, bool esperado)
    {
        var cliente = Cliente();
        cliente.TipoDocumento = tipo;
        cliente.NumeroDocumento = numero;

        Assert.Equal(esperado, cliente.DocumentoTieneFormatoValido());
    }

    [Fact]
    public void El_cliente_de_contado_no_necesita_documento()
    {
        var cliente = Cliente();
        cliente.TipoDocumento = TipoDocumento.Ninguno;
        cliente.NumeroDocumento = null;

        Assert.True(cliente.DocumentoTieneFormatoValido());
    }

    [Fact]
    public void El_credito_fiscal_sin_rnc_no_se_sustenta()
    {
        var cliente = Cliente();
        cliente.TipoComprobante = TipoComprobante.CreditoFiscal;
        cliente.TipoDocumento = TipoDocumento.Cedula;
        cliente.NumeroDocumento = "00112345678";

        Assert.False(cliente.ComprobanteEstaSustentado);
    }

    [Fact]
    public void El_credito_fiscal_con_rnc_si_se_sustenta()
    {
        var cliente = Cliente();
        cliente.TipoComprobante = TipoComprobante.CreditoFiscal;
        cliente.TipoDocumento = TipoDocumento.Rnc;
        cliente.NumeroDocumento = "131123456";

        Assert.True(cliente.ComprobanteEstaSustentado);
    }

    [Fact]
    public void El_comprobante_de_consumo_no_exige_documento()
    {
        var cliente = Cliente();
        cliente.TipoComprobante = TipoComprobante.Consumo;
        cliente.TipoDocumento = TipoDocumento.Ninguno;

        Assert.True(cliente.ComprobanteEstaSustentado);
    }

    [Fact]
    public void Los_codigos_del_enum_son_los_de_la_dgii()
    {
        // Forman parte del NCF, asi que reasignarlos romperia los comprobantes.
        Assert.Equal(1, (int)TipoComprobante.CreditoFiscal);
        Assert.Equal(2, (int)TipoComprobante.Consumo);
        Assert.Equal(14, (int)TipoComprobante.RegimenEspecial);
        Assert.Equal(15, (int)TipoComprobante.Gubernamental);
    }
}
