using MiniERP.Application.Common;
using MiniERP.Application.Inventario.Contracts;
using MiniERP.Application.Inventario.Dtos;
using MiniERP.Domain.Inventario;

namespace MiniERP.Application.Inventario.Services;

/// <summary>
/// Casos de uso del modulo de inventario.
/// </summary>
public class ProductoService(
    IProductoRepositorio productos,
    ICategoriaRepositorio categorias) : IProductoService
{
    public Task<PaginaDe<ProductoListaDto>> BuscarAsync(FiltroProductos filtro, CancellationToken ct = default) =>
        productos.BuscarAsync(filtro, ct);

    public Task<IReadOnlyList<ProductoListaDto>> ObtenerParaReabastecerAsync(CancellationToken ct = default) =>
        productos.ObtenerParaReabastecerAsync(ct);

    public Task<IReadOnlyList<UnidadMedidaDto>> ObtenerUnidadesAsync(CancellationToken ct = default) =>
        productos.ObtenerUnidadesAsync(ct);

    public Task<IReadOnlyList<MovimientoDto>> ObtenerKardexAsync(int productoId, int cantidad = 50, CancellationToken ct = default) =>
        productos.ObtenerKardexAsync(productoId, cantidad, ct);

    public async Task<ProductoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var producto = await productos.ObtenerPorIdAsync(id, ct);

        if (producto is null)
            return null;

        return new ProductoFormDto
        {
            Id = producto.Id,
            Codigo = producto.Codigo,
            CodigoBarras = producto.CodigoBarras,
            Descripcion = producto.Descripcion,
            CategoriaId = producto.CategoriaId,
            UnidadMedidaId = producto.UnidadMedidaId,
            Costo = producto.Costo,
            PrecioVenta = producto.PrecioVenta,
            TasaItbis = producto.TasaItbis,
            ExistenciaMinima = producto.ExistenciaMinima,
            ManejaInventario = producto.ManejaInventario,
            Activo = producto.Activo
        };
    }

    public async Task<Resultado<int>> CrearAsync(ProductoFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var validacion = await ValidarAsync(form, ct);

        if (validacion.Fallo)
            return Resultado.Falla<int>(validacion.Error!);

        var unidad = await productos.ObtenerUnidadAsync(form.UnidadMedidaId, ct);

        if (form.ExistenciaInicial > 0 && !unidad!.CantidadEsValida(form.ExistenciaInicial))
            return Resultado.Falla<int>(ErrorDeCantidad(unidad, form.ExistenciaInicial));

        var producto = new Producto
        {
            Codigo = form.Codigo.Trim().ToUpperInvariant(),
            CodigoBarras = Normalizar(form.CodigoBarras),
            Descripcion = form.Descripcion.Trim(),
            CategoriaId = form.CategoriaId,
            UnidadMedidaId = form.UnidadMedidaId,
            Costo = form.Costo,
            PrecioVenta = form.PrecioVenta,
            TasaItbis = form.TasaItbis,
            ExistenciaMinima = form.ExistenciaMinima,
            ManejaInventario = form.ManejaInventario,
            Activo = form.Activo,
            Existencia = 0,
            CreadoPor = usuarioId
        };

        productos.Agregar(producto);
        await productos.GuardarAsync(ct);

        // La existencia inicial entra como movimiento y no como valor directo: asi
        // ningun saldo existe sin un asiento que lo explique, desde el primer dia.
        if (form.ExistenciaInicial > 0 && producto.ManejaInventario)
        {
            var apertura = new MovimientoInventario
            {
                Tipo = TipoMovimiento.Entrada,
                Cantidad = form.ExistenciaInicial,
                CostoUnitario = form.Costo,
                Motivo = "Existencia inicial",
                ReferenciaTipo = "APERTURA",
                UsuarioId = usuarioId,
                CreadoPor = usuarioId
            };

            producto.AplicarMovimiento(apertura);
            productos.AgregarMovimiento(apertura);
            await productos.GuardarAsync(ct);
        }

        return Resultado.Ok(producto.Id);
    }

    public async Task<Resultado> ActualizarAsync(ProductoFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var producto = await productos.ObtenerPorIdAsync(form.Id, ct);

        if (producto is null)
            return Resultado.Falla("El producto no existe.");

        var validacion = await ValidarAsync(form, ct);

        if (validacion.Fallo)
            return validacion;

        producto.Codigo = form.Codigo.Trim().ToUpperInvariant();
        producto.CodigoBarras = Normalizar(form.CodigoBarras);
        producto.Descripcion = form.Descripcion.Trim();
        producto.CategoriaId = form.CategoriaId;
        producto.UnidadMedidaId = form.UnidadMedidaId;
        producto.Costo = form.Costo;
        producto.PrecioVenta = form.PrecioVenta;
        producto.TasaItbis = form.TasaItbis;
        producto.ExistenciaMinima = form.ExistenciaMinima;
        producto.ManejaInventario = form.ManejaInventario;
        producto.Activo = form.Activo;
        producto.FechaModificacion = DateTime.UtcNow;
        producto.ModificadoPor = usuarioId;

        // La existencia no se toca aqui a proposito: solo cambia por movimiento.
        await productos.GuardarAsync(ct);

        return Resultado.Ok();
    }

    public async Task<Resultado> AjustarExistenciaAsync(AjusteExistenciaDto ajuste, string? usuarioId, CancellationToken ct = default)
    {
        var producto = await productos.ObtenerPorIdAsync(ajuste.ProductoId, ct);

        if (producto is null)
            return Resultado.Falla("El producto no existe.");

        if (!producto.ManejaInventario)
            return Resultado.Falla($"'{producto.Descripcion}' no maneja inventario.");

        if (ajuste.Cantidad <= 0)
            return Resultado.Falla("La cantidad debe ser mayor que cero.");

        var unidad = await productos.ObtenerUnidadAsync(producto.UnidadMedidaId, ct);

        if (unidad is not null && !unidad.CantidadEsValida(ajuste.Cantidad))
            return Resultado.Falla(ErrorDeCantidad(unidad, ajuste.Cantidad));

        var movimiento = new MovimientoInventario
        {
            Tipo = ajuste.Tipo,
            Cantidad = ajuste.Cantidad,
            CostoUnitario = producto.Costo,
            Motivo = ajuste.Motivo?.Trim(),
            ReferenciaTipo = "AJUSTE",
            UsuarioId = usuarioId,
            CreadoPor = usuarioId
        };

        if (movimiento.RequiereMotivo && string.IsNullOrWhiteSpace(movimiento.Motivo))
            return Resultado.Falla("Las mermas y los ajustes necesitan un motivo.");

        try
        {
            producto.AplicarMovimiento(movimiento);
        }
        catch (InvalidOperationException ex)
        {
            return Resultado.Falla(ex.Message);
        }

        productos.AgregarMovimiento(movimiento);
        await productos.GuardarAsync(ct);

        return Resultado.Ok();
    }

    private async Task<Resultado> ValidarAsync(ProductoFormDto form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Codigo))
            return Resultado.Falla("El codigo es obligatorio.");

        if (string.IsNullOrWhiteSpace(form.Descripcion))
            return Resultado.Falla("La descripcion es obligatoria.");

        if (form.Costo < 0 || form.PrecioVenta < 0)
            return Resultado.Falla("El costo y el precio no pueden ser negativos.");

        if (form.ExistenciaMinima < 0)
            return Resultado.Falla("La existencia minima no puede ser negativa.");

        var codigo = form.Codigo.Trim().ToUpperInvariant();
        var excluir = form.Id == 0 ? (int?)null : form.Id;

        if (await productos.ExisteCodigoAsync(codigo, excluir, ct))
            return Resultado.Falla($"Ya existe un producto con el codigo '{codigo}'.");

        var barras = Normalizar(form.CodigoBarras);

        if (barras is not null && await productos.ExisteCodigoBarrasAsync(barras, excluir, ct))
            return Resultado.Falla($"Ya existe un producto con el codigo de barras '{barras}'.");

        if (await categorias.ObtenerPorIdAsync(form.CategoriaId, ct) is null)
            return Resultado.Falla("Selecciona una categoria valida.");

        if (await productos.ObtenerUnidadAsync(form.UnidadMedidaId, ct) is null)
            return Resultado.Falla("Selecciona una unidad de medida valida.");

        return Resultado.Ok();
    }

    private static string ErrorDeCantidad(UnidadMedida unidad, decimal cantidad) =>
        unidad.PermiteDecimales
            ? $"La cantidad {cantidad} excede los {unidad.CantidadDecimales} decimales que admite {unidad.Codigo}."
            : $"{unidad.Nombre} no admite decimales: {cantidad} no es una cantidad valida.";

    private static string? Normalizar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
