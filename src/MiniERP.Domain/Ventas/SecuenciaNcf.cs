using MiniERP.Domain.Clientes;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Ventas;

/// <summary>
/// Rango de comprobantes fiscales autorizado por la DGII para un tipo de comprobante.
/// </summary>
/// <remarks>
/// La DGII no entrega comprobantes de uno en uno: autoriza un rango (del 1 al 5000, por
/// ejemplo) con fecha de vencimiento, y el contribuyente los consume en orden. De ahi que
/// esto sea un rango con un contador y no una tabla de numeros sueltos.
///
/// El numero NO se arma aqui adentro incrementando Actual. Dos cajeros facturando a la
/// vez leerian el mismo Actual y emitirian el mismo NCF, que es una infraccion fiscal, no
/// un bug cosmetico. La asignacion se hace con un UPDATE atomico en la base; esta clase
/// solo sabe formatear y responder si el rango sirve.
/// </remarks>
public class SecuenciaNcf : EntidadBase
{
    public TipoComprobante TipoComprobante { get; set; }

    /// <summary>
    /// Prefijo completo: B02 para el NCF en papel, E32 para el electronico. Se guarda
    /// entero en vez de derivarlo del tipo porque los codigos de e-CF no coinciden con
    /// los del NCF tradicional: un consumo es B02 en papel y E32 en electronico.
    /// </summary>
    public string Prefijo { get; set; } = string.Empty;

    public long Desde { get; set; }

    public long Hasta { get; set; }

    /// <summary>Ultimo numero consumido. En Desde-1 significa que el rango esta sin estrenar.</summary>
    public long Actual { get; set; }

    public DateTime FechaVencimiento { get; set; }

    public bool Activa { get; set; } = true;

    /// <summary>Cuantos comprobantes quedan antes de tener que pedir otro rango a la DGII.</summary>
    public long Disponibles => Math.Max(0, Hasta - Actual);

    public bool Agotada => Actual >= Hasta;

    /// <summary>
    /// Un rango vencido no se puede usar aunque le queden numeros: la autorizacion
    /// de la DGII caduco.
    /// </summary>
    public bool EstaVencida(DateTime hoy) => FechaVencimiento.Date < hoy.Date;

    public bool PuedeEmitir(DateTime hoy) => Activa && !Agotada && !EstaVencida(hoy);

    /// <summary>
    /// Umbral de aviso: con menos de esto, hay que ir pidiendo el proximo rango.
    /// Quedarse sin comprobantes a media manana cierra la caja.
    /// </summary>
    public const long UmbralAviso = 50;

    public bool PorAgotarse => Disponibles is > 0 and <= UmbralAviso;

    /// <summary>
    /// Cuantos digitos lleva el correlativo. El NCF en papel usa ocho y el electronico
    /// diez, asi que el largo sale del prefijo y no de una constante.
    /// </summary>
    public int LargoCorrelativo => EsElectronica ? 10 : 8;

    /// <summary>El e-CF de la Ley 32-23 se distingue por empezar con E.</summary>
    public bool EsElectronica => Prefijo.StartsWith('E');

    /// <summary>
    /// Arma el comprobante completo a partir de un numero ya asignado.
    /// </summary>
    /// <example>Prefijo B02 y numero 123 dan B0200000123.</example>
    public string Formatear(long numero) =>
        Prefijo + numero.ToString(new string('0', LargoCorrelativo));

    /// <summary>
    /// Explica por que este rango no sirve, para poder decirselo al cajero.
    /// </summary>
    public string? MotivoNoDisponible(DateTime hoy)
    {
        if (!Activa)
            return $"La secuencia {Prefijo} esta desactivada.";

        if (Agotada)
            return $"La secuencia {Prefijo} se agoto en el {Formatear(Hasta)}. " +
                   "Solicita un nuevo rango a la DGII.";

        if (EstaVencida(hoy))
            return $"La secuencia {Prefijo} vencio el {FechaVencimiento:dd/MM/yyyy}. " +
                   "Solicita un nuevo rango a la DGII.";

        return null;
    }
}
