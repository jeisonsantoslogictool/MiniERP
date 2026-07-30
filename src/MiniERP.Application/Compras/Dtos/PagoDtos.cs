using MiniERP.Domain.Compras;

namespace MiniERP.Application.Compras.Dtos;

public record PagoListaDto(
    int Id,
    int ProveedorId,
    string ProveedorNombre,
    DateTime Fecha,
    decimal Monto,
    decimal BalanceAnterior,
    decimal BalanceResultante,
    string? Observacion,
    string? UsuarioId);

public class PagoFormDto
{
    public DateTime Fecha { get; set; } = DateTime.Today;
    public int ProveedorId { get; set; }
    public string ProveedorNombre { get; set; } = string.Empty;
    public decimal BalanceActual { get; set; }
    
    public decimal Monto { get; set; }
    public string? Observacion { get; set; }
}
