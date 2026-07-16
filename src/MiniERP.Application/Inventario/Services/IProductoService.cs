using MiniERP.Application.Common;
using MiniERP.Application.Inventario.Dtos;

namespace MiniERP.Application.Inventario.Services;

public interface IProductoService
{
    Task<PaginaDe<ProductoListaDto>> BuscarAsync(FiltroProductos filtro, CancellationToken ct = default);

    Task<IReadOnlyList<ProductoListaDto>> ObtenerParaReabastecerAsync(CancellationToken ct = default);

    Task<ProductoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<UnidadMedidaDto>> ObtenerUnidadesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<MovimientoDto>> ObtenerKardexAsync(int productoId, int cantidad = 50, CancellationToken ct = default);

    Task<Resultado<int>> CrearAsync(ProductoFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> ActualizarAsync(ProductoFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> AjustarExistenciaAsync(AjusteExistenciaDto ajuste, string? usuarioId, CancellationToken ct = default);
}
