using MiniERP.Application.Common;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Application.Finanzas.Services;

public interface IEgresoService
{
    Task<PaginaDe<EgresoListaDto>> BuscarAsync(FiltroEgresos filtro, CancellationToken ct = default);

    /// <summary>Total de los egresos que cumplen el filtro: el gasto del periodo.</summary>
    Task<decimal> ObtenerTotalAsync(FiltroEgresos filtro, CancellationToken ct = default);

    Task<EgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<Resultado<int>> GuardarAsync(EgresoFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> EliminarAsync(int id, CancellationToken ct = default);
}

/// <summary>
/// Casos de uso del registro de gastos operativos.
/// </summary>
public class EgresoService(
    IEgresoRepositorio egresos,
    ICategoriaEgresoRepositorio categorias) : IEgresoService
{
    public Task<PaginaDe<EgresoListaDto>> BuscarAsync(FiltroEgresos filtro, CancellationToken ct = default) =>
        egresos.BuscarAsync(filtro, ct);

    public Task<decimal> ObtenerTotalAsync(FiltroEgresos filtro, CancellationToken ct = default) =>
        egresos.ObtenerTotalAsync(filtro, ct);

    public Task<EgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default) =>
        egresos.ObtenerParaEditarAsync(id, ct);

    public async Task<Resultado<int>> GuardarAsync(EgresoFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        if (form.CategoriaEgresoId == 0)
            return Resultado.Falla<int>("Selecciona una categoría.");

        if (await categorias.ObtenerAsync(form.CategoriaEgresoId, ct) is null)
            return Resultado.Falla<int>("La categoría no existe.");

        if (form.Id == 0)
            return await CrearAsync(form, usuarioId, ct);

        return await ActualizarAsync(form, usuarioId, ct);
    }

    private async Task<Resultado<int>> CrearAsync(EgresoFormDto form, string? usuarioId, CancellationToken ct)
    {
        Egreso egreso;

        try
        {
            // La fabrica del dominio valida el monto y la categoria.
            egreso = Egreso.Registrar(form.Monto, form.Fecha, form.CategoriaEgresoId, form.Descripcion, usuarioId);
        }
        catch (InvalidOperationException ex)
        {
            return Resultado.Falla<int>(ex.Message);
        }

        egresos.Agregar(egreso);
        await egresos.GuardarAsync(ct);

        return Resultado.Ok(egreso.Id);
    }

    private async Task<Resultado<int>> ActualizarAsync(EgresoFormDto form, string? usuarioId, CancellationToken ct)
    {
        if (form.Monto <= 0)
            return Resultado.Falla<int>("El monto debe ser mayor que cero.");

        var egreso = await egresos.ObtenerAsync(form.Id, ct);

        if (egreso is null)
            return Resultado.Falla<int>("El egreso no existe.");

        egreso.Fecha = form.Fecha;
        egreso.CategoriaEgresoId = form.CategoriaEgresoId;
        egreso.Monto = form.Monto;
        egreso.Descripcion = string.IsNullOrWhiteSpace(form.Descripcion) ? null : form.Descripcion.Trim();
        egreso.FechaModificacion = RelojSimulado.UtcNow;
        egreso.ModificadoPor = usuarioId;

        await egresos.GuardarAsync(ct);

        return Resultado.Ok(egreso.Id);
    }

    public async Task<Resultado> EliminarAsync(int id, CancellationToken ct = default)
    {
        var egreso = await egresos.ObtenerAsync(id, ct);

        if (egreso is null)
            return Resultado.Falla("El egreso no existe.");

        egresos.Eliminar(egreso);
        await egresos.GuardarAsync(ct);

        return Resultado.Ok();
    }
}
