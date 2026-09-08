using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Common;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class ProveedorRepositorio(MiniErpDbContext contexto) : IProveedorRepositorio
{
    private static readonly Expression<Func<Proveedor, ProveedorListaDto>> Proyeccion =
        p => new ProveedorListaDto(
            p.Id, p.Codigo, p.Nombre, p.TipoDocumento, p.NumeroDocumento,
            p.Telefono, p.Contacto, p.DiasCredito, p.BalanceActual, p.Activo);

    public Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Proveedores.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<PaginaDe<ProveedorListaDto>> BuscarAsync(FiltroProveedores filtro, CancellationToken ct = default)
    {
        var consulta = contexto.Proveedores.AsNoTracking();

        if (filtro.SoloActivos)
            consulta = consulta.Where(p => p.Activo);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            var sinGuiones = texto.Replace("-", string.Empty);

            consulta = consulta.Where(p =>
                p.Codigo.Contains(texto) ||
                p.Nombre.Contains(texto) ||
                (p.NumeroDocumento != null && p.NumeroDocumento.Contains(sinGuiones)) ||
                (p.Contacto != null && p.Contacto.Contains(texto)));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(p => p.Nombre)
            .Skip(filtro.SaltarRegistros)
            .Take(filtro.TamanoEfectivo)
            .Select(Proyeccion)
            .ToListAsync(ct);

        return new PaginaDe<ProveedorListaDto>(items, total, filtro.Pagina, filtro.TamanoEfectivo);
    }

    public async Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
        await contexto.Proveedores
            .AsNoTracking()
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .Select(p => new OpcionDto(p.Id, p.Nombre))
            .ToListAsync(ct);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
        contexto.Proveedores.AsNoTracking()
            .AnyAsync(p => p.Codigo == codigo && (excluirId == null || p.Id != excluirId), ct);

    public Task<bool> ExisteDocumentoAsync(string numeroDocumento, int? excluirId = null, CancellationToken ct = default) =>
        contexto.Proveedores.AsNoTracking()
            .AnyAsync(p => p.NumeroDocumento == numeroDocumento && (excluirId == null || p.Id != excluirId), ct);

    public async Task<string> SugerirCodigoAsync(CancellationToken ct = default)
    {
        var ultimo = await contexto.Proveedores
            .AsNoTracking()
            .Where(p => p.Codigo.StartsWith("PRV-"))
            .OrderByDescending(p => p.Id)
            .Select(p => p.Codigo)
            .FirstOrDefaultAsync(ct);

        return ultimo is not null && int.TryParse(ultimo[4..], out var numero)
            ? $"PRV-{numero + 1:D4}"
            : "PRV-0001";
    }

    public void Agregar(Proveedor proveedor) => contexto.Proveedores.Add(proveedor);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<CuentasPorPagarDto>> ObtenerCuentasPorPagarAsync(CancellationToken ct = default)
    {
        var proveedoresConDeuda = await contexto.Proveedores
            .AsNoTracking()
            .Where(p => p.Activo && p.BalanceActual > 0)
            .Select(p => new
            {
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Contacto,
                p.Telefono,
                p.BalanceActual,
                p.DiasCredito
            })
            .ToListAsync(ct);

        if (proveedoresConDeuda.Count == 0)
            return [];

        var ids = proveedoresConDeuda.Select(p => p.Id).ToList();

        var compras = await contexto.Compras
            .AsNoTracking()
            .Where(c => ids.Contains(c.ProveedorId) && c.Condicion == Domain.Shared.CondicionPago.Credito && c.Estado == Domain.Compras.EstadoCompra.Recibida)
            .Select(c => new { c.ProveedorId, c.FechaRecepcion, c.Fecha, c.Total })
            .ToListAsync(ct);

        var pagos = await contexto.Pagos
            .AsNoTracking()
            .Where(p => ids.Contains(p.ProveedorId))
            .GroupBy(p => p.ProveedorId)
            .Select(g => new { ProveedorId = g.Key, TotalPagado = g.Sum(p => p.Monto) })
            .ToListAsync(ct);

        var comprasPorProveedor = compras.GroupBy(c => c.ProveedorId).ToDictionary(g => g.Key, g => g.OrderBy(c => c.FechaRecepcion ?? c.Fecha).ToList());
        var pagosPorProveedor = pagos.ToDictionary(p => p.ProveedorId, p => p.TotalPagado);

        var resultado = new List<CuentasPorPagarDto>(proveedoresConDeuda.Count);
        var fechaActual = DateTime.UtcNow.Date;

        foreach (var p in proveedoresConDeuda)
        {
            decimal totalPagado = pagosPorProveedor.TryGetValue(p.Id, out var tp) ? tp : 0m;
            int diasVencidos = 0;
            decimal montoVencido = 0m;

            if (comprasPorProveedor.TryGetValue(p.Id, out var comprasProveedor))
            {
                foreach (var c in comprasProveedor)
                {
                    if (totalPagado >= c.Total)
                    {
                        totalPagado -= c.Total;
                        continue;
                    }

                    var montoPendiente = c.Total - totalPagado;
                    totalPagado = 0;

                    var fechaReferencia = (c.FechaRecepcion ?? c.Fecha).Date;
                    var fechaVencimiento = fechaReferencia.AddDays(p.DiasCredito);
                    var dias = (fechaActual - fechaVencimiento).Days;

                    if (dias > 0)
                    {
                        montoVencido += montoPendiente;
                        if (dias > diasVencidos)
                            diasVencidos = dias;
                    }
                }
            }

            resultado.Add(new CuentasPorPagarDto(
                p.Id,
                p.Codigo,
                p.Nombre,
                p.Contacto,
                p.Telefono,
                p.BalanceActual,
                p.DiasCredito,
                diasVencidos,
                montoVencido
            ));
        }

        return resultado.OrderByDescending(r => r.MontoVencido).ThenByDescending(r => r.DiasVencidos).ThenBy(r => r.Nombre).ToList();
    }
}

public class CompraRepositorio(MiniErpDbContext contexto) : ICompraRepositorio
{
    private static readonly Expression<Func<Compra, CompraListaDto>> Proyeccion =
        c => new CompraListaDto(
            c.Id, c.Numero, c.Proveedor!.Nombre, c.Fecha, c.FechaRecepcion,
            c.NcfProveedor, c.Estado, c.Condicion,
            c.Subtotal, c.Itbis, c.Total, c.Lineas.Count);

    /// <summary>
    /// Con seguimiento y con las lineas cargadas: recibir necesita modificar la compra
    /// y sus lineas, no solo leerlas.
    /// </summary>
    public Task<Compra?> ObtenerConLineasAsync(int id, CancellationToken ct = default) =>
        contexto.Compras
            .Include(c => c.Lineas)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PaginaDe<CompraListaDto>> BuscarAsync(FiltroCompras filtro, CancellationToken ct = default)
    {
        var consulta = contexto.Compras.AsNoTracking();

        if (filtro.ProveedorId is { } proveedorId)
            consulta = consulta.Where(c => c.ProveedorId == proveedorId);

        if (filtro.Estado is { } estado)
            consulta = consulta.Where(c => c.Estado == estado);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();

            consulta = consulta.Where(c =>
                c.Numero.Contains(texto) ||
                c.Proveedor!.Nombre.Contains(texto) ||
                (c.NcfProveedor != null && c.NcfProveedor.Contains(texto)));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(c => c.Fecha)
            .ThenByDescending(c => c.Id)
            .Skip(filtro.SaltarRegistros)
            .Take(filtro.TamanoEfectivo)
            .Select(Proyeccion)
            .ToListAsync(ct);

        return new PaginaDe<CompraListaDto>(items, total, filtro.Pagina, filtro.TamanoEfectivo);
    }

    public async Task<CompraFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var compra = await contexto.Compras
            .AsNoTracking()
            .Include(c => c.Proveedor)
            .Include(c => c.Lineas)
                .ThenInclude(l => l.Producto)
                    .ThenInclude(p => p!.UnidadMedida)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

        if (compra is null)
            return null;

        return new CompraFormDto
        {
            Id = compra.Id,
            Numero = compra.Numero,
            ProveedorId = compra.ProveedorId,
            ProveedorNombre = compra.Proveedor?.Nombre,
            Fecha = compra.Fecha,
            NcfProveedor = compra.NcfProveedor,
            Estado = compra.Estado,
            Condicion = compra.Condicion,
            Observacion = compra.Observacion,
            Lineas = [.. compra.Lineas.Select(l => new LineaCompraFormDto
            {
                Id = l.Id,
                ProductoId = l.ProductoId,
                Codigo = l.Producto?.Codigo ?? string.Empty,
                Descripcion = l.Descripcion,
                UnidadMedida = l.Producto?.UnidadMedida?.Codigo ?? string.Empty,
                PermiteDecimales = l.Producto?.UnidadMedida?.PermiteDecimales ?? false,
                Cantidad = l.Cantidad,
                CostoUnitario = l.CostoUnitario,
                TasaItbis = l.TasaItbis
            })]
        };
    }

    public async Task<string> SugerirNumeroAsync(CancellationToken ct = default)
    {
        var ultimo = await contexto.Compras
            .AsNoTracking()
            .Where(c => c.Numero.StartsWith("COM-"))
            .OrderByDescending(c => c.Id)
            .Select(c => c.Numero)
            .FirstOrDefaultAsync(ct);

        return ultimo is not null && int.TryParse(ultimo[4..], out var numero)
            ? $"COM-{numero + 1:D4}"
            : "COM-0001";
    }

    public async Task<IReadOnlyList<ProductoParaCompraDto>> BuscarProductosAsync(string texto, int maximo = 15, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return [];

        texto = texto.Trim();

        return await contexto.Productos
            .AsNoTracking()
            .Where(p => p.Activo && (
                p.Codigo.Contains(texto) ||
                p.Descripcion.Contains(texto) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(texto))))
            .OrderBy(p => p.Descripcion)
            .Take(maximo)
            .Select(p => new ProductoParaCompraDto(
                p.Id, p.Codigo, p.Descripcion,
                p.UnidadMedida!.Codigo, p.UnidadMedida!.PermiteDecimales,
                p.Costo, p.TasaItbis, p.Existencia))
            .ToListAsync(ct);
    }

    /// <summary>
    /// Trae con seguimiento los productos de las lineas. Recibir los modifica (existencia
    /// y costo), asi que no pueden venir con AsNoTracking.
    /// </summary>
    public async Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosDeAsync(Compra compra, CancellationToken ct = default)
    {
        var ids = compra.Lineas.Select(l => l.ProductoId).Distinct().ToList();

        // El mismo cuidado que en la venta, en la direccion contraria: recibir mercancia
        // promedia el costo y suma existencia sobre lo que diga esta instancia. Si viene
        // cacheada del circuito, la recepcion pisa las ventas que el cajero hizo entre medio.
        foreach (var seguido in contexto.ChangeTracker.Entries<Producto>()
                     .Where(e => ids.Contains(e.Entity.Id))
                     .ToList())
        {
            await seguido.ReloadAsync(ct);
        }

        return await contexto.Productos
            .Where(p => ids.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
    }

    public void Agregar(Compra compra) => contexto.Compras.Add(compra);

    public void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos) =>
        contexto.MovimientosInventario.AddRange(movimientos);

    public void EliminarLineas(IEnumerable<LineaCompra> lineas) =>
        contexto.LineasCompra.RemoveRange(lineas);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);
}
