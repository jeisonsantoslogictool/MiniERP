using MiniERP.Application.Common;
using MiniERP.Application.Inventario.Contracts;
using MiniERP.Application.Inventario.Dtos;
using MiniERP.Domain.Inventario;

namespace MiniERP.Application.Inventario.Services;

public interface ICategoriaService
{
    Task<IReadOnlyList<Categoria>> ObtenerTodasAsync(bool soloActivas = false, CancellationToken ct = default);

    Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default);

    Task<Resultado<int>> CrearAsync(string nombre, string? descripcion, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> ActualizarAsync(int id, string nombre, string? descripcion, bool activo, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> EliminarAsync(int id, CancellationToken ct = default);
}

public class CategoriaService(ICategoriaRepositorio categorias) : ICategoriaService
{
    public Task<IReadOnlyList<Categoria>> ObtenerTodasAsync(bool soloActivas = false, CancellationToken ct = default) =>
        categorias.ObtenerTodasAsync(soloActivas, ct);

    public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
        categorias.ObtenerOpcionesAsync(ct);

    public async Task<Resultado<int>> CrearAsync(string nombre, string? descripcion, string? usuarioId, CancellationToken ct = default)
    {
        nombre = nombre?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
            return Resultado.Falla<int>("El nombre es obligatorio.");

        if (await categorias.ExisteNombreAsync(nombre, null, ct))
            return Resultado.Falla<int>($"Ya existe la categoria '{nombre}'.");

        var categoria = new Categoria
        {
            Nombre = nombre,
            Descripcion = descripcion?.Trim(),
            Activo = true,
            CreadoPor = usuarioId
        };

        categorias.Agregar(categoria);
        await categorias.GuardarAsync(ct);

        return Resultado.Ok(categoria.Id);
    }

    public async Task<Resultado> ActualizarAsync(int id, string nombre, string? descripcion, bool activo, string? usuarioId, CancellationToken ct = default)
    {
        var categoria = await categorias.ObtenerPorIdAsync(id, ct);

        if (categoria is null)
            return Resultado.Falla("La categoria no existe.");

        nombre = nombre?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(nombre))
            return Resultado.Falla("El nombre es obligatorio.");

        if (await categorias.ExisteNombreAsync(nombre, id, ct))
            return Resultado.Falla($"Ya existe la categoria '{nombre}'.");

        categoria.Nombre = nombre;
        categoria.Descripcion = descripcion?.Trim();
        categoria.Activo = activo;
        categoria.FechaModificacion = DateTime.UtcNow;
        categoria.ModificadoPor = usuarioId;

        await categorias.GuardarAsync(ct);

        return Resultado.Ok();
    }

    /// <summary>
    /// Elimina la categoria solo si esta vacia. Con productos dentro se desactiva:
    /// borrarla dejaria huerfano el historico de esos productos.
    /// </summary>
    public async Task<Resultado> EliminarAsync(int id, CancellationToken ct = default)
    {
        var categoria = await categorias.ObtenerPorIdAsync(id, ct);

        if (categoria is null)
            return Resultado.Falla("La categoria no existe.");

        var enUso = await categorias.ContarProductosAsync(id, ct);

        if (enUso > 0)
            return Resultado.Falla(
                $"'{categoria.Nombre}' tiene {enUso} producto(s). Desactivala en vez de eliminarla.");

        categorias.Eliminar(categoria);
        await categorias.GuardarAsync(ct);

        return Resultado.Ok();
    }
}
