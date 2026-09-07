namespace MiniERP.Application.Finanzas.Dtos;

// ---------- Egresos ----------

/// <summary>Egreso tal como se muestra en la lista.</summary>
public record EgresoListaDto(
    int Id,
    DateTime Fecha,
    int CategoriaEgresoId,
    string Categoria,
    decimal Monto,
    string? Descripcion,
    string? RegistradoPor);

public class EgresoFormDto
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; } = RelojSimulado.UtcNow;
    public int CategoriaEgresoId { get; set; }
    public decimal Monto { get; set; }
    public string? Descripcion { get; set; }
}

public record FiltroEgresos(
    DateTime? Desde = null,
    DateTime? Hasta = null,
    int? CategoriaEgresoId = null,
    int Pagina = 1,
    int TamanoPagina = 25)
{
    public int SaltarRegistros => (Math.Max(1, Pagina) - 1) * TamanoEfectivo;
    public int TamanoEfectivo => Math.Clamp(TamanoPagina, 1, 100);
}

// ---------- Categorias de egreso ----------

public record CategoriaEgresoListaDto(
    int Id,
    string Nombre,
    string? Descripcion,
    bool Activo,
    int CantidadEgresos);

public class CategoriaEgresoFormDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;
}

public record FiltroCategoriasEgreso(
    string? Texto = null,
    bool SoloActivas = false,
    int Pagina = 1,
    int TamanoPagina = 25)
{
    public int SaltarRegistros => (Math.Max(1, Pagina) - 1) * TamanoEfectivo;
    public int TamanoEfectivo => Math.Clamp(TamanoPagina, 1, 100);
}
