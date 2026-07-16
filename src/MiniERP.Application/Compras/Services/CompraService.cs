using MiniERP.Application.Common;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Shared;

namespace MiniERP.Application.Compras.Services;

public interface ICompraService
{
    Task<PaginaDe<CompraListaDto>> BuscarAsync(FiltroCompras filtro, CancellationToken ct = default);

    Task<CompraFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<CompraFormDto> NuevaAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ProductoParaCompraDto>> BuscarProductosAsync(string texto, CancellationToken ct = default);

    Task<Resultado<int>> GuardarAsync(CompraFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> RecibirAsync(int compraId, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> AnularAsync(int compraId, string? usuarioId, CancellationToken ct = default);
}

/// <summary>
/// Casos de uso del modulo de compras.
/// </summary>
public class CompraService(
    ICompraRepositorio compras,
    IProveedorRepositorio proveedores) : ICompraService
{
    public Task<PaginaDe<CompraListaDto>> BuscarAsync(FiltroCompras filtro, CancellationToken ct = default) =>
        compras.BuscarAsync(filtro, ct);

    public Task<CompraFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default) =>
        compras.ObtenerParaEditarAsync(id, ct);

    public Task<IReadOnlyList<ProductoParaCompraDto>> BuscarProductosAsync(string texto, CancellationToken ct = default) =>
        compras.BuscarProductosAsync(texto, 15, ct);

    public async Task<CompraFormDto> NuevaAsync(CancellationToken ct = default) =>
        new() { Numero = await compras.SugerirNumeroAsync(ct), Fecha = DateTime.UtcNow };

    public async Task<Resultado<int>> GuardarAsync(CompraFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var validacion = await ValidarAsync(form, ct);

        if (validacion.Fallo)
            return Resultado.Falla<int>(validacion.Error!);

        var compra = form.Id == 0
            ? null
            : await compras.ObtenerConLineasAsync(form.Id, ct);

        if (form.Id != 0 && compra is null)
            return Resultado.Falla<int>("La compra no existe.");

        if (compra is not null && !compra.EsEditable)
            return Resultado.Falla<int>(
                $"La compra {compra.Numero} esta {compra.Estado.ToString().ToLowerInvariant()} y no se puede modificar.");

        if (compra is null)
        {
            compra = new Compra { CreadoPor = usuarioId };
            compras.Agregar(compra);
        }
        else
        {
            // Se reemplazan las lineas en bloque: es una orden en borrador, no vale la
            // pena reconciliar renglon por renglon para ahorrar unos INSERT.
            compras.EliminarLineas(compra.Lineas.ToList());
            compra.Lineas.Clear();
            compra.FechaModificacion = DateTime.UtcNow;
            compra.ModificadoPor = usuarioId;
        }

        compra.Numero = form.Numero.Trim().ToUpperInvariant();
        compra.ProveedorId = form.ProveedorId;
        compra.Fecha = form.Fecha;
        compra.NcfProveedor = Normalizar(form.NcfProveedor)?.ToUpperInvariant();
        compra.Condicion = form.Condicion;
        compra.Observacion = Normalizar(form.Observacion);

        foreach (var linea in form.Lineas)
        {
            compra.Lineas.Add(new LineaCompra
            {
                ProductoId = linea.ProductoId,
                Descripcion = linea.Descripcion,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                TasaItbis = linea.TasaItbis,
                CreadoPor = usuarioId
            });
        }

        compra.Recalcular();

        await compras.GuardarAsync(ct);

        return Resultado.Ok(compra.Id);
    }

    /// <summary>
    /// Recibe la mercancia: mueve el inventario, recalcula costos y, si es a credito,
    /// carga el total al balance del proveedor.
    /// </summary>
    /// <remarks>
    /// Todo se persiste en un solo SaveChanges y por tanto en una sola transaccion.
    /// Es lo que impide que el inventario suba pero el balance del proveedor no, o al
    /// reves, si algo falla a mitad de camino.
    /// </remarks>
    public async Task<Resultado> RecibirAsync(int compraId, string? usuarioId, CancellationToken ct = default)
    {
        var compra = await compras.ObtenerConLineasAsync(compraId, ct);

        if (compra is null)
            return Resultado.Falla("La compra no existe.");

        var productos = await compras.ObtenerProductosDeAsync(compra, ct);

        try
        {
            var movimientos = compra.Recibir(productos, usuarioId);
            compras.AgregarMovimientos(movimientos);
        }
        catch (InvalidOperationException ex)
        {
            return Resultado.Falla(ex.Message);
        }

        if (compra.Condicion == CondicionPago.Credito)
        {
            var proveedor = await proveedores.ObtenerPorIdAsync(compra.ProveedorId, ct);

            if (proveedor is null)
                return Resultado.Falla("El proveedor de la compra no existe.");

            proveedor.BalanceActual += compra.Total;
        }

        compra.FechaModificacion = DateTime.UtcNow;
        compra.ModificadoPor = usuarioId;

        await compras.GuardarAsync(ct);

        return Resultado.Ok();
    }

    public async Task<Resultado> AnularAsync(int compraId, string? usuarioId, CancellationToken ct = default)
    {
        var compra = await compras.ObtenerConLineasAsync(compraId, ct);

        if (compra is null)
            return Resultado.Falla("La compra no existe.");

        try
        {
            compra.Anular();
        }
        catch (InvalidOperationException ex)
        {
            return Resultado.Falla(ex.Message);
        }

        compra.FechaModificacion = DateTime.UtcNow;
        compra.ModificadoPor = usuarioId;

        await compras.GuardarAsync(ct);

        return Resultado.Ok();
    }

    private async Task<Resultado> ValidarAsync(CompraFormDto form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Numero))
            return Resultado.Falla("El numero de la compra es obligatorio.");

        if (form.ProveedorId == 0)
            return Resultado.Falla("Selecciona un proveedor.");

        if (await proveedores.ObtenerPorIdAsync(form.ProveedorId, ct) is null)
            return Resultado.Falla("El proveedor no existe.");

        if (form.Lineas.Count == 0)
            return Resultado.Falla("Agrega al menos un producto a la compra.");

        foreach (var linea in form.Lineas)
        {
            if (linea.Cantidad <= 0)
                return Resultado.Falla($"La cantidad de '{linea.Descripcion}' debe ser mayor que cero.");

            if (linea.CostoUnitario < 0)
                return Resultado.Falla($"El costo de '{linea.Descripcion}' no puede ser negativo.");

            if (!linea.PermiteDecimales && linea.Cantidad != Math.Truncate(linea.Cantidad))
                return Resultado.Falla(
                    $"'{linea.Descripcion}' se compra por {linea.UnidadMedida} y no admite decimales.");
        }

        var duplicado = form.Lineas
            .GroupBy(l => l.ProductoId)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicado is not null)
            return Resultado.Falla(
                $"'{duplicado.First().Descripcion}' esta repetido. Suma las cantidades en una sola linea.");

        return Resultado.Ok();
    }

    private static string? Normalizar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
