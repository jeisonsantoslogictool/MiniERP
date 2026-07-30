using MiniERP.Application.Clientes.Contracts;
using MiniERP.Application.Clientes.Dtos;
using MiniERP.Application.Common;
using MiniERP.Domain.Clientes;

namespace MiniERP.Application.Clientes.Services;

public interface ICobrosService
{
    Task<IReadOnlyList<CobroListaDto>> ObtenerPorClienteAsync(int clienteId, CancellationToken ct = default);
    Task<Resultado<int>> RegistrarCobroAsync(CobroFormDto form, string? usuarioId, CancellationToken ct = default);
}

public class CobrosService(
    ICobrosRepositorio cobros,
    IClienteRepositorio clientes) : ICobrosService
{
    public async Task<IReadOnlyList<CobroListaDto>> ObtenerPorClienteAsync(int clienteId, CancellationToken ct = default)
    {
        var lista = await cobros.ObtenerPorClienteAsync(clienteId, ct);
        return lista.Select(c => new CobroListaDto(
            c.Id,
            c.ClienteId,
            c.Cliente?.Nombre ?? string.Empty,
            c.Fecha,
            c.Monto,
            c.BalanceAnterior,
            c.BalanceResultante,
            c.Observacion,
            c.UsuarioId)).ToList();
    }

    public async Task<Resultado<int>> RegistrarCobroAsync(CobroFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        if (form.Monto <= 0)
            return Resultado.Falla<int>("El monto del cobro debe ser positivo.");

        var cliente = await clientes.ObtenerPorIdAsync(form.ClienteId, ct);
        if (cliente is null)
            return Resultado.Falla<int>("El cliente no existe.");

        var cobro = new Cobro
        {
            Fecha = form.Fecha,
            Monto = form.Monto,
            Observacion = form.Observacion?.Trim(),
            UsuarioId = usuarioId,
            CreadoPor = usuarioId
        };

        try
        {
            cliente.AplicarCobro(cobro);
        }
        catch (InvalidOperationException ex)
        {
            return Resultado.Falla<int>(ex.Message);
        }

        cobros.Agregar(cobro);
        await cobros.GuardarAsync(ct);

        return Resultado.Ok(cobro.Id);
    }
}
