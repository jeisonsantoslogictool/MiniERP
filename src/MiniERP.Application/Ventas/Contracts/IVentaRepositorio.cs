using MiniERP.Application.Common;
using MiniERP.Application.Ventas.Dtos;
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Ventas;

namespace MiniERP.Application.Ventas.Contracts;

public interface IVentaRepositorio
{
    Task<IReadOnlyList<ProductoParaVentaDto>> BuscarProductosAsync(string texto, int maximo = 15, CancellationToken ct = default);

    /// <summary>Busca por codigo de barras exacto: es lo que dispara el escaner.</summary>
    Task<ProductoParaVentaDto?> ObtenerPorCodigoBarrasAsync(string codigoBarras, CancellationToken ct = default);

    Task<IReadOnlyList<ClienteParaVentaDto>> BuscarClientesAsync(string texto, int maximo = 10, CancellationToken ct = default);

    Task<PaginaDe<FacturaListaDto>> BuscarAsync(FiltroFacturas filtro, CancellationToken ct = default);

    Task<FacturaDetalleDto?> ObtenerDetalleAsync(int id, CancellationToken ct = default);

    Task<Factura?> ObtenerConLineasAsync(int id, CancellationToken ct = default);

    Task<string> SugerirNumeroAsync(CancellationToken ct = default);

    /// <summary>Los productos indicados, con seguimiento, para que emitir pueda descontarlos.</summary>
    Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosAsync(IEnumerable<int> ids, CancellationToken ct = default);

    void Agregar(Factura factura);

    void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos);

    Task<int> GuardarAsync(CancellationToken ct = default);

    /// <summary>
    /// Ejecuta la operacion dentro de una transaccion.
    /// </summary>
    /// <remarks>
    /// Emitir toca varias cosas que deben pasar juntas o no pasar: reservar el NCF,
    /// descontar el inventario y cargar el balance del cliente. La reserva del NCF es
    /// una sentencia aparte de SaveChanges, asi que sin una transaccion explicita un
    /// fallo posterior dejaria el comprobante consumido y sin factura que lo respalde.
    /// </remarks>
    Task<T> EnTransaccionAsync<T>(Func<Task<T>> operacion, CancellationToken ct = default);
}
