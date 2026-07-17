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

    [Fact]
    public void Confirmar_baja_el_inventario()
    {
        var producto = Producto(existencia: 20);
        var dev = Devolucion(cantidad: 3);

        dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        Assert.Equal(17, producto.Existencia);
    }

    [Fact]
    public void Confirmar_congela_el_documento()
    {
        var producto = Producto();
        var dev = Devolucion(3);

        dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        Assert.Equal(EstadoDevolucion.Confirmada, dev.Estado);
        Assert.False(dev.EsEditable);
    }

    [Fact]
    public void El_movimiento_generado_es_una_devolucion_a_proveedor()
    {
        var producto = Producto();
        var dev = Devolucion(3, motivo: "Producto vencido");

        var movimientos = dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        var mov = Assert.Single(movimientos);
        Assert.Equal(TipoMovimiento.DevolucionProveedor, mov.Tipo);
        Assert.Equal("DEVOLUCION_COMPRA", mov.ReferenciaTipo);
        Assert.Equal(dev.Id, mov.ReferenciaId);
        Assert.Equal("Devolución DEV-0005: Producto vencido", mov.Motivo);
        Assert.Equal("tester", mov.UsuarioId);
    }

    [Fact]
    public void El_movimiento_usa_el_costo_congelado_de_la_compra()
    {
        // El costo vivo del producto (99) no importa: se devuelve al costo de la compra (30).
        var producto = Producto(costo: 99);
        var dev = Devolucion(3, costoUnitario: 30);

        var movimientos = dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        Assert.Equal(30, Assert.Single(movimientos).CostoUnitario);
    }
}
