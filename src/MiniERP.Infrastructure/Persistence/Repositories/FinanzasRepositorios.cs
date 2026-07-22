using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Common;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;
using MiniERP.Domain.Ventas;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class EgresoRepositorio(MiniErpDbContext contexto) : IEgresoRepositorio
{
    public Task<Egreso?> ObtenerAsync(int id, CancellationToken ct = default) =>
        contexto.Egresos.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<PaginaDe<EgresoListaDto>> BuscarAsync(FiltroEgresos filtro, CancellationToken ct = default)
    {
        var consulta = AplicarFiltro(contexto.Egresos.AsNoTracking(), filtro);

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(e => e.Fecha)
            .ThenByDescending(e => e.Id)
            .Skip(filtro.SaltarRegistros)
            .Take(filtro.TamanoEfectivo)
            .Select(e => new EgresoListaDto(
                e.Id,
                e.Fecha,
                e.CategoriaEgresoId,
                e.CategoriaEgreso!.Nombre,
                e.Monto,
                e.Descripcion,
                e.UsuarioId))
            .ToListAsync(ct);

        return new PaginaDe<EgresoListaDto>(items, total, filtro.Pagina, filtro.TamanoEfectivo);
    }

    public Task<EgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default) =>
        contexto.Egresos
            .AsNoTracking()
            .Where(e => e.Id == id)
            .Select(e => new EgresoFormDto
            {
                Id = e.Id,
                Fecha = e.Fecha,
                CategoriaEgresoId = e.CategoriaEgresoId,
                Monto = e.Monto,
                Descripcion = e.Descripcion
            })
            .FirstOrDefaultAsync(ct);

    public async Task<decimal> ObtenerTotalAsync(FiltroEgresos filtro, CancellationToken ct = default)
    {
        var consulta = AplicarFiltro(contexto.Egresos.AsNoTracking(), filtro);

        return await consulta.SumAsync(e => (decimal?)e.Monto, ct) ?? 0m;
    }

    public void Agregar(Egreso egreso) => contexto.Egresos.Add(egreso);

    public void Eliminar(Egreso egreso) => contexto.Egresos.Remove(egreso);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);

    private static IQueryable<Egreso> AplicarFiltro(IQueryable<Egreso> consulta, FiltroEgresos filtro)
    {
        if (filtro.Desde is { } desde)
            consulta = consulta.Where(e => e.Fecha >= desde);

        if (filtro.Hasta is { } hasta)
            consulta = consulta.Where(e => e.Fecha <= hasta);

        if (filtro.CategoriaEgresoId is { } categoriaId)
            consulta = consulta.Where(e => e.CategoriaEgresoId == categoriaId);

        return consulta;
    }
}

public class CategoriaEgresoRepositorio(MiniErpDbContext contexto) : ICategoriaEgresoRepositorio
{
    public Task<CategoriaEgreso?> ObtenerAsync(int id, CancellationToken ct = default) =>
        contexto.CategoriasEgreso.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PaginaDe<CategoriaEgresoListaDto>> BuscarAsync(FiltroCategoriasEgreso filtro, CancellationToken ct = default)
    {
        var consulta = contexto.CategoriasEgreso.AsNoTracking();

        if (filtro.SoloActivas)
            consulta = consulta.Where(c => c.Activo);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();
            consulta = consulta.Where(c => c.Nombre.Contains(texto));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(c => c.Nombre)
            .Skip(filtro.SaltarRegistros)
            .Take(filtro.TamanoEfectivo)
            .Select(c => new CategoriaEgresoListaDto(
                c.Id,
                c.Nombre,
                c.Descripcion,
                c.Activo,
                contexto.Egresos.Count(e => e.CategoriaEgresoId == c.Id)))
            .ToListAsync(ct);

        return new PaginaDe<CategoriaEgresoListaDto>(items, total, filtro.Pagina, filtro.TamanoEfectivo);
    }

    public async Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
        await contexto.CategoriasEgreso
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new OpcionDto(c.Id, c.Nombre))
            .ToListAsync(ct);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null, CancellationToken ct = default) =>
        contexto.CategoriasEgreso
            .AsNoTracking()
            .AnyAsync(c => c.Nombre == nombre && (excluirId == null || c.Id != excluirId), ct);

    public void Agregar(CategoriaEgreso categoria) => contexto.CategoriasEgreso.Add(categoria);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);
}

public class ReporteIngresosRepositorio(MiniErpDbContext contexto) : IReporteIngresosRepositorio
{
    public async Task<IReadOnlyList<Factura>> ObtenerFacturasDelPeriodoAsync(
        DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        // El "hasta" incluye el dia completo: hasta el 31 son las ventas del 31 a cualquier hora.
        var finDelDia = hasta.Date.AddDays(1);

        return await contexto.Facturas
            .AsNoTracking()
            .Where(f => f.Fecha >= desde && f.Fecha < finDelDia)
            .ToListAsync(ct);
    }
}

public class ReporteRentabilidadRepositorio(MiniErpDbContext contexto) : IReporteRentabilidadRepositorio
{
    public async Task<IReadOnlyList<RenglonVendido>> ObtenerRenglonesDelPeriodoAsync(
        DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        var finDelDia = hasta.Date.AddDays(1);

        return await contexto.LineasFactura
            .AsNoTracking()
            .Where(l => l.Factura!.Fecha >= desde && l.Factura.Fecha < finDelDia)
            .Select(l => new RenglonVendido(
                l.Factura!.Estado == EstadoFactura.Anulada,
                l.ProductoId,
                l.Producto!.Descripcion,
                l.Producto.CategoriaId,
                l.Producto.Categoria!.Nombre,
                l.Cantidad * l.PrecioUnitario,
                l.Cantidad * l.CostoUnitario))   // costo CONGELADO de la linea, no Producto.Costo
            .ToListAsync(ct);
    }
}

public class FlujoCajaRepositorio(MiniErpDbContext contexto) : IFlujoCajaRepositorio
{
    public async Task<decimal> SumarCobrosAsync(DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        var finDelDia = hasta.Date.AddDays(1);

        return await contexto.Cobros
            .AsNoTracking()
            .Where(c => c.Fecha >= desde && c.Fecha < finDelDia)
            .SumAsync(c => (decimal?)c.Monto, ct) ?? 0m;
    }

    public async Task<decimal> SumarPagosAsync(DateTime desde, DateTime hasta, CancellationToken ct = default)
    {
        var finDelDia = hasta.Date.AddDays(1);

        return await contexto.Pagos
            .AsNoTracking()
            .Where(p => p.Fecha >= desde && p.Fecha < finDelDia)
            .SumAsync(p => (decimal?)p.Monto, ct) ?? 0m;
    }
}
