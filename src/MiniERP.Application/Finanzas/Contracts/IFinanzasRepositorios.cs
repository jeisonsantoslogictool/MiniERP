using MiniERP.Application.Common;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Application.Finanzas.Contracts;

public interface IEgresoRepositorio
{
    /// <summary>Trae el egreso con seguimiento, para editarlo o eliminarlo.</summary>
    Task<Egreso?> ObtenerAsync(int id, CancellationToken ct = default);

    Task<PaginaDe<EgresoListaDto>> BuscarAsync(FiltroEgresos filtro, CancellationToken ct = default);

    Task<EgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    /// <summary>Suma de los montos que cumplen el filtro: el total del periodo.</summary>
    Task<decimal> ObtenerTotalAsync(FiltroEgresos filtro, CancellationToken ct = default);

    void Agregar(Egreso egreso);

    void Eliminar(Egreso egreso);

    Task<int> GuardarAsync(CancellationToken ct = default);
}

public interface ICategoriaEgresoRepositorio
{
    Task<CategoriaEgreso?> ObtenerAsync(int id, CancellationToken ct = default);

    Task<PaginaDe<CategoriaEgresoListaDto>> BuscarAsync(FiltroCategoriasEgreso filtro, CancellationToken ct = default);

    /// <summary>Categorias activas para el desplegable del formulario de egreso.</summary>
    Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default);

    Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null, CancellationToken ct = default);

    void Agregar(CategoriaEgreso categoria);

    Task<int> GuardarAsync(CancellationToken ct = default);
}
