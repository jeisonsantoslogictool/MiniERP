using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Common;
using MiniERP.Application.Inventario.Contracts;
using MiniERP.Application.Inventario.Dtos;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class CategoriaRepositorio(MiniErpDbContext contexto) : ICategoriaRepositorio
{
    public Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Categorias.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Categoria>> ObtenerTodasAsync(bool soloActivas = false, CancellationToken ct = default)
    {
        var consulta = contexto.Categorias.AsNoTracking();

        if (soloActivas)
            consulta = consulta.Where(c => c.Activo);

        return await consulta.OrderBy(c => c.Nombre).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
        await contexto.Categorias
            .AsNoTracking()
            .Where(c => c.Activo)
            .OrderBy(c => c.Nombre)
            .Select(c => new OpcionDto(c.Id, c.Nombre))
            .ToListAsync(ct);

    public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null, CancellationToken ct = default) =>
        contexto.Categorias
            .AsNoTracking()
            .AnyAsync(c => c.Nombre == nombre && (excluirId == null || c.Id != excluirId), ct);

    public Task<int> ContarProductosAsync(int categoriaId, CancellationToken ct = default) =>
        contexto.Productos.AsNoTracking().CountAsync(p => p.CategoriaId == categoriaId, ct);

    public void Agregar(Categoria categoria) => contexto.Categorias.Add(categoria);

    public void Eliminar(Categoria categoria) => contexto.Categorias.Remove(categoria);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);
}
