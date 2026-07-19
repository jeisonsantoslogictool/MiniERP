namespace MiniERP.Application.Clientes.Dtos;

public record TransaccionEstadoCuentaDto(
    DateTime Fecha,
    string Referencia,
    string Concepto,
    decimal Debito,  // Facturas a crédito (cargos)
    decimal Credito, // Cobros (abonos)
    decimal BalanceResultante);
