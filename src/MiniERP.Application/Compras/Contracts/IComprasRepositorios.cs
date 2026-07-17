using MiniERP.Application.Common;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;

namespace MiniERP.Application.Compras.Contracts;

public interface IProveedorRepositorio
{
    Task<Proveedor?> ObtenerPorIdAsync(int id, CancellationToken ct = default);

    Task<PaginaDe<ProveedorListaDto>> BuscarAsync(FiltroProveedores filtro, CancellationToken ct = default);

    Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default);

    Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default);

    Task<bool> ExisteDocumentoAsync(string numeroDocumento, int? excluirId = null, CancellationToken ct = default);

    Task<string> SugerirCodigoAsync(CancellationToken ct = default);

    void Agregar(Proveedor proveedor);

    Task<int> GuardarAsync(CancellationToken ct = default);

    Task<IReadOnlyList<CuentasPorPagarDto>> ObtenerCuentasPorPagarAsync(CancellationToken ct = default);
}

public interface ICompraRepositorio
{
    /// <summary>Trae la compra con sus lineas, con seguimiento, para poder recibirla.</summary>
    Task<Compra?> ObtenerConLineasAsync(int id, CancellationToken ct = default);

    Task<PaginaDe<CompraListaDto>> BuscarAsync(FiltroCompras filtro, CancellationToken ct = default);

    Task<CompraFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<string> SugerirNumeroAsync(CancellationToken ct = default);

    /// <summary>Busca productos para agregar a la orden. Devuelve pocos: es un autocompletar.</summary>
    Task<IReadOnlyList<ProductoParaCompraDto>> BuscarProductosAsync(string texto, int maximo = 15, CancellationToken ct = default);

    /// <summary>Los productos de las lineas, con seguimiento, para que recibir los pueda modificar.</summary>
    Task<IReadOnlyDictionary<int, Producto>> ObtenerProductosDeAsync(Compra compra, CancellationToken ct = default);

    void Agregar(Compra compra);

    void AgregarMovimientos(IEnumerable<MovimientoInventario> movimientos);

    void EliminarLineas(IEnumerable<LineaCompra> lineas);

    Task<int> GuardarAsync(CancellationToken ct = default);
}
