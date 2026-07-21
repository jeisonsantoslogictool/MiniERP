using QRCoder;

namespace MiniERP.Web;

/// <summary>
/// Genera el codigo QR de un comprobante como imagen embebida en la propia pagina.
/// </summary>
/// <remarks>
/// Se devuelve como data URI y no como archivo servido por una ruta: el QR pertenece al
/// documento, no al sitio, asi que embebido viaja dentro de la pagina impresa y del PDF
/// sin depender de una peticion mas ni de que el servidor siga en pie.
///
/// PngByteQRCode no depende de System.Drawing, que no existe fuera de Windows.
/// </remarks>
public static class QrCode
{
    /// <summary>
    /// Tamano de cada modulo del QR en pixeles. Seis da un codigo legible por telefono
    /// tanto en pantalla como impreso en papel termico.
    /// </summary>
    private const int PixelesPorModulo = 6;

    public static string ComoDataUri(string contenido)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contenido);

        using var generador = new QRCodeGenerator();

        // Correccion media: el QR sobrevive a un papel manchado o mal cortado, que en una
        // caja de minimarket pasa constantemente.
        using var datos = generador.CreateQrCode(contenido, QRCodeGenerator.ECCLevel.M);

        var png = new PngByteQRCode(datos).GetGraphic(PixelesPorModulo);

        return "data:image/png;base64," + Convert.ToBase64String(png);
    }
}
