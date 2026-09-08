using MiniERP.Application.Clientes.Dtos;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Inventario.Dtos;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Integracion;

/// <summary>
/// Un dia de operacion del minimarket, de punta a punta y por los cinco modulos.
/// </summary>
/// <remarks>
/// Las demas pruebas miran un modulo a la vez, y por eso no pueden ver lo que de verdad
/// se rompe cuando el sistema se usa completo: que el costo promedio que calcula Compras
/// sea el que Ventas congela en la linea, y que ese sea el que Finanzas usa para el margen.
/// Aqui se compra, se vende de contado y fiado, se cobra una parte, se paga al proveedor y
/// se registra un gasto; despues se leen los reportes y se comprueba que la aritmetica
/// cierra en los cinco.
///
/// El escenario esta armado para que caiga en el caso que confunde a cualquiera: el dia
/// deja utilidad y aun asi la gaveta cierra con menos efectivo del que abrio, porque buena
/// parte de la venta se fio y al proveedor si hubo que pagarle.
/// </remarks>
public class MinimarketEndToEndTests
{
    private const string Cajero = "jeison";

    /// <summary>Lo que el dia dejo escrito, para que cada prueba mire una parte.</summary>
    private sealed record Dia(
        Minimarket Comercio,
        int ArrozId,
        int RefrescoId,
        int ClienteId,
        int ProveedorId,
        FacturaEmitidaDto Contado,
        FacturaEmitidaDto Credito,
        FiltroPeriodo Periodo);

    private static async Task<Dia> UnDiaDeOperacion()
    {
        var comercio = new Minimarket();
        var datos = comercio.Datos;

        // ---------- El comercio antes de abrir ----------
        var libra = comercio.SembrarUnidad("LB", permiteDecimales: true, decimales: 3);
        var unidad = comercio.SembrarUnidad("UND", permiteDecimales: false);
        var viveres = comercio.SembrarCategoria("Viveres");
        var bebidas = comercio.SembrarCategoria("Bebidas");
        var cliente = comercio.SembrarCliente("Colmado La Esquina", limiteCredito: 5000m);
        var proveedor = comercio.SembrarProveedor("Distribuidora del Cibao");
        var luz = comercio.SembrarCategoriaEgreso("Luz");
        comercio.SembrarSecuencia(TipoComprobante.Consumo, "B02");

        // El arroz es de canasta basica: exento de ITBIS. El refresco va gravado al 18%.
        var arroz = await comercio.Productos.CrearAsync(new ProductoFormDto
        {
            Codigo = "ARR-LB",
            Descripcion = "Arroz selecto",
            CategoriaId = viveres.Id,
            UnidadMedidaId = libra.Id,
            Costo = 30m,
            PrecioVenta = 45m,
            TasaItbis = 0m,
            ExistenciaInicial = 10m,
            ExistenciaMinima = 5m
        }, Cajero);

        var refresco = await comercio.Productos.CrearAsync(new ProductoFormDto
        {
            Codigo = "REF-UN",
            Descripcion = "Refresco 600 ml",
            CategoriaId = bebidas.Id,
            UnidadMedidaId = unidad.Id,
            Costo = 25m,
            PrecioVenta = 35m,
            TasaItbis = 0.18m,
            ExistenciaInicial = 24m,
            ExistenciaMinima = 6m
        }, Cajero);

        var arrozId = arroz.Valor;
        var refrescoId = refresco.Valor;

        // ---------- Llega mercancia: 10 LB de arroz a 40, a credito ----------
        // Habia 10 LB a 30; el promedio ponderado tiene que quedar en 35.
        var compra = await comercio.Compras.GuardarAsync(new CompraFormDto
        {
            Numero = "COM-0001",
            ProveedorId = proveedor.Id,
            Condicion = CondicionPago.Credito,
            Lineas =
            [
                new LineaCompraFormDto
                {
                    ProductoId = arrozId,
                    Codigo = "ARR-LB",
                    Descripcion = "Arroz selecto",
                    UnidadMedida = "LB",
                    PermiteDecimales = true,
                    Cantidad = 10m,
                    CostoUnitario = 40m,
                    TasaItbis = 0m
                }
            ]
        }, Cajero);

        await comercio.Compras.RecibirAsync(compra.Valor, Cajero);

        // ---------- Se abre la caja ----------
        // Venta de contado: 3 LB de arroz y 2 refrescos, paga con 250.
        var contado = await comercio.Ventas.EmitirAsync(new VentaFormDto
        {
            Condicion = CondicionPago.Contado,
            MontoRecibido = 250m,
            Lineas =
            [
                LineaDeVenta(arrozId, "ARR-LB", "Arroz selecto", "LB", true, 3m, 45m, 0m),
                LineaDeVenta(refrescoId, "REF-UN", "Refresco 600 ml", "UND", false, 2m, 35m, 0.18m)
            ]
        }, Cajero);

        // Venta a credito al colmado de la esquina: 5 LB de arroz, sin cobrar todavia.
        var credito = await comercio.Ventas.EmitirAsync(new VentaFormDto
        {
            ClienteId = cliente.Id,
            Condicion = CondicionPago.Credito,
            Lineas = [LineaDeVenta(arrozId, "ARR-LB", "Arroz selecto", "LB", true, 5m, 45m, 0m)]
        }, Cajero);

        // El cliente abona 100 de lo que debe.
        await comercio.Cobros.RegistrarCobroAsync(new CobroFormDto
        {
            ClienteId = cliente.Id,
            Fecha = DateTime.UtcNow,
            Monto = 100m,
            Observacion = "Abono en efectivo"
        }, Cajero);

        // Gasto operativo del dia.
        await comercio.Egresos.GuardarAsync(new EgresoFormDto
        {
            Fecha = DateTime.UtcNow,
            CategoriaEgresoId = luz.Id,
            Monto = 50m,
            Descripcion = "Energia electrica"
        }, Cajero);

        // Se le abona 300 al proveedor. Se aplica por la regla del dominio, que es la misma
        // que usa el caso de uso de pagos: ningun balance cambia sin su asiento.
        var pago = new Pago { Fecha = DateTime.UtcNow, Monto = 300m, UsuarioId = Cajero };
        proveedor.AplicarPago(pago);
        datos.Pagos.Add(pago);

        var hoy = DateTime.UtcNow.Date;

        return new Dia(
            comercio, arrozId, refrescoId, cliente.Id, proveedor.Id,
            contado.Valor!, credito.Valor!,
            new FiltroPeriodo(hoy, hoy.AddDays(1).AddTicks(-1)));
    }

    private static LineaVentaDto LineaDeVenta(
        int productoId, string codigo, string descripcion, string unidad,
        bool permiteDecimales, decimal cantidad, decimal precio, decimal itbis) =>
        new()
        {
            ProductoId = productoId,
            Codigo = codigo,
            Descripcion = descripcion,
            UnidadMedida = unidad,
            PermiteDecimales = permiteDecimales,
            Cantidad = cantidad,
            PrecioUnitario = precio,
            TasaItbis = itbis
        };

    // ---------- Compras e inventario ----------

    /// <summary>
    /// Diez libras a 30 y diez a 40 valen 35 cada una, no 40. Con ultimo costo el margen
    /// de las primeras diez desapareceria del reporte.
    /// </summary>
    [Fact]
    public async Task Recibir_la_compra_deja_el_costo_en_el_promedio_ponderado()
    {
        var dia = await UnDiaDeOperacion();
        var arroz = dia.Comercio.Datos.Productos[dia.ArrozId];

        Assert.Equal(35m, arroz.Costo);
    }

    [Fact]
    public async Task La_mercancia_recibida_sube_la_existencia_y_la_venta_la_baja()
    {
        var dia = await UnDiaDeOperacion();
        var arroz = dia.Comercio.Datos.Productos[dia.ArrozId];

        // 10 de apertura + 10 de la compra - 3 de contado - 5 a credito.
        Assert.Equal(12m, arroz.Existencia);
    }

    /// <summary>
    /// La invariante del kardex: cada movimiento arranca donde termino el anterior, y el
    /// ultimo tiene que coincidir con la existencia del producto. Si una entrada se
    /// registra pero no se aplica —o se aplica dos veces— la cadena se parte justo aqui,
    /// y el saldo deja de ser reconstruible.
    /// </summary>
    [Fact]
    public async Task El_kardex_del_arroz_cuadra_movimiento_por_movimiento()
    {
        var dia = await UnDiaDeOperacion();
        var arroz = dia.Comercio.Datos.Productos[dia.ArrozId];
        var kardex = dia.Comercio.Datos.KardexDe(dia.ArrozId);

        Assert.Equal(4, kardex.Count);

        decimal saldo = 0;
        foreach (var movimiento in kardex)
        {
            Assert.Equal(saldo, movimiento.ExistenciaAnterior);
            saldo = movimiento.ExistenciaResultante;
        }

        Assert.Equal(arroz.Existencia, saldo);
    }

    /// <summary>
    /// Cada salida tiene que decir de que factura salio. Sin ese enlace, la pregunta
    /// "que factura bajo el arroz" solo se responde leyendo el texto del motivo.
    /// </summary>
    [Fact]
    public async Task Cada_movimiento_de_venta_apunta_a_su_factura()
    {
        var dia = await UnDiaDeOperacion();

        var salidas = dia.Comercio.Datos.KardexDe(dia.ArrozId)
            .Where(m => m.ReferenciaTipo == "FACTURA")
            .ToList();

        Assert.Equal(2, salidas.Count);
        Assert.All(salidas, m => Assert.True(m.ReferenciaId > 0));
        Assert.Contains(salidas, m => m.ReferenciaId == dia.Contado.Id);
        Assert.Contains(salidas, m => m.ReferenciaId == dia.Credito.Id);
    }

    // ---------- Punto de venta ----------

    [Fact]
    public async Task La_venta_de_contado_cobra_el_itbis_solo_de_lo_gravado_y_da_el_cambio()
    {
        var dia = await UnDiaDeOperacion();
        var factura = dia.Comercio.Datos.Facturas.Single(f => f.Id == dia.Contado.Id);

        // Arroz 3 x 45 = 135 exento; refresco 2 x 35 = 70 con 12.60 de ITBIS.
        Assert.Equal(205m, factura.Subtotal);
        Assert.Equal(12.60m, factura.Itbis);
        Assert.Equal(217.60m, factura.Total);
        Assert.Equal(32.40m, factura.Cambio);
    }

    [Fact]
    public async Task Cada_factura_consume_su_ncf_y_los_numeros_no_se_repiten()
    {
        var dia = await UnDiaDeOperacion();

        Assert.Equal("B0200000001", dia.Contado.Ncf);
        Assert.Equal("B0200000002", dia.Credito.Ncf);
    }

    /// <summary>
    /// El costo se congela al vender. Si manana sube por otra compra, el margen de la
    /// venta de hoy no puede moverse: los resultados del informe se moverian solos.
    /// </summary>
    [Fact]
    public async Task El_costo_queda_congelado_en_la_linea_aunque_el_producto_cambie_despues()
    {
        var dia = await UnDiaDeOperacion();
        var factura = dia.Comercio.Datos.Facturas.Single(f => f.Id == dia.Contado.Id);
        var lineaArroz = factura.Lineas.Single(l => l.ProductoId == dia.ArrozId);

        Assert.Equal(35m, lineaArroz.CostoUnitario);

        // Llega mercancia mas cara y el costo del producto sube.
        dia.Comercio.Datos.Productos[dia.ArrozId].Costo = 60m;

        Assert.Equal(35m, lineaArroz.CostoUnitario);
        Assert.Equal(105m, lineaArroz.CostoTotal);
    }

    // ---------- Credito y cobros ----------

    [Fact]
    public async Task La_venta_a_credito_sube_la_deuda_del_cliente_y_el_cobro_la_baja()
    {
        var dia = await UnDiaDeOperacion();
        var cliente = dia.Comercio.Datos.Clientes[dia.ClienteId];

        // Fio 225 y abono 100.
        Assert.Equal(125m, cliente.BalanceActual);
        Assert.Equal(4875m, cliente.CreditoDisponible);
    }

    [Fact]
    public async Task El_cobro_deja_escrito_el_balance_antes_y_despues()
    {
        var dia = await UnDiaDeOperacion();
        var cobro = Assert.Single(dia.Comercio.Datos.Cobros);

        Assert.Equal(225m, cobro.BalanceAnterior);
        Assert.Equal(125m, cobro.BalanceResultante);
        Assert.Equal(dia.ClienteId, cobro.ClienteId);
    }

    [Fact]
    public async Task La_compra_a_credito_sube_la_deuda_con_el_proveedor_y_el_pago_la_baja()
    {
        var dia = await UnDiaDeOperacion();
        var proveedor = dia.Comercio.Datos.Proveedores[dia.ProveedorId];

        // Compro 400 a credito y abono 300.
        Assert.Equal(100m, proveedor.BalanceActual);
    }

    // ---------- Finanzas: los dos reportes ----------

    /// <summary>
    /// El estado de resultados del dia: ingresos sin ITBIS, costo congelado y gastos
    /// operativos. El pago al proveedor no aparece por ninguna parte.
    /// </summary>
    [Fact]
    public async Task El_estado_de_resultados_da_la_utilidad_del_dia()
    {
        var dia = await UnDiaDeOperacion();

        var estado = await dia.Comercio.EstadoResultados.ObtenerAsync(dia.Periodo);

        Assert.Equal(430m, estado.Ingresos);      // 205 de contado + 225 a credito
        Assert.Equal(330m, estado.CostoVendido);  // 8 LB a 35 + 2 refrescos a 25
        Assert.Equal(50m, estado.Egresos);
        Assert.Equal(100m, estado.MargenBruto);
        Assert.Equal(50m, estado.Utilidad);
    }

    /// <summary>
    /// Los 300 que se le pagaron al proveedor son la mitad de todo lo que se vendio: si
    /// entraran como gasto, el dia cerraria con perdida en vez de con 50 de ganancia.
    /// El costo de esa mercancia ya esta contado en el costo de lo vendido.
    /// </summary>
    [Fact]
    public async Task El_pago_al_proveedor_no_aparece_en_el_estado_de_resultados()
    {
        var dia = await UnDiaDeOperacion();

        var estado = await dia.Comercio.EstadoResultados.ObtenerAsync(dia.Periodo);
        var pagado = dia.Comercio.Datos.Pagos.Sum(p => p.Monto);

        Assert.Equal(300m, pagado);
        Assert.Equal(50m, estado.Egresos);
        Assert.True(estado.Utilidad > 0);
    }

    /// <summary>
    /// El flujo de caja cuenta lo contrario: la venta fiada no entro a la gaveta, el
    /// abono del cliente si, y al proveedor hubo que pagarle de verdad.
    /// </summary>
    [Fact]
    public async Task El_flujo_de_caja_cuenta_el_efectivo_y_deja_fuera_lo_fiado()
    {
        var dia = await UnDiaDeOperacion();

        var flujo = await dia.Comercio.FlujoCaja.ObtenerAsync(dia.Periodo);

        Assert.Equal(217.60m, flujo.VentasContado);  // con ITBIS: es dinero en la gaveta
        Assert.Equal(100m, flujo.Cobros);
        Assert.Equal(50m, flujo.Egresos);
        Assert.Equal(300m, flujo.Pagos);
        Assert.Equal(317.60m, flujo.Entradas);
        Assert.Equal(350m, flujo.Salidas);
    }

    /// <summary>
    /// El caso que hay que saber leer: el dia gano 50 y aun asi salio mas efectivo del
    /// que entro. No es un error del sistema; es lo que pasa cuando se vende fiado y se
    /// paga de contado, y es la razon por la que el anteproyecto pide los dos reportes.
    /// </summary>
    [Fact]
    public async Task El_dia_deja_utilidad_y_aun_asi_la_gaveta_cierra_con_menos_efectivo()
    {
        var dia = await UnDiaDeOperacion();

        var estado = await dia.Comercio.EstadoResultados.ObtenerAsync(dia.Periodo);
        var flujo = await dia.Comercio.FlujoCaja.ObtenerAsync(dia.Periodo);

        Assert.Equal(50m, estado.Utilidad);
        Assert.Equal(-32.40m, flujo.EfectivoNeto);
    }

    /// <summary>
    /// El margen por producto es lo que responde "que me deja dinero de verdad": el arroz
    /// deja 10 por libra sobre 45, y el refresco 10 por unidad sobre 35.
    /// </summary>
    [Fact]
    public async Task La_rentabilidad_separa_el_margen_por_producto()
    {
        var dia = await UnDiaDeOperacion();

        var resumen = await dia.Comercio.Rentabilidad.ObtenerAsync(dia.Periodo);

        Assert.Equal(430m, resumen.Total.Subtotal);
        Assert.Equal(330m, resumen.Total.Costo);
        Assert.Equal(100m, resumen.Total.Margen);

        var arroz = resumen.PorProducto.Single(p => p.Id == dia.ArrozId);
        Assert.Equal(360m, arroz.Subtotal);   // 8 LB a 45
        Assert.Equal(280m, arroz.Costo);      // 8 LB a 35
        Assert.Equal(80m, arroz.Margen);

        var refresco = resumen.PorProducto.Single(p => p.Id == dia.RefrescoId);
        Assert.Equal(20m, refresco.Margen);   // 2 x (35 - 25)
    }

    [Fact]
    public async Task La_rentabilidad_tambien_se_agrupa_por_categoria()
    {
        var dia = await UnDiaDeOperacion();

        var resumen = await dia.Comercio.Rentabilidad.ObtenerAsync(dia.Periodo);

        Assert.Equal(2, resumen.PorCategoria.Count);
        Assert.Equal(80m, resumen.PorCategoria.Single(c => c.Nombre == "Viveres").Margen);
        Assert.Equal(20m, resumen.PorCategoria.Single(c => c.Nombre == "Bebidas").Margen);
    }

    [Fact]
    public async Task El_reporte_de_ingresos_separa_lo_cobrado_de_lo_fiado()
    {
        var dia = await UnDiaDeOperacion();

        var ingresos = await dia.Comercio.Ingresos.ObtenerAsync(
            new FiltroReporteIngresos(dia.Periodo.Desde, dia.Periodo.Hasta));

        Assert.Equal(2, ingresos.Cantidad);
        Assert.Equal(217.60m, ingresos.Contado.Total);
        Assert.Equal(225m, ingresos.Credito.Total);
        Assert.Equal(12.60m, ingresos.Itbis);
    }

    // ---------- Lo que el sistema no deja hacer ----------

    /// <summary>
    /// Vender mas de lo que hay deja el inventario en negativo, que es la forma en que un
    /// descuadre se vuelve invisible. La venta se rechaza entera.
    /// </summary>
    [Fact]
    public async Task No_se_puede_vender_mas_arroz_del_que_queda()
    {
        var dia = await UnDiaDeOperacion();
        var arroz = dia.Comercio.Datos.Productos[dia.ArrozId];
        var existenciaAntes = arroz.Existencia;

        var resultado = await dia.Comercio.Ventas.EmitirAsync(new VentaFormDto
        {
            Condicion = CondicionPago.Contado,
            MontoRecibido = 5000m,
            Lineas = [LineaDeVenta(dia.ArrozId, "ARR-LB", "Arroz selecto", "LB", true, 99m, 45m, 0m)]
        }, Cajero);

        Assert.True(resultado.Fallo);
        Assert.Equal(existenciaAntes, arroz.Existencia);
    }

    /// <summary>
    /// El limite de credito es el que sustituye al cuaderno de fiados: pasado el techo,
    /// el sistema no deja seguir fiando.
    /// </summary>
    [Fact]
    public async Task No_se_puede_fiar_por_encima_del_limite_de_credito()
    {
        var dia = await UnDiaDeOperacion();
        var cliente = dia.Comercio.Datos.Clientes[dia.ClienteId];
        cliente.LimiteCredito = 200m;   // ya debe 125

        var resultado = await dia.Comercio.Ventas.EmitirAsync(new VentaFormDto
        {
            ClienteId = dia.ClienteId,
            Condicion = CondicionPago.Credito,
            Lineas = [LineaDeVenta(dia.ArrozId, "ARR-LB", "Arroz selecto", "LB", true, 4m, 45m, 0m)]
        }, Cajero);

        Assert.True(resultado.Fallo);
        Assert.Equal(125m, cliente.BalanceActual);
    }
}
