namespace MiniERP.Application.Inventario.Dtos;

/// <summary>
/// Criterios de busqueda del listado de productos.
/// </summary>
/// <param name="Texto">Busca en codigo, codigo de barras y descripcion.</param>
/// <param name="CategoriaId">Limita a una categoria.</param>
/// <param name="SoloReabastecer">Deja solo los que tocaron su minimo.</param>
/// <param name="SoloActivos">Excluye los descontinuados.</param>
public record FiltroProductos(
    string? Texto = null,
    int? CategoriaId = null,
    bool SoloReabastecer = false,
    bool SoloActivos = true,
    int Pagina = 1,
    int TamanoPagina = 25)
{
    /// <summary>Tope de registros por pagina, para que un filtro vacio no traiga el catalogo entero.</summary>
    public const int MaxTamanoPagina = 100;

    public int SaltarRegistros => (Math.Max(1, Pagina) - 1) * TamanoEfectivo;

    public int TamanoEfectivo => Math.Clamp(TamanoPagina, 1, MaxTamanoPagina);
}
