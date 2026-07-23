using MiniERP.Application.Common;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Domain.Compras;

namespace MiniERP.Application.Compras.Services;

public interface IDevolucionCompraService
{
    Task<PaginaDe<DevolucionCompraListaDto>> BuscarAsync(FiltroDevoluciones filtro, CancellationToken ct = default);

    /// <summary>Arma una devolucion en blanco a partir de una compra recibida.</summary>
    Task<Resultado<DevolucionCompraFormDto>> NuevaDesdeCompraAsync(int compraId, CancellationToken ct = default);

    Task<DevolucionCompraFormDto?> ObtenerParaVerAsync(int id, CancellationToken ct = default);

    Task<Resultado<int>> GuardarAsync(DevolucionCompraFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> ConfirmarAsync(int devolucionId, string? usuarioId, CancellationToken ct = default);
}

/// <summary>
/// Casos de uso de la devolucion a proveedor.
/// </summary>
public class DevolucionCompraService(IDevolucionCompraRepositorio devoluciones) : IDevolucionCompraService
{
    public Task<PaginaDe<DevolucionCompraListaDto>> BuscarAsync(FiltroDevoluciones filtro, CancellationToken ct = default) =>
        devoluciones.BuscarAsync(filtro, ct);

    public Task<DevolucionCompraFormDto?> ObtenerParaVerAsync(int id, CancellationToken ct = default) =>
        devoluciones.ObtenerParaVerAsync(id, ct);

    /// <summary>
    /// Toma una compra recibida y arma la devolucion: sus lineas con el tope aun devolvible
    /// (lo comprado menos lo ya devuelto). Descarta las lineas sin nada por devolver.
    /// </summary>
    public async Task<Resultado<DevolucionCompraFormDto>> NuevaDesdeCompraAsync(int compraId, CancellationToken ct = default)
    {
        var form = await devoluciones.ObtenerCompraParaDevolverAsync(compraId, ct);

        if (form is null)
            return Resultado.Falla<DevolucionCompraFormDto>("La compra no existe o no está recibida.");

        var yaDevuelto = await devoluciones.ObtenerDevueltoPorLineaCompraAsync(compraId, ct);

        foreach (var linea in form.Lineas)
            linea.Devolvible = linea.CantidadComprada - yaDevuelto.GetValueOrDefault(linea.LineaCompraId);

        // Solo tiene sentido ofrecer lo que aun queda por devolver.
        form.Lineas = form.Lineas.Where(l => l.Devolvible > 0).ToList();

        if (form.Lineas.Count == 0)
            return Resultado.Falla<DevolucionCompraFormDto>("No queda mercancía por devolver de esta compra.");

        form.Numero = await devoluciones.SugerirNumeroAsync(ct);

        return Resultado.Ok(form);
    }

    public async Task<Resultado<int>> GuardarAsync(DevolucionCompraFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var validacion = Validar(form);

        if (validacion.Fallo)
            return Resultado.Falla<int>(validacion.Error!);

        var devolucion = form.Id == 0
            ? null
            : await devoluciones.ObtenerConLineasAsync(form.Id, ct);

        if (form.Id != 0 && devolucion is null)
            return Resultado.Falla<int>("La devolución no existe.");

        if (devolucion is not null && !devolucion.EsEditable)
            return Resultado.Falla<int>(
                $"La devolución {devolucion.Numero} está {devolucion.Estado.ToString().ToLowerInvariant()} y no se puede modificar.");

        if (devolucion is null)
        {
            devolucion = new DevolucionCompra { CreadoPor = usuarioId };
            devoluciones.Agregar(devolucion);
        }
        else
        {
            // Se reemplazan las lineas en bloque: es un borrador, no vale la pena
            // reconciliar renglon por renglon para ahorrar unos INSERT.
            devoluciones.EliminarLineas(devolucion.Lineas.ToList());
            devolucion.Lineas.Clear();
            devolucion.FechaModificacion = DateTime.UtcNow;
            devolucion.ModificadoPor = usuarioId;
        }

        devolucion.Numero = form.Numero.Trim().ToUpperInvariant();
        devolucion.CompraId = form.CompraId;
        devolucion.ProveedorId = form.ProveedorId;
        devolucion.Fecha = form.Fecha;
        devolucion.Motivo = form.Motivo.Trim();

        foreach (var linea in form.Lineas.Where(l => l.Cantidad > 0))
        {
            devolucion.Lineas.Add(new LineaDevolucionCompra
            {
                LineaCompraId = linea.LineaCompraId,
                ProductoId = linea.ProductoId,
                Descripcion = linea.Descripcion,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                TasaItbis = linea.TasaItbis,
                CreadoPor = usuarioId
            });
        }

        devolucion.Recalcular();

        await devoluciones.GuardarAsync(ct);

        return Resultado.Ok(devolucion.Id);
    }

    /// <summary>
    /// Confirma la devolucion: mueve el inventario y persiste todo en un solo SaveChanges,
    /// para que un fallo a mitad no deje el documento confirmado sin haber bajado la existencia,
    /// ni al reves.
    /// </summary>
    public async Task<Resultado> ConfirmarAsync(int devolucionId, string? usuarioId, CancellationToken ct = default)
    {
        var devolucion = await devoluciones.ObtenerConLineasAsync(devolucionId, ct);

        if (devolucion is null)
            return Resultado.Falla("La devolución no existe.");

        var productos = await devoluciones.ObtenerProductosDeAsync(devolucion, ct);
        var comprado = await devoluciones.ObtenerCompradoPorLineaCompraAsync(devolucion.CompraId, ct);
        var yaDevuelto = await devoluciones.ObtenerDevueltoPorLineaCompraAsync(devolucion.CompraId, ct);

        // Cuanto se puede devolver por linea = lo comprado menos lo ya devuelto (confirmado).
        var devolvibleMaximo = comprado.ToDictionary(
            par => par.Key,
            par => par.Value - yaDevuelto.GetValueOrDefault(par.Key));

        try
        {
            var movimientos = devolucion.Confirmar(productos, devolvibleMaximo, usuarioId);

            if (devolucion.Proveedor is null)
                return Resultado.Falla("No se encontró el proveedor de la devolución.");

            devolucion.Proveedor.AplicarDevolucion(devolucion);
            devoluciones.AgregarMovimientos(movimientos);
        }
        catch (InvalidOperationException ex)
        {
            return Resultado.Falla(ex.Message);
        }

        devolucion.FechaModificacion = DateTime.UtcNow;
        devolucion.ModificadoPor = usuarioId;

        await devoluciones.GuardarAsync(ct);

        return Resultado.Ok();
    }

    private static Resultado Validar(DevolucionCompraFormDto form)
    {
        if (form.CompraId == 0)
            return Resultado.Falla("La devolución debe referirse a una compra.");

        if (string.IsNullOrWhiteSpace(form.Motivo))
            return Resultado.Falla("El motivo de la devolución es obligatorio.");

        var lineas = form.Lineas.Where(l => l.Cantidad > 0).ToList();

        if (lineas.Count == 0)
            return Resultado.Falla("Indica una cantidad a devolver en al menos una línea.");

        foreach (var linea in lineas)
        {
            if (!linea.PermiteDecimales && linea.Cantidad != Math.Truncate(linea.Cantidad))
                return Resultado.Falla(
                    $"'{linea.Descripcion}' se maneja por {linea.UnidadMedida} y no admite decimales.");
        }

        var duplicado = lineas
            .GroupBy(l => l.LineaCompraId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicado is not null)
            return Resultado.Falla(
                $"'{duplicado.First().Descripcion}' está repetido. Suma las cantidades en una sola línea.");

        return Resultado.Ok();
    }
}
