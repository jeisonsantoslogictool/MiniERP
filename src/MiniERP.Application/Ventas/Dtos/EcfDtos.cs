namespace MiniERP.Application.Ventas.Dtos;

/// <summary>
/// Datos fiscales del comercio que emite.
/// </summary>
/// <remarks>
/// Van en configuracion (seccion "Comercio" de appsettings) y no en la base de datos: son
/// datos del despliegue, no del negocio. El comercio es uno solo —el alcance excluye
/// multiples sucursales— y no cambian con la operacion, asi que una tabla con una fila
/// unica seria una entidad de mas y una migracion de mas.
/// </remarks>
public class DatosEmisor
{
    public const string SectionName = "Comercio";

    public string Rnc { get; set; } = string.Empty;

    public string RazonSocial { get; set; } = string.Empty;

    public string NombreComercial { get; set; } = string.Empty;

    public string Direccion { get; set; } = string.Empty;

    public string Telefono { get; set; } = string.Empty;

    /// <summary>Sin RNC ni razon social no hay emisor que poner en el comprobante.</summary>
    public bool EstaConfigurado =>
        !string.IsNullOrWhiteSpace(Rnc) && !string.IsNullOrWhiteSpace(RazonSocial);
}

/// <summary>
/// El comprobante electronico ya armado, listo para verlo o descargarlo.
/// </summary>
/// <param name="ENcf">Comprobante electronico derivado del NCF de la factura.</param>
/// <param name="CodigoSeguridad">Los seis caracteres que van en el QR. Simulado.</param>
/// <param name="Xml">El documento completo.</param>
/// <param name="UrlConsulta">Lo que codifica el QR: la consulta del timbre ante la DGII.</param>
/// <param name="FechaFirma">Instante que el documento declara como firmado.</param>
/// <param name="NombreArchivo">Nombre sugerido al descargar.</param>
public record EcfGeneradoDto(
    string ENcf,
    string CodigoSeguridad,
    string Xml,
    string UrlConsulta,
    DateTime FechaFirma,
    string NombreArchivo);
