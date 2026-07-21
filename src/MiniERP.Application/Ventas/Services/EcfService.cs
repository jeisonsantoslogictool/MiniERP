using System.Globalization;
using System.Xml.Linq;
using MiniERP.Application.Common;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Application.Ventas.Services;

public interface IEcfService
{
    /// <summary>Arma el comprobante electronico de una factura ya emitida.</summary>
    Task<Resultado<EcfGeneradoDto>> GenerarAsync(int facturaId, CancellationToken ct = default);
}

/// <summary>
/// Genera el XML del comprobante fiscal electronico y la URL que codifica su QR.
/// </summary>
/// <remarks>
/// SIMULADO, y con un limite explicito: la estructura del XML, el e-NCF y el QR son los
/// reales —el esquema de la DGII es publico y no hace falta estar registrado para armarlo
/// bien—; lo que se simula es la firma digital, el codigo de seguridad que sale de ella y
/// el envio. Emitir de verdad exige registrar la empresa ante la DGII y un certificado de
/// persona juridica: tramite externo, del comercio y no del grupo, excluido del alcance por
/// escrito. Ver CLAUDE.md, "El e-CF se simula".
///
/// Todo documento que sale de aqui lleva la advertencia dentro del XML, no solo en pantalla:
/// el archivo se descarga y viaja solo, y el piloto es un comercio real facturando.
///
/// El documento es reproducible: la fecha de firma es la de la factura y no la del momento
/// de generarlo, asi que pedir dos veces el e-CF de la misma factura da el mismo XML. Una
/// firma real tendria esa misma propiedad, y sin ella no se podria probar nada.
/// </remarks>
public class EcfService(IVentaService ventas, DatosEmisor emisor) : IEcfService
{
    /// <summary>Version del formato de e-CF que se declara en el documento.</summary>
    private const string VersionEcf = "1.0";

    /// <summary>01: ingresos por operaciones del giro del negocio.</summary>
    private const string TipoIngresos = "01";

    /// <summary>1: los precios del detalle van sin ITBIS incluido, que es como los guarda la factura.</summary>
    private const string PreciosSinItbisIncluido = "0";

    /// <summary>1: bien. Un minimarket no factura servicios.</summary>
    private const string IndicadorBien = "1";

    /// <summary>1: gravado con la tasa general. 4: exento.</summary>
    private const string FacturacionGravada = "1";
    private const string FacturacionExenta = "4";

    private const string Advertencia = "DOCUMENTO SIMULADO - NO VALIDO ANTE LA DGII";

    public async Task<Resultado<EcfGeneradoDto>> GenerarAsync(
        int facturaId, CancellationToken ct = default)
    {
        if (!emisor.EstaConfigurado)
            return Resultado.Falla<EcfGeneradoDto>(
                "Faltan los datos fiscales del comercio. Complete la seccion \"Comercio\" " +
                "(RNC y razon social) en la configuracion antes de generar comprobantes.");

        var factura = await ventas.ObtenerDetalleAsync(facturaId, ct);

        if (factura is null)
            return Resultado.Falla<EcfGeneradoDto>("La factura no existe.");

        if (string.IsNullOrWhiteSpace(factura.Ncf))
            return Resultado.Falla<EcfGeneradoDto>(
                "La factura no tiene comprobante asignado, asi que no hay e-CF que generar.");

        if (factura.Estado == EstadoFactura.Anulada)
            return Resultado.Falla<EcfGeneradoDto>(
                "Una factura anulada no genera e-CF. Ante la DGII, un comprobante ya enviado " +
                "se retira con una anulacion de e-NCF, no volviendo a emitirlo.");

        var eNcf = ComprobanteElectronico.DerivarENcf(factura.Ncf, factura.TipoComprobante);

        var codigo = CodigoSeguridadEcf.Calcular(
            emisor.Rnc, eNcf, factura.Total, factura.Fecha);

        var xml = ConstruirXml(factura, eNcf, codigo);
        var url = ConstruirUrlConsulta(factura, eNcf, codigo);

        return Resultado.Ok(new EcfGeneradoDto(
            ENcf: eNcf,
            CodigoSeguridad: codigo,
            Xml: xml,
            UrlConsulta: url,
            FechaFirma: factura.Fecha,
            NombreArchivo: $"{emisor.Rnc}{eNcf}.xml"));
    }

    /// <summary>
    /// Arma el XML segun la estructura del e-CF, con la firma sustituida por la advertencia.
    /// </summary>
    private string ConstruirXml(FacturaDetalleDto factura, string eNcf, string codigo)
    {
        var gravado = factura.Lineas.Where(l => l.TasaItbis > 0).Sum(l => l.Subtotal);
        var exento = factura.Lineas.Where(l => l.TasaItbis == 0).Sum(l => l.Subtotal);
        // Si nada va gravado, la tasa declarada es cero: no se inventa un 18 que el
        // documento no cobra.
        var tasa = factura.Lineas.FirstOrDefault(l => l.TasaItbis > 0)?.TasaItbis ?? 0m;

        var encabezado = new XElement("Encabezado",
            new XElement("Version", VersionEcf),
            new XElement("IdDoc",
                new XElement("TipoeCF", ComprobanteElectronico.CodigoTipoDe(factura.TipoComprobante)),
                new XElement("eNCF", eNcf),
                new XElement("IndicadorMontoGravado", PreciosSinItbisIncluido),
                new XElement("TipoIngresos", TipoIngresos),
                new XElement("TipoPago", factura.Condicion == CondicionPago.Contado ? "1" : "2")),
            new XElement("Emisor",
                new XElement("RNCEmisor", emisor.Rnc),
                new XElement("RazonSocialEmisor", emisor.RazonSocial),
                new XElement("NombreComercial", emisor.NombreComercial),
                new XElement("DireccionEmisor", emisor.Direccion),
                new XElement("FechaEmision", Fecha(factura.Fecha))),
            new XElement("Comprador",
                new XElement("RNCComprador", factura.ClienteDocumento ?? string.Empty),
                new XElement("RazonSocialComprador", factura.ClienteNombre)),
            new XElement("Totales",
                new XElement("MontoGravadoTotal", Monto(gravado)),
                new XElement("MontoGravadoI1", Monto(gravado)),
                new XElement("MontoExento", Monto(exento)),
                new XElement("ITBIS1", Porcentaje(tasa)),
                new XElement("TotalITBIS", Monto(factura.Itbis)),
                new XElement("TotalITBIS1", Monto(factura.Itbis)),
                new XElement("MontoTotal", Monto(factura.Total))));

        var detalle = new XElement("DetallesItems",
            factura.Lineas.Select((linea, indice) => new XElement("Item",
                new XElement("NumeroLinea", indice + 1),
                new XElement("IndicadorFacturacion",
                    linea.TasaItbis > 0 ? FacturacionGravada : FacturacionExenta),
                new XElement("NombreItem", linea.Descripcion),
                new XElement("IndicadorBienoServicio", IndicadorBien),
                new XElement("CantidadItem", Cantidad(linea.Cantidad)),
                new XElement("UnidadMedida", linea.UnidadMedida),
                new XElement("PrecioUnitarioItem", Monto(linea.PrecioUnitario)),
                new XElement("MontoItem", Monto(linea.Subtotal)))));

        // Donde ira la firma digital cuando el comercio se certifique. Se deja nombrado
        // para que el hueco sea evidente y nadie lo confunda con un documento firmado.
        var simulacion = new XElement("_SimulacionSinFirma",
            new XElement("Advertencia", Advertencia),
            new XElement("Motivo",
                "Firmar un e-CF exige registrar la empresa ante la DGII y un certificado " +
                "digital de persona juridica. Fuera del alcance del proyecto."),
            new XElement("CodigoSeguridadSimulado", codigo));

        var documento = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XComment($" {Advertencia} "),
            new XElement("ECF",
                encabezado,
                detalle,
                new XElement("FechaHoraFirma", FechaHora(factura.Fecha)),
                simulacion));

        return documento.Declaration + Environment.NewLine + documento.ToString();
    }

    /// <summary>
    /// Arma la URL que codifica el QR: la consulta del timbre en el portal de la DGII.
    /// </summary>
    /// <remarks>
    /// El consumo electronico (E32) se consulta por una ruta propia y sin RNC del comprador,
    /// porque el cliente de mostrador no se identifica. Se respeta esa diferencia para que
    /// el QR tenga la forma real y no una inventada.
    /// </remarks>
    private string ConstruirUrlConsulta(FacturaDetalleDto factura, string eNcf, string codigo)
    {
        var esConsumo = factura.TipoComprobante == TipoComprobante.Consumo;

        var ruta = esConsumo
            ? "https://ecf.dgii.gov.do/ecf/consultatimbrefc"
            : "https://ecf.dgii.gov.do/ecf/consultatimbre";

        var parametros = new List<string> { $"RncEmisor={Escapar(emisor.Rnc)}" };

        if (!esConsumo)
            parametros.Add($"RncComprador={Escapar(factura.ClienteDocumento ?? string.Empty)}");

        parametros.Add($"ENCF={Escapar(eNcf)}");
        parametros.Add($"FechaEmision={Escapar(Fecha(factura.Fecha))}");
        parametros.Add($"MontoTotal={Escapar(Monto(factura.Total))}");
        parametros.Add($"FechaFirma={Escapar(FechaHora(factura.Fecha))}");
        parametros.Add($"CodigoSeguridad={Escapar(codigo)}");

        return $"{ruta}?{string.Join('&', parametros)}";
    }

    private static string Escapar(string valor) => Uri.EscapeDataString(valor);

    private static string Monto(decimal valor) => valor.ToString("F2", CultureInfo.InvariantCulture);

    private static string Cantidad(decimal valor) => valor.ToString("0.####", CultureInfo.InvariantCulture);

    /// <summary>La tasa se guarda como fraccion (0.18) y el XML la pide en por ciento (18).</summary>
    private static string Porcentaje(decimal tasa) =>
        (tasa * 100).ToString("0.##", CultureInfo.InvariantCulture);

    private static string Fecha(DateTime valor) => valor.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);

    private static string FechaHora(DateTime valor) =>
        valor.ToString("dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture);
}
