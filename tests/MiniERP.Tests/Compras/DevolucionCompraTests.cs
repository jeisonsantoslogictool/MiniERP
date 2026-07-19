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

    [Fact]
    public void No_se_devuelve_mas_de_lo_comprado()
    {
        var producto = Producto(existencia: 20);
        var dev = Devolucion(cantidad: 6);

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(Catalogo(producto), Devolvible(5), "tester"));

        Assert.Contains("quedan 5", ex.Message);
        Assert.Equal(20, producto.Existencia); // no toco nada
    }

    [Fact]
    public void Se_puede_devolver_exactamente_lo_que_queda_por_devolver()
    {
        // Compre 10, ya devolvi 7 -> quedan 3. Devuelvo 3: pasa justo en el borde.
        var producto = Producto(existencia: 20);
        var dev = Devolucion(cantidad: 3);

        dev.Confirmar(Catalogo(producto), Devolvible(3), "tester");

        Assert.Equal(17, producto.Existencia);
        Assert.Equal(EstadoDevolucion.Confirmada, dev.Estado);
    }

    [Fact]
    public void No_se_devuelve_mas_de_lo_que_hay_en_existencia()
    {
        // El proveedor deja devolver 10, pero ya se vendieron y solo quedan 2 en el estante.
        var producto = Producto(existencia: 2);
        var dev = Devolucion(cantidad: 5);

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(Catalogo(producto), Devolvible(10), "tester"));

        Assert.Contains("negativa", ex.Message);
        Assert.Equal(2, producto.Existencia);
    }

    [Fact]
    public void Una_devolucion_sin_motivo_no_se_confirma()
    {
        var producto = Producto();
        var dev = Devolucion(3, motivo: "   ");

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(Catalogo(producto), Devolvible(10), "tester"));

        Assert.Contains("motivo", ex.Message);
        Assert.Equal(50, producto.Existencia);
    }

    [Fact]
    public void Una_devolucion_sin_lineas_no_se_confirma()
    {
        var dev = new DevolucionCompra { Numero = "DEV-0001", Motivo = "x" };

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(new Dictionary<int, Producto>(), new Dictionary<int, decimal>(), "tester"));

        Assert.Contains("no tiene líneas", ex.Message);
    }

    [Fact]
    public void Confirmar_sin_el_producto_de_la_linea_falla_antes_de_tocar_nada()
    {
        var dev = Devolucion(3);

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(new Dictionary<int, Producto>(), Devolvible(10), "tester"));

        Assert.Contains("Falta el producto", ex.Message);
        Assert.Equal(EstadoDevolucion.Borrador, dev.Estado);
    }

    [Fact]
    public void Una_devolucion_no_se_confirma_dos_veces()
    {
        var producto = Producto(existencia: 20);
        var dev = Devolucion(3);
        dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(Catalogo(producto), Devolvible(10), "tester"));

        Assert.Contains("confirmada", ex.Message);
        Assert.Equal(17, producto.Existencia); // no volvio a bajar
    }

    [Fact]
    public void El_costo_del_producto_no_se_repromedia_al_devolver()
    {
        var producto = Producto(existencia: 20, costo: 35);
        var dev = Devolucion(3, costoUnitario: 30);

        dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        Assert.Equal(35, producto.Costo); // el costo vivo no cambia
    }
}
