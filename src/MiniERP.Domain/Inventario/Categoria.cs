using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Inventario;

/// <summary>
/// Agrupacion de productos: Lacteos, Bebidas, Limpieza, Carnes.
/// </summary>
public class Categoria : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Producto> Productos { get; set; } = [];
}
