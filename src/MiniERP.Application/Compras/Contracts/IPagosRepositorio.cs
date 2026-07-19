using MiniERP.Domain.Compras;

namespace MiniERP.Application.Compras.Contracts;

public interface IPagosRepositorio
{
    Task<Pago?> ObtenerPorIdAsync(int id, CancellationToken ct = default);
    
    Task<IReadOnlyList<Pago>> ObtenerPorProveedorAsync(int proveedorId, CancellationToken ct = default);
    
    void Agregar(Pago pago);
    
    Task<int> GuardarAsync(CancellationToken ct = default);
}
