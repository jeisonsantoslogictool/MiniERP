using MiniERP.Application.Common;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Application.Finanzas.Services;

public interface ICategoriaEgresoService
{
    Task<PaginaDe<CategoriaEgresoListaDto>> BuscarAsync(FiltroCategoriasEgreso filtro, CancellationToken ct = default);

    Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default);

    Task<CategoriaEgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<Resultado<int>> GuardarAsync(CategoriaEgresoFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> CambiarEstadoAsync(int id, bool activo, string? usuarioId, CancellationToken ct = default);
}

/// <summary>
/// Casos de uso del catalogo de categorias de gasto.
/// </summary>
public class CategoriaEgresoService(ICategoriaEgresoRepositorio categorias) : ICategoriaEgresoService
{
    public Task<PaginaDe<CategoriaEgresoListaDto>> BuscarAsync(FiltroCategoriasEgreso filtro, CancellationToken ct = default) =>
        categorias.BuscarAsync(filtro, ct);

    public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
        categorias.ObtenerOpcionesAsync(ct);

    public async Task<CategoriaEgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var categoria = await categorias.ObtenerAsync(id, ct);

        if (categoria is null)
            return null;

        return new CategoriaEgresoFormDto
        {
            Id = categoria.Id,
            Nombre = categoria.Nombre,
            Descripcion = categoria.Descripcion,
            Activo = categoria.Activo
        };
    }

    public async Task<Resultado<int>> GuardarAsync(CategoriaEgresoFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(form.Nombre))
            return Resultado.Falla<int>("El nombre de la categoría es obligatorio.");

        var nombre = form.Nombre.Trim();

        if (await categorias.ExisteNombreAsync(nombre, form.Id == 0 ? null : form.Id, ct))
            return Resultado.Falla<int>($"Ya existe una categoría llamada '{nombre}'.");

        var categoria = form.Id == 0
            ? null
            : await categorias.ObtenerAsync(form.Id, ct);

        if (form.Id != 0 && categoria is null)
            return Resultado.Falla<int>("La categoría no existe.");

        if (categoria is null)
        {
            categoria = new CategoriaEgreso { CreadoPor = usuarioId };
            categorias.Agregar(categoria);
        }
        else
        {
            categoria.FechaModificacion = DateTime.UtcNow;
            categoria.ModificadoPor = usuarioId;
        }

        categoria.Nombre = nombre;
        categoria.Descripcion = string.IsNullOrWhiteSpace(form.Descripcion) ? null : form.Descripcion.Trim();
        categoria.Activo = form.Activo;

        await categorias.GuardarAsync(ct);

        return Resultado.Ok(categoria.Id);
    }

    public async Task<Resultado> CambiarEstadoAsync(int id, bool activo, string? usuarioId, CancellationToken ct = default)
    {
        var categoria = await categorias.ObtenerAsync(id, ct);

        if (categoria is null)
            return Resultado.Falla("La categoría no existe.");

        categoria.Activo = activo;
        categoria.FechaModificacion = DateTime.UtcNow;
        categoria.ModificadoPor = usuarioId;

        await categorias.GuardarAsync(ct);

        return Resultado.Ok();
    }
}
