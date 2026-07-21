using System.Globalization;
using MiniERP.Domain.Clientes;

namespace MiniERP.Domain.Ventas;

/// <summary>
/// Reglas del comprobante fiscal electronico (e-CF) de la Ley 32-23.
/// </summary>
/// <remarks>
/// Los codigos del e-CF NO son los del NCF en papel: un consumo es B02 impreso y E32
/// electronico. Por eso el prefijo se mapea aqui y no se deriva del valor del enum, que
/// lleva el codigo del comprobante tradicional.
///
/// SIMULACION: el sistema no esta certificado ante la DGII —eso exige registrar la empresa
/// y un certificado digital de persona juridica, tramite externo y fuera del alcance—, asi
/// que el e-NCF se DERIVA del NCF ya emitido en vez de consumir un rango electronico
/// autorizado. Es determinista a proposito: la misma factura produce siempre el mismo e-CF,
/// que es lo que lo hace verificable y probable. Ver CLAUDE.md, "El e-CF se simula".
///
/// Cuando el comercio se certifique, esto cambia en un solo punto: el e-NCF pasa a salir de
/// una SecuenciaNcf con prefijo E —que la clase ya soporta, con correlativo de diez— y se
/// guarda en la factura para no recalcularlo.
/// </remarks>
public static class ComprobanteElectronico
{
    /// <summary>El correlativo del e-CF lleva diez digitos; el del NCF en papel, ocho.</summary>
    public const int LargoCorrelativo = 10;

    /// <summary>Los tres primeros caracteres de un NCF son el prefijo (letra + tipo).</summary>
    private const int LargoPrefijo = 3;

    /// <summary>
    /// Prefijo electronico que corresponde al tipo de comprobante.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">El tipo no tiene equivalente electronico.</exception>
    public static string PrefijoDe(TipoComprobante tipo) => tipo switch
    {
        TipoComprobante.CreditoFiscal => "E31",
        TipoComprobante.Consumo => "E32",
        TipoComprobante.RegimenEspecial => "E44",
        TipoComprobante.Gubernamental => "E45",
        _ => throw new ArgumentOutOfRangeException(
            nameof(tipo), tipo, "El tipo de comprobante no tiene equivalente electronico.")
    };

    /// <summary>
    /// Codigo de dos digitos del tipo de e-CF, tal como va dentro del XML.
    /// </summary>
    /// <example>Consumo da 32.</example>
    public static string CodigoTipoDe(TipoComprobante tipo) => PrefijoDe(tipo)[1..];

    /// <summary>
    /// Deriva el e-NCF del NCF ya emitido, conservando el correlativo.
    /// </summary>
    /// <example>B0200000123 de consumo da E320000000123.</example>
    /// <exception cref="InvalidOperationException">El NCF no trae un correlativo legible.</exception>
    public static string DerivarENcf(string ncf, TipoComprobante tipo)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ncf);

        var limpio = ncf.Trim().ToUpperInvariant();

        // El correlativo es todo lo que sigue al prefijo. Filtrar "los digitos del NCF"
        // seria un error: el 02 de B02 tambien es digito y se colaria en el numero.
        if (limpio.Length <= LargoPrefijo)
            throw new InvalidOperationException($"El NCF '{ncf}' no tiene correlativo.");

        var correlativo = limpio[LargoPrefijo..];

        if (!correlativo.All(char.IsAsciiDigit))
            throw new InvalidOperationException($"El correlativo del NCF '{ncf}' no es numerico.");

        var numero = long.Parse(correlativo, CultureInfo.InvariantCulture);

        return PrefijoDe(tipo) + numero.ToString(new string('0', LargoCorrelativo), CultureInfo.InvariantCulture);
    }
}
