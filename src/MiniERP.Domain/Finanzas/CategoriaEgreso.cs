using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Tipo de gasto operativo: Alquiler, Luz, Agua, Sueldos, Transporte.
/// Catalogo editable; el comerciante agrega los suyos.
/// </summary>
public class CategoriaEgreso : EntidadBase
{
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public ICollection<Egreso> Egresos { get; set; } = [];
}
