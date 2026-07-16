namespace MiniERP.Domain.Shared;

/// <summary>
/// Precisiones del sistema.
/// </summary>
/// <remarks>
/// El redondeo se aplica al totalizar, nunca al almacenar. Precios y costos llevan
/// cuatro decimales para que un producto a 33.3333 la libra no pierda precision; los
/// importes que el cliente paga se redondean a dos, que es lo que existe en efectivo.
///
/// Se redondea AwayFromZero y no ToEven, que es el comportamiento por defecto de .NET:
/// un cliente no entiende por que 2.5 a veces da 2 y a veces da 3, y la DGII espera
/// el redondeo aritmetico de toda la vida.
/// </remarks>
public static class RetailConstants
{
    /// <summary>Decimales de precios y costos al almacenar.</summary>
    public const int DecimalesPrecio = 4;

    /// <summary>Decimales de los importes de un documento.</summary>
    public const int DecimalesImporte = 2;

    /// <summary>Decimales de cantidad para lo que se pesa.</summary>
    public const int DecimalesCantidad = 3;

    /// <summary>Redondeo aritmetico: 2.5 siempre da 3.</summary>
    public const MidpointRounding ModoRedondeo = MidpointRounding.AwayFromZero;

    /// <summary>Redondea un importe a los decimales que existen en efectivo.</summary>
    public static decimal RedondearImporte(decimal valor) =>
        Math.Round(valor, DecimalesImporte, ModoRedondeo);
}
