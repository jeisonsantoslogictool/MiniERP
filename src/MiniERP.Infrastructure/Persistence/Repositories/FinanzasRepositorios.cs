using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Common;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;

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
                contexto.Users
                    .Where(u => u.Id == e.UsuarioId)
                    .Select(u => u.Email)
                    .FirstOrDefault()))
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
