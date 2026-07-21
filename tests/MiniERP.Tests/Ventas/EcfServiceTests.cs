using System.Xml.Linq;
using MiniERP.Application.Common;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Application.Ventas.Services;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Ventas;

/// <summary>
/// El XML del comprobante electronico y la URL de su QR.
/// </summary>
/// <remarks>
/// Lo que se simula es la firma, no el documento: la estructura, los totales y el e-NCF se
/// prueban con el mismo rigor que si el comercio estuviera certificado, porque el dia que
/// lo este es lo unico que no tendra que cambiar.
/// </remarks>
public class EcfServiceTests
{
    private static readonly DateTime Emision = new(2026, 7, 21, 14, 30, 0);

    private static DatosEmisor Emisor() => new()
    {
        Rnc = "130123456",
        RazonSocial = "MINIMARKET DEMO, SRL",
        NombreComercial = "Minimarket Demo",
        Direccion = "Moca, Espaillat",
        Telefono = "809-000-0000"
    };

    /// <summary>
    /// Tres libras de arroz gravadas y dos panes exentos: cubre las dos ramas del ITBIS
    /// en un solo documento, que es como salen las facturas de un minimarket.
    /// </summary>
    private static FacturaDetalleDto Factura(
        string? ncf = "B0200000123",
        TipoComprobante tipo = TipoComprobante.Consumo,
        EstadoFactura estado = EstadoFactura.Emitida,
        CondicionPago condicion = CondicionPago.Contado,
        string? documentoCliente = "40200112233") => new(
            Id: 7,
            Numero: "F-000007",
            Ncf: ncf,
            TipoComprobante: tipo,
            ClienteNombre: "Juan Perez",
            ClienteDocumento: documentoCliente,
            Fecha: Emision,
            Estado: estado,
            Condicion: condicion,
            Subtotal: 164.00m,
            Itbis: 20.52m,
            Total: 184.52m,
            MontoRecibido: 200.00m,
            Cambio: 15.48m,
            UsuarioId: "cajero@minierp.local",
            MotivoAnulacion: null,
            Lineas:
            [
                new LineaFacturaDetalleDto(
                    "ARR-LB", "Arroz selecto", "LB", true, 3m, 38.00m, 0.18m, 114.00m, 20.52m, 134.52m),
                new LineaFacturaDetalleDto(
                    "PAN-UN", "Pan de agua", "UN", false, 2m, 25.00m, 0m, 50.00m, 0m, 50.00m)
            ]);

    private static EcfService Servicio(FacturaDetalleDto? factura, DatosEmisor? emisor = null) =>
        new(new VentaServiceFalso(factura), emisor ?? Emisor());

    private static async Task<EcfGeneradoDto> GenerarAsync(
        FacturaDetalleDto? factura = null, DatosEmisor? emisor = null)
    {
        var resultado = await Servicio(factura ?? Factura(), emisor).GenerarAsync(7);

        Assert.True(resultado.Exito, resultado.Error);

        return resultado.Valor!;
    }

    private static async Task<XElement> XmlAsync(FacturaDetalleDto? factura = null) =>
        XDocument.Parse((await GenerarAsync(factura)).Xml).Root!;

    // ---- Identificacion ----

    [Fact]
    public async Task El_documento_lleva_el_encf_derivado_del_ncf()
    {
        var ecf = await GenerarAsync();

        Assert.Equal("E320000000123", ecf.ENcf);
        Assert.Equal("E320000000123", (await XmlAsync()).Element("Encabezado")!
            .Element("IdDoc")!.Element("eNCF")!.Value);
    }

    [Fact]
    public async Task El_consumo_se_declara_como_tipo_32() =>
        Assert.Equal("32", (await XmlAsync()).Element("Encabezado")!
            .Element("IdDoc")!.Element("TipoeCF")!.Value);

    [Fact]
    public async Task El_contado_y_el_credito_se_distinguen_en_el_tipo_de_pago()
    {
        var contado = await XmlAsync(Factura(condicion: CondicionPago.Contado));
        var credito = await XmlAsync(Factura(condicion: CondicionPago.Credito));

        Assert.Equal("1", contado.Element("Encabezado")!.Element("IdDoc")!.Element("TipoPago")!.Value);
        Assert.Equal("2", credito.Element("Encabezado")!.Element("IdDoc")!.Element("TipoPago")!.Value);
    }

    [Fact]
    public async Task El_emisor_sale_de_la_configuracion_del_comercio()
    {
        var emisor = (await XmlAsync()).Element("Encabezado")!.Element("Emisor")!;

        Assert.Equal("130123456", emisor.Element("RNCEmisor")!.Value);
        Assert.Equal("MINIMARKET DEMO, SRL", emisor.Element("RazonSocialEmisor")!.Value);
        Assert.Equal("21-07-2026", emisor.Element("FechaEmision")!.Value);
    }

    // ---- Totales ----

    [Fact]
    public async Task Los_totales_del_xml_cuadran_con_la_factura()
    {
        var totales = (await XmlAsync()).Element("Encabezado")!.Element("Totales")!;

        Assert.Equal("114.00", totales.Element("MontoGravadoTotal")!.Value);
        Assert.Equal("50.00", totales.Element("MontoExento")!.Value);
        Assert.Equal("20.52", totales.Element("TotalITBIS")!.Value);
        Assert.Equal("184.52", totales.Element("MontoTotal")!.Value);
    }

    [Fact]
    public async Task La_tasa_va_en_por_ciento_y_no_en_fraccion() =>
        Assert.Equal("18", (await XmlAsync()).Element("Encabezado")!
            .Element("Totales")!.Element("ITBIS1")!.Value);

    [Fact]
    public async Task Lo_gravado_y_lo_exento_no_se_mezclan()
    {
        var items = (await XmlAsync()).Element("DetallesItems")!.Elements("Item").ToList();

        Assert.Equal("1", items[0].Element("IndicadorFacturacion")!.Value);  // arroz: gravado
        Assert.Equal("4", items[1].Element("IndicadorFacturacion")!.Value);  // pan: exento
    }

    [Fact]
    public async Task Cada_linea_de_la_factura_es_un_item_numerado()
    {
        var items = (await XmlAsync()).Element("DetallesItems")!.Elements("Item").ToList();

        Assert.Equal(2, items.Count);
        Assert.Equal("1", items[0].Element("NumeroLinea")!.Value);
        Assert.Equal("Arroz selecto", items[0].Element("NombreItem")!.Value);
        Assert.Equal("3", items[0].Element("CantidadItem")!.Value);
        Assert.Equal("38.00", items[0].Element("PrecioUnitarioItem")!.Value);
    }

    // ---- La simulacion, declarada ----

    [Fact]
    public async Task El_documento_se_declara_simulado_dentro_del_xml()
    {
        var ecf = await GenerarAsync();
        var raiz = XDocument.Parse(ecf.Xml).Root!;

        // Dentro del archivo y no solo en la pantalla: el XML se descarga y viaja solo.
        Assert.Contains("DOCUMENTO SIMULADO", ecf.Xml, StringComparison.Ordinal);
        Assert.NotNull(raiz.Element("_SimulacionSinFirma"));
    }

    [Fact]
    public async Task Donde_iria_la_firma_no_hay_firma()
    {
        var raiz = await XmlAsync();

        Assert.Null(raiz.Element("Signature"));
        Assert.Null(raiz.Element("_Signature"));
    }

    [Fact]
    public async Task El_mismo_comprobante_se_regenera_identico()
    {
        // La fecha de firma es la de la factura y no la del momento de generarlo. Sin esto
        // el XML cambiaria en cada consulta y no habria nada que comparar.
        var primero = await GenerarAsync();
        var segundo = await GenerarAsync();

        Assert.Equal(primero.Xml, segundo.Xml);
        Assert.Equal(primero.CodigoSeguridad, segundo.CodigoSeguridad);
    }

    // ---- El QR ----

    [Fact]
    public async Task El_qr_del_consumo_usa_la_consulta_de_consumo_y_omite_al_comprador()
    {
        // El cliente de mostrador no se identifica, y la DGII lo consulta por otra ruta.
        var ecf = await GenerarAsync(Factura(tipo: TipoComprobante.Consumo));

        Assert.Contains("consultatimbrefc", ecf.UrlConsulta, StringComparison.Ordinal);
        Assert.DoesNotContain("RncComprador", ecf.UrlConsulta, StringComparison.Ordinal);
    }

    [Fact]
    public async Task El_qr_del_credito_fiscal_si_identifica_al_comprador()
    {
        var ecf = await GenerarAsync(Factura(ncf: "B0100000123", tipo: TipoComprobante.CreditoFiscal));

        Assert.Contains("consultatimbre?", ecf.UrlConsulta, StringComparison.Ordinal);
        Assert.Contains("RncComprador=40200112233", ecf.UrlConsulta, StringComparison.Ordinal);
        Assert.Contains("ENCF=E310000000123", ecf.UrlConsulta, StringComparison.Ordinal);
    }

    [Fact]
    public async Task El_qr_lleva_el_monto_y_el_codigo_de_seguridad()
    {
        var ecf = await GenerarAsync();

        Assert.Contains("MontoTotal=184.52", ecf.UrlConsulta, StringComparison.Ordinal);
        Assert.Contains($"CodigoSeguridad={ecf.CodigoSeguridad}", ecf.UrlConsulta, StringComparison.Ordinal);
    }

    // ---- Lo que no genera comprobante ----

    [Fact]
    public async Task Una_factura_anulada_no_genera_ecf()
    {
        var resultado = await Servicio(Factura(estado: EstadoFactura.Anulada)).GenerarAsync(7);

        Assert.True(resultado.Fallo);
        Assert.Contains("anulada", resultado.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Una_factura_sin_ncf_no_genera_ecf()
    {
        var resultado = await Servicio(Factura(ncf: null)).GenerarAsync(7);

        Assert.True(resultado.Fallo);
        Assert.Contains("comprobante asignado", resultado.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Una_factura_que_no_existe_no_genera_ecf()
    {
        var resultado = await Servicio(factura: null).GenerarAsync(7);

        Assert.True(resultado.Fallo);
        Assert.Contains("no existe", resultado.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Sin_los_datos_fiscales_del_comercio_no_se_emite()
    {
        // Un e-CF sin RNC del emisor no identifica a nadie. Mejor negarse que producir
        // un documento con el campo en blanco.
        var resultado = await Servicio(Factura(), new DatosEmisor()).GenerarAsync(7);

        Assert.True(resultado.Fallo);
        Assert.Contains("datos fiscales", resultado.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Doble del servicio de ventas: solo sabe devolver la factura del caso.</summary>
    private sealed class VentaServiceFalso(FacturaDetalleDto? factura) : IVentaService
    {
        public Task<FacturaDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(factura);

        public Task<IReadOnlyList<ProductoParaVentaDto>> BuscarProductosAsync(
            string texto, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<ProductoParaVentaDto?> ObtenerPorCodigoBarrasAsync(
            string codigoBarras, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<ClienteParaVentaDto>> BuscarClientesAsync(
            string texto, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<PaginaDe<FacturaListaDto>> BuscarAsync(
            FiltroFacturas filtro, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<Resultado<FacturaEmitidaDto>> EmitirAsync(
            VentaFormDto form, string? usuarioId, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Resultado> AnularAsync(
            int facturaId, string motivo, string? usuarioId, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
