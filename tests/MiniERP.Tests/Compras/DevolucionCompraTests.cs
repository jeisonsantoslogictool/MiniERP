using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;

namespace MiniERP.Tests.Compras;

public class DevolucionCompraTests
{
    private static Producto Producto(decimal existencia = 50, decimal costo = 30) => new()
    {
        Id = 1,
        Codigo = "ARR-001",
        Descripcion = "Arroz selecto",
        Existencia = existencia,
        Costo = costo,
        ManejaInventario = true
    };

    private static DevolucionCompra Devolucion(
        decimal cantidad,
        decimal costoUnitario = 30,
        string motivo = "Producto vencido",
        decimal itbis = 0m,
        int lineaCompraId = 100)
    {
        var dev = new DevolucionCompra
        {
            Id = 5,
            Numero = "DEV-0005",
            CompraId = 7,
            ProveedorId = 1,
            Motivo = motivo
        };

        dev.Lineas.Add(new LineaDevolucionCompra
        {
            LineaCompraId = lineaCompraId,
            ProductoId = 1,
            Descripcion = "Arroz selecto",
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            TasaItbis = itbis
        });

        return dev;
    }

    private static Dictionary<int, Producto> Catalogo(Producto p) => new() { [p.Id] = p };

    private static Dictionary<int, decimal> Devolvible(decimal cantidad, int lineaCompraId = 100) =>
        new() { [lineaCompraId] = cantidad };

    [Fact]
    public void Los_totales_se_arman_desde_las_lineas()
    {
        var dev = Devolucion(cantidad: 10, costoUnitario: 30, itbis: 0.18m);

        dev.Recalcular();

        Assert.Equal(300m, dev.Subtotal);
        Assert.Equal(54m, dev.Itbis);
        Assert.Equal(354m, dev.Total);
    }
}
