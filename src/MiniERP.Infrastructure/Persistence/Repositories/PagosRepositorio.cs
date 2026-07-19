using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Domain.Compras;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class PagosRepositorio(MiniErpDbContext contexto) : IPagosRepositorio
{
    public Task<Pago?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Pagos
            .Include(p => p.Proveedor)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Pago>> ObtenerPorProveedorAsync(int proveedorId, CancellationToken ct = default) =>
        await contexto.Pagos
            .AsNoTracking()
            .Include(p => p.Proveedor)
            .Where(p => p.ProveedorId == proveedorId)
            .OrderByDescending(p => p.Fecha)
            .ToListAsync(ct);

    public void Agregar(Pago pago) => contexto.Pagos.Add(pago);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);
}
