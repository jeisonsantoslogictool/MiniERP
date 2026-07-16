using MiniERP.Application.Common;
using MiniERP.Application.Inventario.Dtos;
using MiniERP.Domain.Inventario;

namespace MiniERP.Application.Inventario.Contracts;

/// <summary>
/// Acceso a datos de productos y su kardex.
/// </summary>
/// <remarks>
/// Devuelve entidades ya materializadas y no IQueryable, para que la capa de aplicacion
/// no quede atada a Entity Framework ni pueda disparar consultas sin darse cuenta.
/// </remarks>
public interface IProductoRepositorio
{
    Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    Task<Producto?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default);

    Task<PaginaDe<ProductoListaDto>> BuscarAsync(FiltroProductos filtro, CancellationToken ct = default);

    /// <summary>Productos que tocaron su minimo. Alimenta la alerta de reabastecimiento.</summary>
    Task<IReadOnlyList<ProductoListaDto>> ObtenerParaReabastecerAsync(CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default);

    Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int? excluirId = null, CancellationToken ct = default);

    Task<UnidadMedida?> ObtenerUnidadAsync(int unidadMedidaId, CancellationToken ct = default);

    Task<IReadOnlyList<UnidadMedidaDto>> ObtenerUnidadesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<MovimientoDto>> ObtenerKardexAsync(int productoId, int cantidad = 50, CancellationToken ct = default);

    void Agregar(Producto producto);

    void AgregarMovimiento(MovimientoInventario movimiento);

    Task<int> GuardarAsync(CancellationToken ct = default);
}

public interface ICategoriaRepositorio
{
    Task<Categoria?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    Task<IReadOnlyList<Categoria>> ObtenerTodasAsync(bool soloActivas = false, CancellationToken ct = default);

    Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default);

    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null, CancellationToken ct = default);

    /// <summary>Cuantos productos la usan. Una categoria en uso no se puede eliminar.</summary>
    Task<int> ContarProductosAsync(int categoriaId, CancellationToken ct = default);

    void Agregar(Categoria categoria);

    void Eliminar(Categoria categoria);

    Task<int> GuardarAsync(CancellationToken ct = default);
}
