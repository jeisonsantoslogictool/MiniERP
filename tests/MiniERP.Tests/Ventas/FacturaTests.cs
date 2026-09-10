using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Ventas;

public class FacturaTests
{
    private static Producto Producto(decimal existencia = 100, decimal costo = 30, decimal precio = 40) => new()
    {
        Id = 1,
        Codigo = "ARR-001",
        Descripcion = "Arroz selecto",
        Existencia = existencia,
        Costo = costo,
        PrecioVenta = precio,
        ManejaInventario = true
    };

    private static Factura Factura(decimal cantidad = 3, decimal precio = 40, decimal itbis = 0m)
    {
        var factura = new Factura { Id = 5, Numero = "FAC-0005" };

        factura.Lineas.Add(new LineaFactura
        {
            ProductoId = 1,
            Descripcion = "Arroz selecto",
            Cantidad = cantidad,
            PrecioUnitario = precio,
            TasaItbis = itbis
        });

        return factura;
    }

    private static Dictionary<int, Producto> Catalogo(Producto p) => new() { [p.Id] = p };

    [Fact]
    public void Emitir_descuenta_del_inventario()
    {
        var producto = Producto(existencia: 29.5m);
        var factura = Factura(cantidad: 3);

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal(26.5m, producto.Existencia);
    }

    [Fact]
    public void Emitir_asigna_el_comprobante_recibido()
    {
        var producto = Producto();
        var factura = Factura();

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal("B0200000001", factura.Ncf);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
        Assert.Equal("cajero", factura.UsuarioId);
    }

    /// <summary>
    /// Emitir no puede enlazar el movimiento con su factura, y no debe fingir que si.
    /// </summary>
    /// <remarks>
    /// La version anterior de esta prueba afirmaba <c>Equal(factura.Id, ReferenciaId)</c>
    /// y pasaba en verde, porque el ayudante construye la factura con Id = 5. En
    /// produccion no ocurre nunca: al emitir, la factura es nueva y su Id vale 0, asi que
    /// todos los movimientos de venta quedaban con ReferenciaId = 0 y ninguna salida de
    /// mercancia podia rastrearse hasta su documento. La prueba estaba probando un
    /// escenario imposible. Ahora fija lo contrario: aqui el movimiento sale sin
    /// referencia, y quien la pone es el caso de uso una vez la factura tiene Id
    /// (ver <c>VentaServiceTests</c>).
    /// </remarks>
    [Fact]
    public void El_movimiento_sale_sin_referencia_porque_la_factura_aun_no_tiene_Id()
    {
        var producto = Producto();
        var factura = Factura(cantidad: 3);

        var movimientos = factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        var movimiento = Assert.Single(movimientos);
        Assert.Equal(TipoMovimiento.Salida, movimiento.Tipo);
        Assert.Equal("FACTURA", movimiento.ReferenciaTipo);
        Assert.Null(movimiento.ReferenciaId);
        Assert.Equal("Factura B0200000001", movimiento.Motivo);
    }

    [Fact]
    public void No_se_puede_vender_mas_de_lo_que_hay()
    {
        var producto = Producto(existencia: 2);
        var factura = Factura(cantidad: 3);

        var ex = Assert.Throws<InvalidOperationException>(
            () => factura.Emitir(Catalogo(producto), "B0200000001", "cajero"));

        Assert.Contains("negativa", ex.Message);
        Assert.Equal(2, producto.Existencia);
        Assert.Null(factura.Ncf);
    }

    [Fact]
    public void Una_factura_sin_lineas_no_se_emite()
    {
        var factura = new Factura { Numero = "FAC-0001" };

        var ex = Assert.Throws<InvalidOperationException>(
            () => factura.Emitir(new Dictionary<int, Producto>(), "B0200000001", "cajero"));

        Assert.Contains("no tiene lineas", ex.Message);
    }

    [Fact]
    public void No_se_emite_sin_comprobante()
    {
        var producto = Producto();
        var factura = Factura();

        Assert.ThrowsAny<ArgumentException>(
            () => factura.Emitir(Catalogo(producto), "", "cajero"));
    }

    // ---------- El congelado del costo: la base del Capitulo V ----------

    [Fact]
    public void El_costo_se_congela_al_emitir()
    {
        var producto = Producto(costo: 30);
        var factura = Factura(cantidad: 3);

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal(30, factura.Lineas.First().CostoUnitario);
        Assert.Equal(90, factura.CostoTotal);
    }

    [Fact]
    public void Una_compra_posterior_no_altera_el_margen_de_una_venta_ya_hecha()
    {
        // Se vende a costo 30, y despues entra mercancia mas cara que sube el promedio.
        // La venta de ayer debe seguir mostrando lo que realmente se gano ayer.
        var producto = Producto(existencia: 100, costo: 30);
        var factura = Factura(cantidad: 10, precio: 40);

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        var margenAlVender = factura.Margen;

        var compra = new Compra { Id = 9, Numero = "COM-0009" };
        compra.Lineas.Add(new LineaCompra
        {
            ProductoId = 1,
            Descripcion = "Arroz selecto",
            Cantidad = 100,
            CostoUnitario = 38
        });
        compra.Recibir(Catalogo(producto), "almacen");

        Assert.True(producto.Costo > 30);          // el costo del producto subio
        Assert.Equal(margenAlVender, factura.Margen);  // el margen de la venta, no
        Assert.Equal(100m, factura.Margen);            // (40 - 30) * 10
    }

    [Fact]
    public void El_margen_no_cuenta_el_itbis_porque_no_es_del_comercio()
    {
        var producto = Producto(costo: 30);
        var factura = Factura(cantidad: 10, precio: 40, itbis: 0.18m);

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal(400m, factura.Subtotal);
        Assert.Equal(72m, factura.Itbis);
        Assert.Equal(472m, factura.Total);   // lo que paga el cliente
        Assert.Equal(100m, factura.Margen);  // 400 - 300, sin tocar el ITBIS
        Assert.Equal(0.25m, factura.MargenPorcentaje);
    }

    [Fact]
    public void Una_venta_por_debajo_del_costo_da_margen_negativo()
    {
        var producto = Producto(costo: 30);
        var factura = Factura(cantidad: 10, precio: 25);

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal(-50m, factura.Margen);
    }

    // ---------- Cambio ----------

    [Fact]
    public void El_cambio_sale_de_lo_que_el_cliente_entrego()
    {
        var producto = Producto(costo: 30);
        var factura = Factura(cantidad: 10, precio: 40);
        factura.MontoRecibido = 500;

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal(400m, factura.Total);
        Assert.Equal(100m, factura.Cambio);
    }

    [Fact]
    public void Pagar_justo_no_deja_cambio()
    {
        var producto = Producto();
        var factura = Factura(cantidad: 10, precio: 40);
        factura.MontoRecibido = 400;

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal(0m, factura.Cambio);
    }

    [Fact]
    public void Una_venta_a_credito_no_tiene_cambio()
    {
        var producto = Producto();
        var factura = Factura(cantidad: 10, precio: 40);
        factura.Condicion = CondicionPago.Credito;
        factura.MontoRecibido = 500;   // aunque venga un monto, a credito no aplica

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal(0m, factura.Cambio);
    }

    // ---------- Anulacion ----------

    [Fact]
    public void Anular_repone_el_inventario()
    {
        var producto = Producto(existencia: 29.5m);
        var factura = Factura(cantidad: 3);
        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        factura.Anular(Catalogo(producto), "El cliente se arrepintio", "cajero");

        Assert.Equal(29.5m, producto.Existencia);
        Assert.Equal(EstadoFactura.Anulada, factura.Estado);
    }

    [Fact]
    public void La_anulacion_no_libera_el_ncf()
    {
        var producto = Producto();
        var factura = Factura();
        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        factura.Anular(Catalogo(producto), "Error del cajero", "cajero");

        // Ante la DGII queda como comprobante anulado, y el rango sigue avanzando.
        Assert.Equal("B0200000001", factura.Ncf);
    }

    [Fact]
    public void La_reposicion_entra_como_devolucion_y_dice_por_que()
    {
        var producto = Producto();
        var factura = Factura();
        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        var movimientos = factura.Anular(Catalogo(producto), "El cliente se arrepintio", "cajero");

        var movimiento = Assert.Single(movimientos);
        Assert.Equal(TipoMovimiento.DevolucionCliente, movimiento.Tipo);
        Assert.True(movimiento.EsEntrada);
        Assert.Contains("El cliente se arrepintio", movimiento.Motivo);
        Assert.Equal("ANULACION", movimiento.ReferenciaTipo);
    }

    [Fact]
    public void Anular_exige_motivo()
    {
        var producto = Producto();
        var factura = Factura();
        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        var ex = Assert.Throws<InvalidOperationException>(
            () => factura.Anular(Catalogo(producto), "  ", "cajero"));

        Assert.Contains("motivo", ex.Message);
        Assert.Equal(EstadoFactura.Emitida, factura.Estado);
    }

    [Fact]
    public void Una_factura_no_se_anula_dos_veces()
    {
        var producto = Producto();
        var factura = Factura();
        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");
        factura.Anular(Catalogo(producto), "Error", "cajero");

        var ex = Assert.Throws<InvalidOperationException>(
            () => factura.Anular(Catalogo(producto), "Otra vez", "cajero"));

        Assert.Contains("ya esta anulada", ex.Message);
    }

    // ---------- Peso ----------

    [Fact]
    public void Se_puede_facturar_por_peso_con_decimales()
    {
        var producto = Producto(existencia: 29.5m, costo: 30.8729m, precio: 40);
        var factura = Factura(cantidad: 2.375m, precio: 40);

        factura.Emitir(Catalogo(producto), "B0200000001", "cajero");

        Assert.Equal(27.125m, producto.Existencia);
        Assert.Equal(95m, factura.Subtotal);                    // 2.375 * 40
        Assert.Equal(73.32m, factura.CostoTotal);               // 2.375 * 30.8729 = 73.3231 -> 73.32
    }

    // ---------- Servicios (D-01) ----------

    /// <summary>
    /// Una recarga o una fotocopia se factura, pero no sale de ningun estante. Antes,
    /// Emitir devolvia igual un movimiento para el servicio; como AplicarMovimiento lo
    /// ignoraba sin asignarle ProductoId, la capa de datos intentaba guardar un movimiento
    /// huerfano, la clave foranea lo rechazaba y la venta entera se perdia.
    /// </summary>
    [Fact]
    public void Un_servicio_se_factura_sin_generar_movimiento_de_inventario()
    {
        var servicio = Producto(existencia: 0, costo: 20, precio: 25);
        servicio.ManejaInventario = false;
        var factura = Factura(cantidad: 2, precio: 25);

        var movimientos = factura.Emitir(Catalogo(servicio), "B0200000001", "cajero");

        Assert.Empty(movimientos);
        Assert.Equal(0, servicio.Existencia);
        Assert.Equal("B0200000001", factura.Ncf);
        Assert.Equal(50m, factura.Subtotal);
        Assert.Equal(40m, factura.CostoTotal);                  // el costo si se congela: hay margen
    }

    [Fact]
    public void Ningun_movimiento_sale_sin_producto_al_mezclar_servicio_y_mercancia()
    {
        var arroz = Producto(existencia: 10);
        var recarga = new Producto
        {
            Id = 2,
            Codigo = "REC-001",
            Descripcion = "Recarga Claro",
            Costo = 95,
            PrecioVenta = 100,
            ManejaInventario = false
        };
        var factura = Factura(cantidad: 3);
        factura.Lineas.Add(new LineaFactura
        {
            ProductoId = 2, Descripcion = "Recarga Claro", Cantidad = 1, PrecioUnitario = 100
        });
        var catalogo = new Dictionary<int, Producto> { [1] = arroz, [2] = recarga };

        var movimientos = factura.Emitir(catalogo, "B0200000001", "cajero");

        var unico = Assert.Single(movimientos);
        Assert.Equal(arroz.Id, unico.ProductoId);
        Assert.All(movimientos, m => Assert.NotEqual(0, m.ProductoId));
        Assert.Equal(7, arroz.Existencia);
    }

    [Fact]
    public void Anular_una_venta_de_servicio_no_repone_nada()
    {
        var servicio = Producto(existencia: 0, costo: 20, precio: 25);
        servicio.ManejaInventario = false;
        var factura = Factura(cantidad: 2, precio: 25);
        factura.Emitir(Catalogo(servicio), "B0200000001", "cajero");

        var movimientos = factura.Anular(Catalogo(servicio), "Cliente se arrepintio", "cajero");

        Assert.Empty(movimientos);
        Assert.Equal(0, servicio.Existencia);
        Assert.Equal(EstadoFactura.Anulada, factura.Estado);
    }
}
