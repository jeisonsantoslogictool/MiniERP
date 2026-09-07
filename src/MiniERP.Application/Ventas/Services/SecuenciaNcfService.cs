using MiniERP.Application.Common;
using MiniERP.Application.Ventas.Contracts;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Domain.Ventas;

namespace MiniERP.Application.Ventas.Services;

public interface ISecuenciaNcfService
{
    Task<IReadOnlyList<SecuenciaNcfDto>> ObtenerTodasAsync(CancellationToken ct = default);

    Task<Resultado> RegistrarAsync(RegistrarSecuenciaDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> CambiarEstadoAsync(int id, bool activa, CancellationToken ct = default);
}

/// <summary>
/// Administracion de las secuencias de NCF: registrar los rangos que autoriza la DGII y ver
/// cuantos comprobantes quedan. La asignacion en si la hace el repositorio de forma atomica
/// al facturar; esto es solo el mantenimiento de los rangos.
/// </summary>
public class SecuenciaNcfService(ISecuenciaNcfRepositorio secuencias) : ISecuenciaNcfService
{
    public async Task<IReadOnlyList<SecuenciaNcfDto>> ObtenerTodasAsync(CancellationToken ct = default)
    {
        var hoy = RelojSimulado.UtcNow.Date;

        var todas = await secuencias.ObtenerTodasAsync(ct);

        return [.. todas.Select(s => new SecuenciaNcfDto(
            s.Id, s.TipoComprobante, s.Prefijo, s.Desde, s.Hasta, s.Actual,
            s.Disponibles, s.FechaVencimiento, s.Activa, s.Agotada,
            s.EstaVencida(hoy), s.PorAgotarse, s.EsElectronica))];
    }

    public async Task<Resultado> RegistrarAsync(
        RegistrarSecuenciaDto form, string? usuarioId, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(form);

        var prefijo = (form.Prefijo ?? string.Empty).Trim().ToUpperInvariant();

        if (string.IsNullOrWhiteSpace(prefijo))
            return Resultado.Falla("El prefijo es obligatorio (por ejemplo, B02).");

        if (form.Desde < 1)
            return Resultado.Falla("El numero inicial debe ser mayor que cero.");

        if (form.Hasta <= form.Desde)
            return Resultado.Falla("El numero final debe ser mayor que el inicial.");

        if (form.FechaVencimiento.Date <= RelojSimulado.UtcNow.Date)
            return Resultado.Falla("La fecha de vencimiento debe ser futura.");

        // Dos rangos del mismo prefijo que se solapen producirian dos comprobantes con el
        // mismo NCF. Se rechaza antes de guardar: es una infraccion fiscal, no un descuido.
        var existentes = await secuencias.ObtenerTodasAsync(ct);

        var solapa = existentes.FirstOrDefault(s =>
            s.Prefijo == prefijo && form.Desde <= s.Hasta && form.Hasta >= s.Desde);

        if (solapa is not null)
            return Resultado.Falla(
                $"El rango se solapa con la secuencia {solapa.Prefijo} ({solapa.Desde}-{solapa.Hasta}) ya registrada.");

        secuencias.Agregar(new SecuenciaNcf
        {
            TipoComprobante = form.Tipo,
            Prefijo = prefijo,
            Desde = form.Desde,
            Hasta = form.Hasta,
            Actual = form.Desde - 1,   // sin estrenar: el primero que se asigne sera Desde
            FechaVencimiento = form.FechaVencimiento.Date,
            Activa = true,
            CreadoPor = usuarioId
        });

        await secuencias.GuardarAsync(ct);

        return Resultado.Ok();
    }

    public async Task<Resultado> CambiarEstadoAsync(int id, bool activa, CancellationToken ct = default)
    {
        var secuencia = await secuencias.ObtenerAsync(id, ct);

        if (secuencia is null)
            return Resultado.Falla("La secuencia no existe.");

        secuencia.Activa = activa;

        await secuencias.GuardarAsync(ct);

        return Resultado.Ok();
    }
}
