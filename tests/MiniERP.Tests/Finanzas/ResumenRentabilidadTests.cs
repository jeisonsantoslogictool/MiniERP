using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

public class ResumenRentabilidadTests
{
    private static RenglonVendido Renglon(
        decimal subtotal, decimal costo,
        int productoId = 1, string producto = "Arroz",
        int categoriaId = 1, string categoria = "Viveres",
        bool anulada = false) =>
        new(anulada, productoId, producto, categoriaId, categoria, subtotal, costo);

    [Fact]
    public void Un_periodo_vacio_da_ceros()
    {
        var r = ResumenRentabilidad.Calcular([]);

        Assert.Equal(0m, r.Total.Subtotal);
        Assert.Equal(0m, r.Total.Margen);
        Assert.Empty(r.PorProducto);
        Assert.Empty(r.PorCategoria);
    }

    [Fact]
    public void El_margen_es_subtotal_menos_costo()
    {
        var r = ResumenRentabilidad.Calcular([Renglon(subtotal: 100, costo: 60)]);

        Assert.Equal(100m, r.Total.Subtotal);
        Assert.Equal(60m, r.Total.Costo);
        Assert.Equal(40m, r.Total.Margen);
    }

    [Fact]
    public void El_margen_porcentaje_es_sobre_el_subtotal()
    {
        var r = ResumenRentabilidad.Calcular([Renglon(100, 60)]);

        Assert.Equal(0.40m, r.Total.MargenPorcentaje);
    }

    [Fact]
    public void Agrupa_por_producto()
    {
        var renglones = new[]
        {
            Renglon(100, 60, productoId: 1, producto: "Arroz"),
            Renglon(50, 30, productoId: 1, producto: "Arroz"),
            Renglon(200, 100, productoId: 2, producto: "Aceite"),
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal(2, r.PorProducto.Count);
        var arroz = r.PorProducto.Single(p => p.Id == 1);
        Assert.Equal(150m, arroz.Subtotal);
        Assert.Equal(60m, arroz.Margen); // (100-60) + (50-30)
    }

    [Fact]
    public void Agrupa_por_categoria()
    {
        var renglones = new[]
        {
            Renglon(100, 60, productoId: 1, categoriaId: 1, categoria: "Viveres"),
            Renglon(200, 100, productoId: 2, categoriaId: 1, categoria: "Viveres"),
            Renglon(50, 20, productoId: 3, categoriaId: 2, categoria: "Bebidas"),
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal(2, r.PorCategoria.Count);
        var viveres = r.PorCategoria.Single(c => c.Id == 1);
        Assert.Equal(300m, viveres.Subtotal);
        Assert.Equal(140m, viveres.Margen); // (100-60) + (200-100)
    }

    [Fact]
    public void Ordena_por_margen_descendente()
    {
        var renglones = new[]
        {
            Renglon(50, 40, productoId: 1, producto: "Poco"),    // margen 10
            Renglon(200, 50, productoId: 2, producto: "Mucho"),  // margen 150
            Renglon(100, 70, productoId: 3, producto: "Medio"),  // margen 30
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal("Mucho", r.PorProducto[0].Nombre);
        Assert.Equal("Medio", r.PorProducto[1].Nombre);
        Assert.Equal("Poco", r.PorProducto[2].Nombre);
    }

    [Fact]
    public void El_total_suma_todos_los_renglones()
    {
        var renglones = new[]
        {
            Renglon(100, 60, productoId: 1, categoriaId: 1),
            Renglon(200, 100, productoId: 2, categoriaId: 2),
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal(300m, r.Total.Subtotal);
        Assert.Equal(160m, r.Total.Costo);
        Assert.Equal(140m, r.Total.Margen);
    }

    [Fact]
    public void Excluye_las_facturas_anuladas()
    {
        var renglones = new[]
        {
            Renglon(100, 60),
            Renglon(999, 1, anulada: true),
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal(100m, r.Total.Subtotal);
        Assert.Equal(40m, r.Total.Margen);
        Assert.Single(r.PorProducto);
    }
}
