namespace MiniERP.Application.Common;

/// <summary>
/// Opcion de un desplegable. Vive en Common porque la usan todos los modulos:
/// duplicarla por modulo provocaria ambiguedad en las paginas que importan varios.
/// </summary>
public record OpcionDto(int Id, string Texto);
