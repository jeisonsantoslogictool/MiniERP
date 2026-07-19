using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Application.Common;
using MiniERP.Domain.Compras;

namespace MiniERP.Application.Compras.Services;

public interface IPagosService
{
    Task<IReadOnlyList<PagoListaDto>> ObtenerPorProveedorAsync(int proveedorId, CancellationToken ct = default);
    Task<Resultado<int>> RegistrarPagoAsync(PagoFormDto form, string? usuarioId, CancellationToken ct = default);
}

public class PagosService(
    IPagosRepositorio pagos,
    IProveedorRepositorio proveedores) : IPagosService
{
    public async Task<IReadOnlyList<PagoListaDto>> ObtenerPorProveedorAsync(int proveedorId, CancellationToken ct = default)
    {
        var lista = await pagos.ObtenerPorProveedorAsync(proveedorId, ct);
        return lista.Select(p => new PagoListaDto(
            p.Id,
            p.ProveedorId,
            p.Proveedor?.Nombre ?? string.Empty,
            p.Fecha,
            p.Monto,
            p.BalanceAnterior,
            p.BalanceResultante,
            p.Observacion,
            p.UsuarioId)).ToList();
    }

    public async Task<Resultado<int>> RegistrarPagoAsync(PagoFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        if (form.Monto <= 0)
            return Resultado.Falla<int>("El monto del pago debe ser positivo.");

        var proveedor = await proveedores.ObtenerPorIdAsync(form.ProveedorId, ct);
        if (proveedor is null)
            return Resultado.Falla<int>("El proveedor no existe.");

        var pago = new Pago
        {
            Monto = form.Monto,
            Observacion = form.Observacion?.Trim(),
            UsuarioId = usuarioId,
            CreadoPor = usuarioId
        };

        try
        {
            proveedor.AplicarPago(pago);
        }
        catch (InvalidOperationException ex)
        {
            return Resultado.Falla<int>(ex.Message);
        }

        pagos.Agregar(pago);
        await pagos.GuardarAsync(ct);

        return Resultado.Ok(pago.Id);
    }
}
