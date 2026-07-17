using MiniERP.Application.Common;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;

namespace MiniERP.Application.Compras.Contracts;

public interface IDevolucionCompraRepositorio
{
    /// <summary>Trae la devolucion con sus lineas, con seguimiento, para poder confirmarla.</summary>
    Task<DevolucionCompra?> ObtenerConLineasAsync(int id, CancellationToken ct = default);

    Task<PaginaDe<DevolucionCompraListaDto>> BuscarAsync(FiltroDevoluciones filtro, CancellationToken ct = default);

    /// <summary>Devolucion en modo lectura, para ver su detalle.</summary>
    Task<DevolucionCompraFormDto?> ObtenerParaVerAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Arma el esqueleto de una devolucion desde una compra recibida: encabezado y lineas
    /// con el producto, la cantidad comprada y el costo congelado. Devuelve null si la compra
    /// no existe o no esta recibida (una orden en borrador no tiene mercancia que devolver).
    /// El servicio completa el tope devolvible y el numero.
    /// </summary>
    Task<DevolucionCompraFormDto?> ObtenerCompraParaDevolverAsync(int compraId, CancellationToken ct = default);

    /// <summary>Cantidad comprada por linea de la compra, indexada por LineaCompraId.</summary>
    Task<IReadOnlyDictionary<int, decimal>> ObtenerCompradoPorLineaCompraAsync(int compraId, CancellationToken ct = default);

    /// <summary>
    /// Suma, por linea de compra, lo ya devuelto en devoluciones CONFIRMADAS de esa compra,
    /// indexada por LineaCompraId. Con esto el servicio calcula cuanto queda por devolver.
    /// </summary>
    Task<IReadOnlyDictionary<int, decimal>> ObtenerDevueltoPorLineaCompraAsync(int compraId, CancellationToken ct = default);

    /// <summary>Los productos de las lineas, con seguimiento, para que confirmar los pueda modificar.</summary>
    Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosDeAsync(DevolucionCompra devolucion, CancellationToken ct = default);

    Task<string> SugerirNumeroAsync(CancellationToken ct = default);

    void Agregar(DevolucionCompra devolucion);

    void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos);

    void EliminarLineas(IEnumerable<LineaDevolucionCompra> lineas);

    Task<int> GuardarAsync(CancellationToken ct = default);
}
