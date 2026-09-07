namespace MiniERP.Domain.Shared;

/// <summary>
/// Base de las entidades persistidas. Las fechas se guardan siempre en UTC;
/// la conversion a la hora del usuario ocurre al mostrar.
/// </summary>
public abstract class EntidadBase
{
    public int Id { get; set; }

    public DateTime FechaCreacion { get; set; } = RelojSimulado.UtcNow;

    public string? CreadoPor { get; set; }

    public DateTime? FechaModificacion { get; set; }

    public string? ModificadoPor { get; set; }
}
