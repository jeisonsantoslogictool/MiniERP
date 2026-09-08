using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Common;
using MiniERP.Application.Ventas.Contracts;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Ventas;

namespace MiniERP.Infrastructure.Persistence.Repositories;

/// <summary>
/// Datos del punto de venta.
/// </summary>
/// <remarks>
/// Recibe dos cosas a proposito. El <c>contexto</c> compartido del circuito es el que
/// escribe: la emision necesita que la factura, el NCF y el movimiento entren en la MISMA
/// transaccion, y eso solo se sostiene si comparten instancia. Las lecturas puras, en
/// cambio, abren su propio contexto con la <c>fabrica</c>: el buscador del POS dispara una
/// consulta por cada tecla —un escaner escribe el codigo entero en milisegundos— y dos de
/// esas consultas sobre el mismo contexto revientan con "A second operation was started on
/// this context instance", que es el error que le salta al cajero a mitad de una venta.
/// </remarks>
public class VentaRepositorio(
    MiniErpDbContext contexto,
    IDbContextFactory<MiniErpDbContext> fabrica) : IVentaRepositorio
{
    private static readonly Expression<Func<Producto, ProductoParaVentaDto>> ProyeccionProducto =
        p => new ProductoParaVentaDto(
            p.Id, p.Codigo, p.CodigoBarras, p.Descripcion,
            p.UnidadMedida!.Codigo, p.UnidadMedida!.PermiteDecimales,
            p.PrecioVenta, p.TasaItbis, p.Existencia, p.ManejaInventario);

    private static readonly Expression<Func<Factura, FacturaListaDto>> ProyeccionFactura =
        f => new FacturaListaDto(
            f.Id, f.Numero, f.Ncf, f.ClienteNombre, f.Fecha,
            f.Estado, f.Condicion, f.Total, f.Subtotal - f.CostoTotal, f.UsuarioId);

    public async Task<IReadOnlyList<ProductoParaVentaDto>> BuscarProductosAsync(
        string texto, int maximo = 15, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return [];

        texto = texto.Trim();

        await using var db = await fabrica.CreateDbContextAsync(ct);

        return await db.Productos
            .AsNoTracking()
            .Where(p => p.Activo && (
                p.Codigo.Contains(texto) ||
                p.Descripcion.Contains(texto) ||
                (p.CodigoBarras != null && p.CodigoBarras.Contains(texto))))
            .OrderBy(p => p.Descripcion)
            .Take(maximo)
            .Select(ProyeccionProducto)
            .ToListAsync(ct);
    }

    /// <summary>
    /// Coincidencia exacta y no parcial: lo que llega del escaner es el codigo completo,
    /// y una busqueda parcial podria devolver el producto equivocado.
    /// </summary>
    public async Task<ProductoParaVentaDto?> ObtenerPorCodigoBarrasAsync(
        string codigoBarras, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);

        return await db.Productos
            .AsNoTracking()
            .Where(p => p.Activo && p.CodigoBarras == codigoBarras)
            .Select(ProyeccionProducto)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<ClienteParaVentaDto>> BuscarClientesAsync(
        string texto, int maximo = 10, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(texto))
            return [];

        texto = texto.Trim();
        var sinGuiones = texto.Replace("-", string.Empty);

        await using var db = await fabrica.CreateDbContextAsync(ct);

        return await db.Clientes
            .AsNoTracking()
            .Where(c => c.Activo && (
                c.Codigo.Contains(texto) ||
                c.Nombre.Contains(texto) ||
                (c.NumeroDocumento != null && c.NumeroDocumento.Contains(sinGuiones)) ||
                (c.Telefono != null && c.Telefono.Contains(texto))))
            .OrderBy(c => c.Nombre)
            .Take(maximo)
            .Select(c => new ClienteParaVentaDto(
                c.Id, c.Codigo, c.Nombre, c.NumeroDocumento, c.TipoComprobante,
                c.LimiteCredito, c.BalanceActual,
                c.LimiteCredito - c.BalanceActual < 0 ? 0 : c.LimiteCredito - c.BalanceActual,
                c.LimiteCredito > 0))
            .ToListAsync(ct);
    }

    public async Task<PaginaDe<FacturaListaDto>> BuscarAsync(FiltroFacturas filtro, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);

        var consulta = db.Facturas.AsNoTracking();

        if (filtro.Estado is { } estado)
            consulta = consulta.Where(f => f.Estado == estado);

        if (filtro.Desde is { } desde)
            consulta = consulta.Where(f => f.Fecha >= desde);

        if (filtro.Hasta is { } hasta)
            consulta = consulta.Where(f => f.Fecha <= hasta);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            var texto = filtro.Texto.Trim();

            consulta = consulta.Where(f =>
                f.Numero.Contains(texto) ||
                (f.Ncf != null && f.Ncf.Contains(texto)) ||
                f.ClienteNombre.Contains(texto));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderByDescending(f => f.Fecha)
            .ThenByDescending(f => f.Id)
            .Skip(filtro.SaltarRegistros)
            .Take(filtro.TamanoEfectivo)
            .Select(ProyeccionFactura)
            .ToListAsync(ct);

        return new PaginaDe<FacturaListaDto>(items, total, filtro.Pagina, filtro.TamanoEfectivo);
    }

    public async Task<FacturaDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct = default)
    {
        await using var db = await fabrica.CreateDbContextAsync(ct);

        var factura = await db.Facturas
            .AsNoTracking()
            .Include(f => f.Lineas)
                .ThenInclude(l => l.Producto)
                    .ThenInclude(p => p!.UnidadMedida)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (factura is null)
            return null;

        return new FacturaDetalleDto(
            factura.Id, factura.Numero, factura.Ncf, factura.TipoComprobante,
            factura.ClienteNombre, factura.ClienteDocumento, factura.Fecha,
            factura.Estado, factura.Condicion,
            factura.Subtotal, factura.Itbis, factura.Total,
            factura.MontoRecibido, factura.Cambio,
            factura.UsuarioId, factura.MotivoAnulacion,
            [.. factura.Lineas.Select(l => new LineaFacturaDetalleDto(
                l.Producto?.Codigo ?? string.Empty,
                l.Descripcion,
                l.Producto?.UnidadMedida?.Codigo ?? string.Empty,
                l.Producto?.UnidadMedida?.PermiteDecimales ?? false,
                l.Cantidad, l.PrecioUnitario, l.TasaItbis,
                l.Subtotal, l.Itbis, l.Total))]);
    }

    public Task<Factura?> ObtenerConLineasAsync(int id, CancellationToken ct = default) =>
        contexto.Facturas
            .Include(f => f.Lineas)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<string> SugerirNumeroAsync(CancellationToken ct = default)
    {
        var ultimo = await contexto.Facturas
            .AsNoTracking()
            .Where(f => f.Numero.StartsWith("FAC-"))
            .OrderByDescending(f => f.Id)
            .Select(f => f.Numero)
            .FirstOrDefaultAsync(ct);

        return ultimo is not null && int.TryParse(ultimo[4..], out var numero)
            ? $"FAC-{numero + 1:D6}"
            : "FAC-000001";
    }

    public async Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosAsync(
        IEnumerable<int> ids, CancellationToken ct = default)
    {
        var lista = ids.Distinct().ToList();

        // Esta es la lectura de la que depende el cobro: valida la existencia y luego la
        // reescribe. Como el contexto vive lo que dure el circuito, un producto puede llevar
        // horas seguido con la existencia de la venta anterior, y una consulta con seguimiento
        // devuelve esa instancia cacheada descartando lo que acaba de leer de la base. Con eso
        // el cajero cobraba contra un inventario viejo y, al guardar, borraba la mercancia que
        // el almacen habia registrado entre medio. Se recarga cada uno antes de leer.
        foreach (var seguido in contexto.ChangeTracker.Entries<Producto>()
                     .Where(e => lista.Contains(e.Entity.Id))
                     .ToList())
        {
            await seguido.ReloadAsync(ct);
        }

        return await contexto.Productos
            .Where(p => lista.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, ct);
    }

    public void Agregar(Factura factura) => contexto.Facturas.Add(factura);

    public void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos) =>
        contexto.MovimientosInventario.AddRange(movimientos);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);

    /// <summary>
    /// Envuelve la operacion en una transaccion de base de datos.
    /// </summary>
    /// <remarks>
    /// La reserva del NCF es una sentencia UPDATE aparte de SaveChanges. Sin esta
    /// transaccion, un fallo al descontar el inventario dejaria el comprobante consumido
    /// y sin factura que lo respalde: un hueco en la secuencia que hay que justificar
    /// ante la DGII. Con ella, el rollback devuelve el numero.
    ///
    /// El precio es que la fila de la secuencia queda bloqueada mientras dura la venta,
    /// de modo que dos cajas se serializan en ese instante. Para un local con una o dos
    /// cajas eso no se nota; con muchas cajas habria que replantearlo.
    /// </remarks>
    public async Task<T> EnTransaccionAsync<T>(Func<Task<T>> operacion, CancellationToken ct = default)
    {
        // La estrategia de reintentos de EF exige que la transaccion se cree dentro de
        // ella, para poder repetir el bloque completo si la conexion falla.
        var estrategia = contexto.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            await using var transaccion = await contexto.Database.BeginTransactionAsync(ct);

            var resultado = await operacion();

            // Solo se confirma si la operacion salio bien. Un Resultado fallido no es una
            // excepcion, asi que hay que preguntarle explicitamente.
            if (resultado is Resultado r && r.Fallo)
            {
                await transaccion.RollbackAsync(ct);

                // La base revirtio; el contexto del circuito no. Los productos que la venta
                // alcanzo a descontar antes de fallar siguen en memoria con la existencia
                // rebajada, y el siguiente guardado de esta misma ventana los escribiria: una
                // merma que nadie registro. Se los devuelve a lo que dice la base, y lo que
                // solo existia en memoria —la factura que no llego a nacer— queda descartado.
                foreach (var entrada in contexto.ChangeTracker.Entries()
                             .Where(e => e.State is EntityState.Modified or EntityState.Added)
                             .ToList())
                {
                    await entrada.ReloadAsync(ct);
                }

                return resultado;
            }

            await transaccion.CommitAsync(ct);

            return resultado;
        });
    }
}
