namespace MiniERP.Application.Compras.Dtos;

public record CuentasPorPagarDto(
    int ProveedorId,
    string Codigo,
    string Nombre,
    string? Contacto,
    string? Telefono,
    decimal BalanceActual,
    int DiasCredito,
    int DiasVencidos,
    decimal MontoVencido
);
