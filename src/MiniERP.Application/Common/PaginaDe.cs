namespace MiniERP.Application.Common;

/// <summary>
/// Pagina de resultados de una busqueda.
/// </summary>
/// <param name="Items">Registros de la pagina actual.</param>
/// <param name="Total">Total de coincidencias, para calcular la paginacion.</param>
public record PaginaDe<T>(IReadOnlyList<T> Items, int Total, int Pagina, int TamanoPagina)
{
    public int TotalPaginas => TamanoPagina == 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanoPagina);

    public bool HayAnterior => Pagina > 1;

    public bool HaySiguiente => Pagina < TotalPaginas;
}
