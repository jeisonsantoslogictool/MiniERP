using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Rentabilidad de un periodo en tres cortes: total, por producto y por categoria.
/// Es un valor calculado desde los renglones vendidos; no se guarda.
/// </summary>
public record ResumenRentabilidad(
    RentabilidadTotal Total,
    IReadOnlyList<RentabilidadPorItem> PorProducto,
    IReadOnlyList<RentabilidadPorItem> PorCategoria)
{
    public static ResumenRentabilidad Calcular(IEnumerable<RenglonVendido> renglones)
    {
        ArgumentNullException.ThrowIfNull(renglones);

        // Una venta anulada no cuenta.
        var vendidos = renglones.Where(r => !r.FacturaAnulada).ToList();

        var porProducto = vendidos
            .GroupBy(r => (r.ProductoId, r.Producto))
            .Select(g => PorItem(g.Key.ProductoId, g.Key.Producto, g))
            .OrderByDescending(i => i.Margen)
            .ToList();

        var porCategoria = vendidos
            .GroupBy(r => (r.CategoriaId, r.Categoria))
            .Select(g => PorItem(g.Key.CategoriaId, g.Key.Categoria, g))
            .OrderByDescending(i => i.Margen)
            .ToList();

        return new ResumenRentabilidad(Totalizar(vendidos), porProducto, porCategoria);
    }

    private static RentabilidadTotal Totalizar(IEnumerable<RenglonVendido> renglones)
    {
        var subtotal = RetailConstants.RedondearImporte(renglones.Sum(r => r.Subtotal));
        var costo = RetailConstants.RedondearImporte(renglones.Sum(r => r.Costo));
        var margen = subtotal - costo;

        return new RentabilidadTotal(subtotal, costo, margen, Porcentaje(margen, subtotal));
    }

    private static RentabilidadPorItem PorItem(int id, string nombre, IEnumerable<RenglonVendido> renglones)
    {
        var subtotal = RetailConstants.RedondearImporte(renglones.Sum(r => r.Subtotal));
        var costo = RetailConstants.RedondearImporte(renglones.Sum(r => r.Costo));
        var margen = subtotal - costo;

        return new RentabilidadPorItem(id, nombre, subtotal, costo, margen, Porcentaje(margen, subtotal));
    }

    private static decimal Porcentaje(decimal margen, decimal subtotal) =>
        subtotal == 0 ? 0 : margen / subtotal;
}

public record RentabilidadTotal(decimal Subtotal, decimal Costo, decimal Margen, decimal MargenPorcentaje);

public record RentabilidadPorItem(int Id, string Nombre, decimal Subtotal, decimal Costo, decimal Margen, decimal MargenPorcentaje);

/// <summary>Un renglon vendido, tal como lo proyecta el repositorio desde LineaFactura.</summary>
public record RenglonVendido(
    bool FacturaAnulada,
    int ProductoId, string Producto,
    int CategoriaId, string Categoria,
    decimal Subtotal, decimal Costo);
