using MiniERP.Domain.Inventario;

namespace MiniERP.Application.Inventario.Dtos;

/// <summary>Fila del listado de productos.</summary>
public record ProductoListaDto(
    int Id,
    string Codigo,
    string? CodigoBarras,
    string Descripcion,
    string Categoria,
    string UnidadMedida,
    decimal Costo,
    decimal PrecioVenta,
    decimal PrecioConItbis,
    decimal MargenPorcentaje,
    decimal Existencia,
    decimal ExistenciaMinima,
    bool PermiteDecimales,
    bool RequiereReabastecimiento,
    bool Agotado,
    bool Activo);

/// <summary>Datos que captura el formulario de producto.</summary>
public class ProductoFormDto
{
    public int Id { get; set; }

    public string Codigo { get; set; } = string.Empty;

    public string? CodigoBarras { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public int CategoriaId { get; set; }

    public int UnidadMedidaId { get; set; }

    public decimal Costo { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal TasaItbis { get; set; } = Producto.TasaItbisGeneral;

    /// <summary>
    /// Existencia con que entra un producto nuevo. Al editar no se toca: la existencia
    /// solo cambia por movimiento, para que nunca haya un saldo sin respaldo.
    /// </summary>
    public decimal ExistenciaInicial { get; set; }

    public decimal ExistenciaMinima { get; set; }

    public bool ManejaInventario { get; set; } = true;

    public bool Activo { get; set; } = true;
}

/// <summary>Solicitud de ajuste, merma o entrada manual de existencia.</summary>
public record AjusteExistenciaDto(
    int ProductoId,
    TipoMovimiento Tipo,
    decimal Cantidad,
    string? Motivo);

/// <summary>Fila del kardex de un producto.</summary>
public record MovimientoDto(
    DateTime Fecha,
    TipoMovimiento Tipo,
    decimal Cantidad,
    bool EsEntrada,
    decimal ExistenciaAnterior,
    decimal ExistenciaResultante,
    string? Motivo,
    string? Usuario);

/// <summary>Unidad de medida, con lo que la pantalla necesita para validar la cantidad.</summary>
public record UnidadMedidaDto(int Id, string Codigo, string Nombre, bool PermiteDecimales, int CantidadDecimales);
