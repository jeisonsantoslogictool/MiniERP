namespace MiniERP.Application.Clientes.Dtos;

public record CuentasPorCobrarDto(
    int ClienteId,
    string Codigo,
    string Nombre,
    string? Telefono,
    decimal BalanceActual,
    int DiasCredito,
    int DiasVencidos,
    decimal MontoVencido
);
