using MiniERP.Application.Clientes.Contracts;
using MiniERP.Application.Common;
using MiniERP.Application.Ventas.Contracts;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Application.Ventas.Services;

public interface IVentaService
{
    Task<IReadOnlyList<ProductoParaVentaDto>> BuscarProductosAsync(string texto, CancellationToken ct = default);

    Task<ProductoParaVentaDto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);

    Task<IReadOnlyList<ClienteParaVentaDto>> BuscarClientesAsync(string texto, CancellationToken ct = default);

    Task<PaginaDe<FacturaListaDto>> BuscarAsync(FiltroFacturas filtro, CancellationToken ct = default);

    Task<FacturaDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct = default);

    Task<Resultado<FacturaEmitidaDto>> EmitirAsync(VentaFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> AnularAsync(int facturaId, string motivo, string? usuarioId, CancellationToken ct = default);
}

/// <summary>
/// Casos de uso del punto de venta.
/// </summary>
public class VentaService(
    IVentaRepositorio ventas,
    ISecuenciaNcfRepositorio secuencias,
    IClienteRepositorio clientes) : IVentaService
{
    public Task<IReadOnlyList<ProductoParaVentaDto>> BuscarProductosAsync(string texto, CancellationToken ct = default) =>
        ventas.BuscarProductosAsync(texto, 15, ct);

    public Task<ProductoParaVentaDto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default) =>
        ventas.ObtenerPorCodigoBarrasAsync(codigoBarras, ct);

    public Task<IReadOnlyList<ClienteParaVentaDto>> BuscarClientesAsync(string texto, CancellationToken ct = default) =>
        ventas.BuscarClientesAsync(texto, 10, ct);

    public Task<PaginaDe<FacturaListaDto>> BuscarAsync(FiltroFacturas filtro, CancellationToken ct = default) =>
        ventas.BuscarAsync(filtro, ct);

    public Task<FacturaDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct = default) =>
        ventas.ObtenerDetalleAsync(id, ct);

    /// <summary>
    /// Cobra el carrito: reserva el comprobante, descuenta el inventario y, si es a
    /// credito, carga el total al balance del cliente.
    /// </summary>
    public async Task<Resultado<FacturaEmitidaDto>> EmitirAsync(
        VentaFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var validacion = ValidarCarrito(form);

        if (validacion.Fallo)
            return Resultado.Falla<FacturaEmitidaDto>(validacion.Error!);

        Cliente? cliente = null;

        if (form.ClienteId is { } clienteId)
        {
            cliente = await clientes.ObtenerPorIdAsync(clienteId, ct);

            if (cliente is null)
                return Resultado.Falla<FacturaEmitidaDto>("El cliente no existe.");

            if (!cliente.Activo)
                return Resultado.Falla<FacturaEmitidaDto>($"{cliente.Nombre} esta inactivo.");
        }

        var credito = ValidarCredito(form, cliente);

        if (credito.Fallo)
            return Resultado.Falla<FacturaEmitidaDto>(credito.Error!);

        // El comprobante de una venta sin cliente registrado es siempre de consumo:
        // no hay a quien sustentarle un credito fiscal.
        var tipoComprobante = cliente?.TipoComprobante ?? TipoComprobante.Consumo;

        return await ventas.EnTransaccionAsync(async () =>
        {
            var ncf = await secuencias.AsignarSiguienteAsync(tipoComprobante, ct);

            if (ncf is null)
            {
                var secuencia = await secuencias.ObtenerPorTipoAsync(tipoComprobante, ct);

                return Resultado.Falla<FacturaEmitidaDto>(
                    secuencia?.MotivoNoDisponible(DateTime.UtcNow)
                    ?? $"No hay secuencia de comprobantes configurada para {tipoComprobante}. " +
                       "Registra el rango que te autorizo la DGII.");
            }

            var factura = new Factura
            {
                Numero = await ventas.SugerirNumeroAsync(ct),
                TipoComprobante = tipoComprobante,
                ClienteId = cliente?.Id,
                ClienteNombre = cliente?.Nombre ?? "Cliente de contado",
                ClienteDocumento = cliente?.NumeroDocumento,
                Fecha = DateTime.UtcNow,
                Condicion = form.Condicion,
                MontoRecibido = form.Condicion == CondicionPago.Contado ? form.MontoRecibido : 0,
                CreadoPor = usuarioId
            };

            foreach (var linea in form.Lineas)
            {
                factura.Lineas.Add(new LineaFactura
                {
                    ProductoId = linea.ProductoId,
                    Descripcion = linea.Descripcion,
                    Cantidad = linea.Cantidad,
                    PrecioUnitario = linea.PrecioUnitario,
                    TasaItbis = linea.TasaItbis,
                    CreadoPor = usuarioId
                });
            }

            var productos = await ventas.ObtenerProductosAsync(
                form.Lineas.Select(l => l.ProductoId), ct);

            IReadOnlyList<Domain.Inventario.MovimientoInventario> movimientos;

            try
            {
                movimientos = factura.Emitir(productos, ncf, usuarioId);
            }
            catch (InvalidOperationException ex)
            {
                // La transaccion revierte: el comprobante reservado se libera y el
                // inventario queda como estaba.
                return Resultado.Falla<FacturaEmitidaDto>(ex.Message);
            }

            ventas.Agregar(factura);

            if (form.Condicion == CondicionPago.Credito && cliente is not null)
                cliente.BalanceActual += factura.Total;

            // Son dos guardados a proposito, y este es el orden que importa. Los
            // movimientos tienen que decir de que factura salieron, y ese Id no existe
            // hasta que la fila esta escrita. Guardar primero el documento y enlazarlos
            // despues es lo unico que evita grabar un cero: si no, "por que bajo el arroz
            // el martes" solo se responde leyendo el texto del motivo.
            // Los dos guardados van dentro de EnTransaccionAsync, asi que la venta sigue
            // siendo un solo hecho: o queda entera con su rastro, o no queda nada.
            await ventas.GuardarAsync(ct);

            foreach (var movimiento in movimientos)
                movimiento.ReferenciaId = factura.Id;

            ventas.AgregarMovimientos(movimientos);
            await ventas.GuardarAsync(ct);

            return Resultado.Ok(new FacturaEmitidaDto(
                factura.Id, factura.Numero, ncf, factura.Total, factura.Cambio));
        }, ct);
    }

    public async Task<Resultado> AnularAsync(
        int facturaId, string motivo, string? usuarioId, CancellationToken ct = default)
    {
        var factura = await ventas.ObtenerConLineasAsync(facturaId, ct);

        if (factura is null)
            return Resultado.Falla("La factura no existe.");

        return await ventas.EnTransaccionAsync(async () =>
        {
            var productos = await ventas.ObtenerProductosAsync(
                factura.Lineas.Select(l => l.ProductoId), ct);

            try
            {
                var movimientos = factura.Anular(productos, motivo, usuarioId);
                ventas.AgregarMovimientos(movimientos);
            }
            catch (InvalidOperationException ex)
            {
                return Resultado.Falla(ex.Message);
            }

            // Anular una venta a credito descarga la deuda del cliente: lo que no compro,
            // no lo debe.
            if (factura.Condicion == CondicionPago.Credito && factura.ClienteId is { } clienteId)
            {
                var cliente = await clientes.ObtenerPorIdAsync(clienteId, ct);

                if (cliente is not null)
                    cliente.BalanceActual -= factura.Total;
            }

            await ventas.GuardarAsync(ct);

            return Resultado.Ok();
        }, ct);
    }

    private static Resultado ValidarCarrito(VentaFormDto form)
    {
        if (!form.HayLineas)
            return Resultado.Falla("El carrito esta vacio.");

        foreach (var linea in form.Lineas)
        {
            if (linea.Cantidad <= 0)
                return Resultado.Falla($"La cantidad de '{linea.Descripcion}' debe ser mayor que cero.");

            if (!linea.PermiteDecimales && linea.Cantidad != Math.Truncate(linea.Cantidad))
                return Resultado.Falla(
                    $"'{linea.Descripcion}' se vende por {linea.UnidadMedida} y no admite decimales.");

            if (linea.PrecioUnitario < 0)
                return Resultado.Falla($"El precio de '{linea.Descripcion}' no puede ser negativo.");
        }

        var duplicado = form.Lineas
            .GroupBy(l => l.ProductoId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicado is not null)
            return Resultado.Falla(
                $"'{duplicado.First().Descripcion}' esta repetido. Suma las cantidades en una sola linea.");

        return Resultado.Ok();
    }

    private static Resultado ValidarCredito(VentaFormDto form, Cliente? cliente)
    {
        if (form.Condicion == CondicionPago.Contado)
        {
            return form.MontoRecibido < form.Total
                ? Resultado.Falla(
                    $"Faltan {form.Faltante:N2} para cubrir el total de {form.Total:N2}.")
                : Resultado.Ok();
        }

        if (cliente is null)
            return Resultado.Falla(
                "Una venta a credito necesita un cliente registrado: no se le fia a un desconocido.");

        if (!cliente.TieneCredito)
            return Resultado.Falla($"{cliente.Nombre} es cliente de contado, no tiene credito aprobado.");

        if (!cliente.PuedeAsumirCredito(form.Total))
            return Resultado.Falla(
                $"{cliente.Nombre} tiene {cliente.CreditoDisponible:N2} disponible " +
                $"y esta venta es de {form.Total:N2}. Debe {cliente.BalanceActual:N2} " +
                $"de un limite de {cliente.LimiteCredito:N2}.");

        return Resultado.Ok();
    }
}
