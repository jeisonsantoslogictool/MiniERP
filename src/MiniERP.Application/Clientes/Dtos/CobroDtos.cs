using MiniERP.Domain.Clientes;

namespace MiniERP.Application.Clientes.Dtos;

public record CobroListaDto(
    int Id,
    int ClienteId,
    string ClienteNombre,
    DateTime Fecha,
    decimal Monto,
    decimal BalanceAnterior,
    decimal BalanceResultante,
    string? Observacion,
    string? UsuarioId);

public class CobroFormDto
{
    public DateTime Fecha { get; set; } = RelojSimulado.Today;
    public int ClienteId { get; set; }
    public string ClienteNombre { get; set; } = string.Empty;
    public decimal BalanceActual { get; set; }
    
    public decimal Monto { get; set; }
    public string? Observacion { get; set; }
}
