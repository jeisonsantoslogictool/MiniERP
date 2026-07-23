using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Common;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class DevolucionCompraRepositorio(MiniErpDbContext contexto)
    : IDevolucionCompraRepositorio
{
    public Task<DevolucionCompra?> ObtenerConLineasAsync(int id, CancellationToken ct = default) =>
        contexto.DevolucionesCompra
            .Include(d => d.Lineas)
            .Include(d => d.Proveedor)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<PaginaDe<DevolucionCompraListaDto>> BuscarAsync(
        FiltroDevoluciones filtro, CancellationToken ct = default)
    {
        var consulta = contexto.DevolucionesCompra.AsNoTracking();

        if (filtro.ProveedorId is { } proveedorId)
            consulta = consulta.Where(d => d.ProveedorId == proveedorId);

        if (filtro.Estado is { } estado)
            consulta = consulta.Where(d => d.Estado == estado);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(d =>
                d.Numero.Contains(texto) ||
                d.Compra!.Numero.Contains(texto) ||
                d.Proveedor!.Nombre.Contains(texto) ||
                d.Motivo.Contains(texto));
        }

        var total = await consulta.CountAsync(ct);
        var items = await consulta
            .OrderByDescending(d => d.Fecha)
            .ThenByDescending(d => d.Id)
            .Skip(filtro.SaltarRegistros)
            .Take(filtro.TamanoEfectivo)
            .Select(d => new DevolucionCompraListaDto(
                d.Id, d.Numero, d.Proveedor!.Nombre, d.Compra!.Numero, d.Fecha,
                d.Estado, d.Motivo, d.Total, d.Lineas.Count))
            .ToListAsync(ct);

        return new PaginaDe<DevolucionCompraListaDto>(
            items, total, filtro.Pagina, filtro.TamanoEfectivo);
    }

    public async Task<DevolucionCompraFormDto?> ObtenerParaVerAsync(
        int id, CancellationToken ct = default)
    {
        var devolucion = await contexto.DevolucionesCompra
            .AsNoTracking()
            .Include(d => d.Compra)
            .Include(d => d.Proveedor)
            .Include(d => d.Lineas)
                .ThenInclude(l => l.Producto)
                    .ThenInclude(p => p!.UnidadMedida)
            .FirstOrDefaultAsync(d => d.Id == id, ct);

        if (devolucion is null)
            return null;

        var compradas = await ObtenerCompradoPorLineaCompraAsync(devolucion.CompraId, ct);
        var devueltas = await ObtenerDevueltoPorLineaCompraAsync(devolucion.CompraId, ct);

        return new DevolucionCompraFormDto
        {
            Id = devolucion.Id,
            Numero = devolucion.Numero,
            CompraId = devolucion.CompraId,
            CompraNumero = devolucion.Compra?.Numero ?? string.Empty,
            ProveedorId = devolucion.ProveedorId,
            ProveedorNombre = devolucion.Proveedor?.Nombre,
            Fecha = devolucion.Fecha,
            Motivo = devolucion.Motivo,
            Estado = devolucion.Estado,
            Lineas = [.. devolucion.Lineas.Select(l => new LineaDevolucionFormDto
            {
                Id = l.Id,
                LineaCompraId = l.LineaCompraId,
                ProductoId = l.ProductoId,
                Codigo = l.Producto?.Codigo ?? string.Empty,
                Descripcion = l.Descripcion,
                UnidadMedida = l.Producto?.UnidadMedida?.Codigo ?? string.Empty,
                PermiteDecimales = l.Producto?.UnidadMedida?.PermiteDecimales ?? false,
                CantidadComprada = compradas.GetValueOrDefault(l.LineaCompraId),
                Devolvible = devolucion.Estado == EstadoDevolucion.Confirmada
                    ? l.Cantidad
                    : compradas.GetValueOrDefault(l.LineaCompraId) -
                      devueltas.GetValueOrDefault(l.LineaCompraId),
                Cantidad = l.Cantidad,
                CostoUnitario = l.CostoUnitario,
                TasaItbis = l.TasaItbis
            })]
        };
    }

    public async Task<DevolucionCompraFormDto?> ObtenerCompraParaDevolverAsync(
        int compraId, CancellationToken ct = default)
    {
        var compra = await contexto.Compras
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Include(c => c.Lineas)
                .ThenInclude(l => l.Producto)
                    .ThenInclude(p => p!.UnidadMedida)
            .FirstOrDefaultAsync(c => c.Id == compraId && c.Estado == EstadoCompra.Recibida, ct);

        if (compra is null)
            return null;

        return new DevolucionCompraFormDto
        {
            CompraId = compra.Id,
            CompraNumero = compra.Numero,
            ProveedorId = compra.ProveedorId,
            ProveedorNombre = compra.Proveedor?.Nombre,
            Fecha = DateTime.UtcNow,
            Lineas = [.. compra.Lineas.Select(l => new LineaDevolucionFormDto
            {
                LineaCompraId = l.Id,
                ProductoId = l.ProductoId,
                Codigo = l.Producto?.Codigo ?? string.Empty,
                Descripcion = l.Descripcion,
                UnidadMedida = l.Producto?.UnidadMedida?.Codigo ?? string.Empty,
                PermiteDecimales = l.Producto?.UnidadMedida?.PermiteDecimales ?? false,
                CantidadComprada = l.Cantidad,
                Cantidad = 0,
                CostoUnitario = l.CostoUnitario,
                TasaItbis = l.TasaItbis
            })]
        };
    }

    public async Task<IReadOnlyDictionary<int, decimal>> ObtenerCompradoPorLineaCompraAsync(
        int compraId, CancellationToken ct = default) =>
        await contexto.LineasCompra.AsNoTracking()
            .Where(l => l.CompraId == compraId)
            .ToDictionaryAsync(l => l.Id, l => l.Cantidad, ct);

    public async Task<IReadOnlyDictionary<int, decimal>> ObtenerDevueltoPorLineaCompraAsync(
        int compraId, CancellationToken ct = default) =>
        await contexto.LineasDevolucionCompra.AsNoTracking()
            .Where(l => l.Devolucion!.CompraId == compraId &&
                        l.Devolucion.Estado == EstadoDevolucion.Confirmada)
            .GroupBy(l => l.LineaCompraId)
            .ToDictionaryAsync(g => g.Key, g => g.Sum(l => l.Cantidad), ct);

    public async Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosDeAsync(
        DevolucionCompra devolucion, CancellationToken ct = default)
    {
        var ids = devolucion.Lineas.Select(l => l.ProductoId).Distinct().ToList();
        return await contexto.Productos
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
    }

    public async Task<string> SugerirNumeroAsync(CancellationToken ct = default)
    {
        var ultimo = await contexto.DevolucionesCompra.AsNoTracking()
            .Where(d => d.Numero.StartsWith("DEV-"))
            .OrderByDescending(d => d.Id)
            .Select(d => d.Numero)
            .FirstOrDefaultAsync(ct);

        return ultimo is not null && int.TryParse(ultimo[4..], out var numero)
            ? $"DEV-{numero + 1:D4}"
            : "DEV-0001";
    }

    public void Agregar(DevolucionCompra devolucion) =>
        contexto.DevolucionesCompra.Add(devolucion);

    public void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos) =>
        contexto.MovimientosInventario.AddRange(movimientos);

    public void EliminarLineas(IEnumerable<LineaDevolucionCompra> lineas) =>
        contexto.LineasDevolucionCompra.RemoveRange(lineas);

    public Task<int> GuardarAsync(CancellationToken ct = default) =>
        contexto.SaveChangesAsync(ct);
}
