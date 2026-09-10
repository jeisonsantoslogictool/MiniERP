using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Tests.Compras;

public class CompraTests
{
    private static Producto Producto(decimal existencia = 0, decimal costo = 0) => new()
    {
        Id = 1,
        Codigo = "ARR-001",
        Descripcion = "Arroz selecto",
        Existencia = existencia,
        Costo = costo,
        ManejaInventario = true
    };

    private static Compra Compra(decimal cantidad, decimal costoUnitario, decimal itbis = 0.18m)
    {
        var compra = new Compra { Id = 7, Numero = "COM-0007", ProveedorId = 1 };

        compra.Lineas.Add(new LineaCompra
        {
            ProductoId = 1,
            Descripcion = "Arroz selecto",
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            TasaItbis = itbis
        });

        return compra;
    }

    private static Dictionary<int, Producto> Catalogo(Producto p) => new() { [p.Id] = p };

    [Fact]
    public void Recibir_suma_al_inventario()
    {
        var producto = Producto(existencia: 0);
        var compra = Compra(cantidad: 10, costoUnitario: 30);

        compra.Recibir(Catalogo(producto), "tester");

        Assert.Equal(10, producto.Existencia);
    }

    [Fact]
    public void Recibir_congela_el_documento()
    {
        var producto = Producto();
        var compra = Compra(10, 30);

        compra.Recibir(Catalogo(producto), "tester");

        Assert.Equal(EstadoCompra.Recibida, compra.Estado);
        Assert.False(compra.EsEditable);
        Assert.NotNull(compra.FechaRecepcion);
    }

    [Fact]
    public void El_movimiento_generado_apunta_a_la_compra_que_lo_origino()
    {
        var producto = Producto();
        var compra = Compra(10, 30);

        var movimientos = compra.Recibir(Catalogo(producto), "tester");

        var movimiento = Assert.Single(movimientos);
        Assert.Equal(TipoMovimiento.Entrada, movimiento.Tipo);
        Assert.Equal("COMPRA", movimiento.ReferenciaTipo);
        Assert.Equal(compra.Id, movimiento.ReferenciaId);
        Assert.Equal("Compra COM-0007", movimiento.Motivo);
        Assert.Equal("tester", movimiento.UsuarioId);
    }

    [Fact]
    public void Un_producto_sin_existencia_toma_el_costo_recibido()
    {
        var producto = Producto(existencia: 0, costo: 0);
        var compra = Compra(10, 30);

        compra.Recibir(Catalogo(producto), "tester");

        Assert.Equal(30, producto.Costo);
    }

    [Fact]
    public void El_costo_se_promedia_ponderado_por_las_unidades()
    {
        // 10 unidades a 30 ya en existencia, entran 10 mas a 40.
        // (10*30 + 10*40) / 20 = 35. Con ultimo costo darian 40 y el margen mentiria.
        var producto = Producto(existencia: 10, costo: 30);
        var compra = Compra(cantidad: 10, costoUnitario: 40);

        compra.Recibir(Catalogo(producto), "tester");

        Assert.Equal(35, producto.Costo);
        Assert.Equal(20, producto.Existencia);
    }

    [Fact]
    public void El_promedio_pondera_por_cantidad_y_no_por_el_promedio_simple()
    {
        // 90 a 10 y entran 10 a 20: (900 + 200) / 100 = 11, no 15.
        var producto = Producto(existencia: 90, costo: 10);
        var compra = Compra(cantidad: 10, costoUnitario: 20);

        compra.Recibir(Catalogo(producto), "tester");

        Assert.Equal(11, producto.Costo);
    }

    [Fact]
    public void El_promedio_soporta_cantidades_con_decimales()
    {
        // 25.5 LB a 28 y entran 10.5 LB a 32:
        // (714 + 336) / 36 = 29.1667
        var producto = Producto(existencia: 25.5m, costo: 28m);
        var compra = Compra(cantidad: 10.5m, costoUnitario: 32m);

        compra.Recibir(Catalogo(producto), "tester");

        Assert.Equal(29.1667m, producto.Costo);
        Assert.Equal(36m, producto.Existencia);
    }

    [Fact]
    public void Un_servicio_no_promedia_nada_y_toma_el_costo_recibido()
    {
        var producto = Producto(existencia: 0, costo: 5);
        producto.ManejaInventario = false;
        var compra = Compra(10, 30);

        compra.Recibir(Catalogo(producto), "tester");

        Assert.Equal(30, producto.Costo);
        Assert.Equal(0, producto.Existencia);
    }

    /// <summary>
    /// Recibir un servicio actualiza su costo y congela la compra, pero no puede devolver
    /// un movimiento: sin ProductoId asignado, la clave foranea lo rechazaria y la
    /// recepcion entera fallaria (mismo defecto que D-01 en ventas).
    /// </summary>
    [Fact]
    public void Recibir_un_servicio_no_genera_movimiento_de_inventario()
    {
        var producto = Producto(existencia: 0, costo: 5);
        producto.ManejaInventario = false;
        var compra = Compra(10, 30);

        var movimientos = compra.Recibir(Catalogo(producto), "tester");

        Assert.Empty(movimientos);
        Assert.Equal(EstadoCompra.Recibida, compra.Estado);
    }

    [Fact]
    public void Al_recibir_mercancia_y_servicio_juntos_solo_la_mercancia_mueve_kardex()
    {
        var arroz = Producto(existencia: 0);
        var flete = new Producto
        {
            Id = 2, Codigo = "SRV-FLETE", Descripcion = "Flete", Costo = 0, ManejaInventario = false
        };
        var compra = Compra(10, 30);
        compra.Lineas.Add(new LineaCompra
        {
            ProductoId = 2, Descripcion = "Flete", Cantidad = 1, CostoUnitario = 500, TasaItbis = 0
        });
        var catalogo = new Dictionary<int, Producto> { [1] = arroz, [2] = flete };

        var movimientos = compra.Recibir(catalogo, "tester");

        var unico = Assert.Single(movimientos);
        Assert.Equal(arroz.Id, unico.ProductoId);
        Assert.Equal(10, arroz.Existencia);
        Assert.Equal(500, flete.Costo);
    }

    [Fact]
    public void Una_compra_no_se_recibe_dos_veces()
    {
        var producto = Producto();
        var compra = Compra(10, 30);
        compra.Recibir(Catalogo(producto), "tester");

        var ex = Assert.Throws<InvalidOperationException>(
            () => compra.Recibir(Catalogo(producto), "tester"));

        Assert.Contains("recibida", ex.Message);
        Assert.Equal(10, producto.Existencia);
    }

    [Fact]
    public void Una_compra_sin_lineas_no_se_recibe()
    {
        var compra = new Compra { Numero = "COM-0001" };

        var ex = Assert.Throws<InvalidOperationException>(
            () => compra.Recibir(new Dictionary<int, Producto>(), "tester"));

        Assert.Contains("no tiene lineas", ex.Message);
    }

    [Fact]
    public void Recibir_sin_el_producto_de_la_linea_falla_antes_de_tocar_nada()
    {
        var compra = Compra(10, 30);

        var ex = Assert.Throws<InvalidOperationException>(
            () => compra.Recibir(new Dictionary<int, Producto>(), "tester"));

        Assert.Contains("Falta el producto", ex.Message);
        Assert.Equal(EstadoCompra.Borrador, compra.Estado);
    }

    [Fact]
    public void Una_compra_recibida_no_se_anula_porque_el_inventario_ya_se_movio()
    {
        var producto = Producto();
        var compra = Compra(10, 30);
        compra.Recibir(Catalogo(producto), "tester");

        var ex = Assert.Throws<InvalidOperationException>(() => compra.Anular());

        Assert.Contains("devolucion", ex.Message);
        Assert.Equal(EstadoCompra.Recibida, compra.Estado);
    }

    [Fact]
    public void Una_orden_todavia_no_recibida_si_se_anula()
    {
        var compra = Compra(10, 30);

        compra.Anular();

        Assert.Equal(EstadoCompra.Anulada, compra.Estado);
    }

    [Fact]
    public void Una_compra_anulada_no_se_recibe()
    {
        var producto = Producto();
        var compra = Compra(10, 30);
        compra.Anular();

        var ex = Assert.Throws<InvalidOperationException>(
            () => compra.Recibir(Catalogo(producto), "tester"));

        Assert.Contains("anulada", ex.Message);
        Assert.Equal(0, producto.Existencia);
    }

    [Fact]
    public void Los_totales_se_arman_desde_las_lineas()
    {
        var compra = Compra(cantidad: 10, costoUnitario: 30, itbis: 0.18m);

        compra.Recalcular();

        Assert.Equal(300m, compra.Subtotal);
        Assert.Equal(54m, compra.Itbis);
        Assert.Equal(354m, compra.Total);
    }

    [Fact]
    public void Una_compra_de_productos_exentos_no_lleva_itbis()
    {
        var compra = Compra(cantidad: 10, costoUnitario: 30, itbis: 0m);

        compra.Recalcular();

        Assert.Equal(300m, compra.Subtotal);
        Assert.Equal(0m, compra.Itbis);
        Assert.Equal(300m, compra.Total);
    }

    [Fact]
    public void Los_totales_mezclan_lineas_gravadas_y_exentas()
    {
        var compra = new Compra { Numero = "COM-0009" };
        compra.Lineas.Add(new LineaCompra { ProductoId = 1, Descripcion = "Arroz", Cantidad = 10, CostoUnitario = 30, TasaItbis = 0m });
        compra.Lineas.Add(new LineaCompra { ProductoId = 2, Descripcion = "Jabon", Cantidad = 5, CostoUnitario = 50, TasaItbis = 0.18m });

        compra.Recalcular();

        Assert.Equal(550m, compra.Subtotal);   // 300 + 250
        Assert.Equal(45m, compra.Itbis);       // solo el jabon: 250 * 0.18
        Assert.Equal(595m, compra.Total);
    }

    [Fact]
    public void El_itbis_se_redondea_a_dos_decimales_hacia_arriba_en_el_medio()
    {
        // 33.33 * 0.18 = 5.9994 -> 6.00
        var compra = Compra(cantidad: 1, costoUnitario: 33.33m, itbis: 0.18m);

        compra.Recalcular();

        Assert.Equal(6.00m, compra.Itbis);
    }

    [Theory]
    [InlineData(2.505, 2.51)]   // .NET por defecto (a par) daria 2.50
    [InlineData(2.515, 2.52)]   // .NET por defecto daria 2.52 tambien: aqui coinciden
    [InlineData(0.005, 0.01)]   // .NET por defecto daria 0.00
    public void El_redondeo_de_importes_es_aritmetico_y_no_bancario(decimal valor, decimal esperado)
    {
        // El comerciante espera que el medio siempre suba. Que 2.505 a veces de 2.50
        // y 2.515 de 2.52 le hace perder la confianza en la caja.
        Assert.Equal(esperado, RetailConstants.RedondearImporte(valor));
    }
}
