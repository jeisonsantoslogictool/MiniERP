using MiniERP.Application.Common;
using MiniERP.Domain.Clientes;

namespace MiniERP.Application.Clientes.Contracts;

public interface ICobrosRepositorio
{
    Task<Cobro?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    
    Task<IReadOnlyList<Cobro>> ObtenerPorClienteAsync(int clienteId, CancellationToken ct = default);
    
    void Agregar(Cobro cobro);
    
    Task<int> GuardarAsync(CancellationToken ct = default);
}
