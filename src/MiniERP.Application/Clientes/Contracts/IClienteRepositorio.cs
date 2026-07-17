using MiniERP.Application.Clientes.Dtos;
using MiniERP.Application.Common;
using MiniERP.Domain.Clientes;

namespace MiniERP.Application.Clientes.Contracts;

public interface IClienteRepositorio
{
    Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    Task<PaginaDe<ClienteListaDto>> BuscarAsync(FiltroClientes filtro, CancellationToken ct = default);

    Task<ResumenCarteraDto> ObtenerResumenAsync(CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default);

    Task<bool> ExisteDocumentoAsync(string numeroDocumento, int? excluirId = null, CancellationToken ct = default);

    /// <summary>Sugiere el proximo codigo correlativo, para no obligar a inventarlo.</summary>
    Task<string> SugerirCodigoAsync(CancellationToken ct = default);

    void Agregar(Cliente cliente);

    Task<int> GuardarAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TransaccionEstadoCuentaDto>> ObtenerEstadoCuentaAsync(int clienteId, CancellationToken ct = default);
}
