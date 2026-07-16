using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Inventario.Contracts;
using MiniERP.Application.Inventario.Dtos;
using MiniERP.Domain.Inventario;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class ProductoRepositorio(MiniErpDbContext contexto) : IProductoRepositorio
{
    /// <summary>
    /// Proyeccion compartida por el listado y la alerta.
    /// </summary>
    /// <remarks>
    /// Va como Expression y no como metodo porque EF Core traduce arboles de expresion
    /// a SQL, pero no puede traducir una llamada a metodo: con un metodo, la consulta
    /// se evaluaria en memoria trayendo el catalogo completo, o fallaria directamente.
    ///
    /// Las propiedades calculadas del dominio (PrecioConItbis, RequiereReabastecimiento)
    /// se repiten aqui en terminos de columnas por la misma razon. Es duplicacion
    /// deliberada: hay pruebas del dominio que fijan el comportamiento correcto.
    /// </remarks>
    private static readonly Expression<Func<Producto, ProductoListaDto>> Proyeccion =
        p => new ProductoListaDto(
            p.Id,
            p.Codigo,
            p.CodigoBarras,
            p.Descripcion,
            p.Categoria!.Nombre,
            p.UnidadMedida!.Codigo,
            p.Costo,
            p.PrecioVenta,
            p.PrecioVenta * (1 + p.TasaItbis),
            p.PrecioVenta == 0 ? 0 : (p.PrecioVenta - p.Costo) / p.PrecioVenta,
            p.Existencia,
            p.ExistenciaMinima,
            p.UnidadMedida!.PermiteDecimales,
            p.ManejaInventario && p.Activo && p.Existencia <= p.ExistenciaMinima,
            p.ManejaInventario && p.Existencia <= 0,
            p.Activo);

    public Task<Producto?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Productos.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Producto?> ObtenerPorCodigoAsync(string codigo, CancellationToken ct = default) =>
        contexto.Productos.FirstOrDefaultAsync(p => p.Codigo == codigo, ct);

    public async Task<PaginaDe<ProductoListaDto>> BuscarAsync(FiltroProductos filtro, CancellationToken ct = default)
    {
        var consulta = contexto.Productos.AsNoTracking();

        if (filtro.SoloActivos)
            consulta = consulta.Where(p => p.Activo);

        if (filtro.CategoriaId is { } categoriaId)
            consulta = consulta.Where(p => p.CategoriaId == categoriaId);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();

            consulta = consulta.Where(p =>
                p.Codigo.Contains(texto) ||
                p.Descripcion.Contains(texto) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(texto)));
        }

        if (filtro.SoloReabastecer)
        {
            // RequiereReabastecimiento es una propiedad calculada del dominio y no se
            // puede traducir a SQL: la condicion se repite aqui en terminos de columnas.
            consulta = consulta.Where(p => p.ManejaInventario && p.Existencia <= p.ExistenciaMinima);
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(p => p.Descripcion)
            .Skip(filtro.SaltarRegistros)
            .Take(filtro.TamanoEfectivo)
            .Select(Proyeccion)
            .ToListAsync(ct);

        return new PaginaDe<ProductoListaDto>(items, total, filtro.Pagina, filtro.TamanoEfectivo);
    }

    public async Task<IReadOnlyList<ProductoListaDto>> ObtenerParaReabastecerAsync(CancellationToken ct = default) =>
        await contexto.Productos
            .AsNoTracking()
            .Where(p => p.Activo && p.ManejaInventario && p.Existencia <= p.ExistenciaMinima)
            .OrderBy(p => p.Existencia)
            .ThenBy(p => p.Descripcion)
            .Select(Proyeccion)
            .ToListAsync(ct);

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
        contexto.Productos
            .AsNoTracking()
            .AnyAsync(p => p.Codigo == codigo && (excluirId == null || p.Id != excluirId), ct);

    public Task<bool> ExisteCodigoBarrasAsync(string codigoBarras, int? excluirId = null, CancellationToken ct = default) =>
        contexto.Productos
            .AsNoTracking()
            .AnyAsync(p => p.CodigoBarras == codigoBarras && (excluirId == null || p.Id != excluirId), ct);

    public Task<UnidadMedida?> ObtenerUnidadAsync(int unidadMedidaId, CancellationToken ct = default) =>
        contexto.UnidadesMedida.AsNoTracking().FirstOrDefaultAsync(u => u.Id == unidadMedidaId, ct);

    public async Task<IReadOnlyList<UnidadMedidaDto>> ObtenerUnidadesAsync(CancellationToken ct = default) =>
        await contexto.UnidadesMedida
            .AsNoTracking()
            .Where(u => u.Activo)
            .OrderBy(u => u.Codigo)
            .Select(u => new UnidadMedidaDto(u.Id, u.Codigo, u.Nombre, u.PermiteDecimales, u.CantidadDecimales))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<MovimientoDto>> ObtenerKardexAsync(int productoId, int cantidad = 50, CancellationToken ct = default) =>
        await contexto.MovimientosInventario
            .AsNoTracking()
            .Where(m => m.ProductoId == productoId)
            .OrderByDescending(m => m.Fecha)
            .ThenByDescending(m => m.Id)
            .Take(cantidad)
            .Select(m => new MovimientoDto(
                m.Fecha,
                m.Tipo,
                m.Cantidad,
                m.Tipo == TipoMovimiento.Entrada
                    || m.Tipo == TipoMovimiento.AjustePositivo
                    || m.Tipo == TipoMovimiento.DevolucionCliente,
                m.ExistenciaAnterior,
                m.ExistenciaResultante,
                m.Motivo,
                m.UsuarioId))
            .ToListAsync(ct);

    public void Agregar(Producto producto) => contexto.Productos.Add(producto);

    public void AgregarMovimiento(MovimientoInventario movimiento) => contexto.MovimientosInventario.Add(movimiento);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);
}
