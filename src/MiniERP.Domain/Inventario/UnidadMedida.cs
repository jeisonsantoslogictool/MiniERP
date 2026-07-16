using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Inventario;

/// <summary>
/// Unidad en que se compra y se vende un producto.
/// </summary>
/// <remarks>
/// La distincion que importa es <see cref="PermiteDecimales"/>: un minimarket vende
/// arroz por libra (2.5 LB es valido) y refrescos por unidad (2.5 UND no lo es).
/// De aqui salen las validaciones de cantidad en inventario, compras y punto de venta.
/// </remarks>
public class UnidadMedida : EntidadBase
{
    /// <summary>Codigo corto que ve el cajero: UND, LB, KG, GAL.</summary>
    public string Codigo { get; set; } = string.Empty;

    public string Nombre { get; set; } = string.Empty;

    /// <summary>Si es false, toda cantidad debe ser entera.</summary>
    public bool PermiteDecimales { get; set; }

    /// <summary>Decimales admitidos cuando <see cref="PermiteDecimales"/> es true.</summary>
    public int CantidadDecimales { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = [];

    /// <summary>
    /// Valida que la cantidad sea expresable en esta unidad. Rechaza tanto los
    /// decimales en unidades enteras como el exceso de precision en las de peso.
    /// </summary>
    public bool CantidadEsValida(decimal cantidad)
    {
        if (cantidad <= 0)
            return false;

        var decimalesPermitidos = PermiteDecimales ? CantidadDecimales : 0;

        return cantidad == Math.Round(cantidad, decimalesPermitidos);
    }
}
