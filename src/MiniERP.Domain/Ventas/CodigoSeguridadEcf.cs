using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace MiniERP.Domain.Ventas;

/// <summary>
/// Codigo de seguridad que viaja en el QR del e-CF.
/// </summary>
/// <remarks>
/// En un e-CF real son los seis primeros caracteres de la firma digital del documento, y
/// es lo que permite a la DGII confirmar que el comprobante que se escanea es el que se
/// emitio. Producirlo de verdad exige el certificado de persona juridica que el alcance
/// excluye.
///
/// SIMULADO: aqui sale de un hash SHA-256 de los mismos campos que firmaria el certificado
/// —emisor, e-NCF, monto y fecha—. No prueba nada ante la DGII y no pretende hacerlo. Se
/// eligio determinista, y no aleatorio, por dos razones: el mismo comprobante da siempre el
/// mismo codigo, como haria una firma, y eso lo vuelve verificable en una prueba.
/// </remarks>
public static class CodigoSeguridadEcf
{
    /// <summary>La DGII toma seis caracteres de la firma.</summary>
    public const int Largo = 6;

    /// <summary>
    /// Calcula el codigo simulado de un comprobante.
    /// </summary>
    public static string Calcular(string rncEmisor, string eNcf, decimal montoTotal, DateTime fechaEmision)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rncEmisor);
        ArgumentException.ThrowIfNullOrWhiteSpace(eNcf);

        var semilla = string.Format(
            CultureInfo.InvariantCulture,
            "{0}|{1}|{2:F2}|{3:yyyy-MM-dd}",
            rncEmisor.Trim(),
            eNcf.Trim(),
            montoTotal,
            fechaEmision);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(semilla));

        // Se descartan '+', '/' y '=' del base64: el codigo viaja como parametro de la URL
        // del QR, y esos tres se escapan y ensucian el codigo que el cajero ve impreso.
        var alfanumerico = new string([.. Convert.ToBase64String(hash).Where(char.IsAsciiLetterOrDigit)]);

        return alfanumerico[..Largo].ToUpperInvariant();
    }
}
