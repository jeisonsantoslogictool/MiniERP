using System.Globalization;
using MiniERP.Domain.Inventario;

namespace MiniERP.Web;

/// <summary>
/// Formateo para las pantallas.
/// </summary>
public static class Formato
{
    /// <summary>
    /// Muestra la cantidad con los decimales que corresponden a su unidad: tres para
    /// lo que se pesa, ninguno para lo que se cuenta. Un "2.000" en refrescos confunde.
    /// </summary>
    public static string Cantidad(decimal valor, bool permiteDecimales) =>
        valor.ToString(permiteDecimales ? "N3" : "N0", CultureInfo.InvariantCulture);

    /// <summary>Importe en pesos, redondeado al mostrar y no al calcular.</summary>
    public static string Dinero(decimal valor) =>
        "RD$ " + valor.ToString("N2", CultureInfo.InvariantCulture);

    public static string Porcentaje(decimal fraccion) =>
        (fraccion * 100).ToString("N1", CultureInfo.InvariantCulture) + " %";

    /// <summary>Fecha en la hora local de la República Dominicana, que es UTC-4 y sin horario de verano.</summary>
    public static string FechaHora(DateTime utc) =>
        utc.AddHours(-4).ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    public static string Movimiento(TipoMovimiento tipo) => tipo switch
    {
        TipoMovimiento.Entrada => "Entrada",
        TipoMovimiento.Salida => "Salida",
        TipoMovimiento.AjustePositivo => "Ajuste (+)",
        TipoMovimiento.AjusteNegativo => "Ajuste (−)",
        TipoMovimiento.Merma => "Merma",
        TipoMovimiento.DevolucionCliente => "Devolución de cliente",
        TipoMovimiento.DevolucionProveedor => "Devolución a proveedor",
        _ => tipo.ToString()
    };
}
