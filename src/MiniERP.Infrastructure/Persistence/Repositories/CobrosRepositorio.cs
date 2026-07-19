using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Clientes.Contracts;
using MiniERP.Domain.Clientes;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class CobrosRepositorio(MiniErpDbContext contexto) : ICobrosRepositorio
{
    public Task<Cobro?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Cobros
            .Include(c => c.Cliente)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<IReadOnlyList<Cobro>> ObtenerPorClienteAsync(int clienteId, CancellationToken ct = default) =>
        await contexto.Cobros
            .AsNoTracking()
            .Include(c => c.Cliente)
            .Where(c => c.ClienteId == clienteId)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync(ct);

    public void Agregar(Cobro cobro) => contexto.Cobros.Add(cobro);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);
}
