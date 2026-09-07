using MiniERP.Application.Common;
using MiniERP.Application.Compras.Contracts;
using MiniERP.Application.Compras.Dtos;
using MiniERP.Domain.Clientes;
using MiniERP.Domain.Compras;

namespace MiniERP.Application.Compras.Services;

public interface IProveedorService
{
    Task<PaginaDe<ProveedorListaDto>> BuscarAsync(FiltroProveedores filtro, CancellationToken ct = default);

    Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default);

    Task<ProveedorFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<ProveedorFormDto> NuevoAsync(CancellationToken ct = default);

    Task<Resultado<int>> CrearAsync(ProveedorFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> ActualizarAsync(ProveedorFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<IReadOnlyList<CuentasPorPagarDto>> ObtenerCuentasPorPagarAsync(CancellationToken ct = default);
}

public class ProveedorService(IProveedorRepositorio proveedores) : IProveedorService
{
    public Task<PaginaDe<ProveedorListaDto>> BuscarAsync(FiltroProveedores filtro, CancellationToken ct = default) =>
        proveedores.BuscarAsync(filtro, ct);

    public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
        proveedores.ObtenerOpcionesAsync(ct);

    public Task<IReadOnlyList<CuentasPorPagarDto>> ObtenerCuentasPorPagarAsync(CancellationToken ct = default) =>
        proveedores.ObtenerCuentasPorPagarAsync(ct);

    public async Task<ProveedorFormDto> NuevoAsync(CancellationToken ct = default) =>
        new() { Codigo = await proveedores.SugerirCodigoAsync(ct) };

    public async Task<ProveedorFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var p = await proveedores.ObtenerPorIdAsync(id, ct);

        if (p is null)
            return null;

        return new ProveedorFormDto
        {
            Id = p.Id,
            Codigo = p.Codigo,
            Nombre = p.Nombre,
            TipoDocumento = p.TipoDocumento,
            NumeroDocumento = p.NumeroDocumento,
            Telefono = p.Telefono,
            Email = p.Email,
            Direccion = p.Direccion,
            Contacto = p.Contacto,
            DiasCredito = p.DiasCredito,
            Activo = p.Activo,
            BalanceActual = p.BalanceActual
        };
    }

    public async Task<Resultado<int>> CrearAsync(ProveedorFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var validacion = await ValidarAsync(form, ct);

        if (validacion.Fallo)
            return Resultado.Falla<int>(validacion.Error!);

        var proveedor = new Proveedor
        {
            Codigo = form.Codigo.Trim().ToUpperInvariant(),
            Nombre = form.Nombre.Trim(),
            TipoDocumento = form.TipoDocumento,
            NumeroDocumento = NormalizarDocumento(form.NumeroDocumento),
            Telefono = Normalizar(form.Telefono),
            Email = Normalizar(form.Email),
            Direccion = Normalizar(form.Direccion),
            Contacto = Normalizar(form.Contacto),
            DiasCredito = form.DiasCredito,
            Activo = form.Activo,
            BalanceActual = 0,
            CreadoPor = usuarioId
        };

        proveedores.Agregar(proveedor);
        await proveedores.GuardarAsync(ct);

        return Resultado.Ok(proveedor.Id);
    }

    public async Task<Resultado> ActualizarAsync(ProveedorFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var proveedor = await proveedores.ObtenerPorIdAsync(form.Id, ct);

        if (proveedor is null)
            return Resultado.Falla("El proveedor no existe.");

        var validacion = await ValidarAsync(form, ct);

        if (validacion.Fallo)
            return validacion;

        proveedor.Codigo = form.Codigo.Trim().ToUpperInvariant();
        proveedor.Nombre = form.Nombre.Trim();
        proveedor.TipoDocumento = form.TipoDocumento;
        proveedor.NumeroDocumento = NormalizarDocumento(form.NumeroDocumento);
        proveedor.Telefono = Normalizar(form.Telefono);
        proveedor.Email = Normalizar(form.Email);
        proveedor.Direccion = Normalizar(form.Direccion);
        proveedor.Contacto = Normalizar(form.Contacto);
        proveedor.DiasCredito = form.DiasCredito;
        proveedor.Activo = form.Activo;
        proveedor.FechaModificacion = RelojSimulado.UtcNow;
        proveedor.ModificadoPor = usuarioId;

        // El balance no se toca: lo mueven las compras a credito y los pagos.
        await proveedores.GuardarAsync(ct);

        return Resultado.Ok();
    }

    private async Task<Resultado> ValidarAsync(ProveedorFormDto form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Codigo))
            return Resultado.Falla("El codigo es obligatorio.");

        if (string.IsNullOrWhiteSpace(form.Nombre))
            return Resultado.Falla("El nombre es obligatorio.");

        if (form.DiasCredito < 0)
            return Resultado.Falla("Los dias de credito no pueden ser negativos.");

        var excluir = form.Id == 0 ? (int?)null : form.Id;
        var codigo = form.Codigo.Trim().ToUpperInvariant();

        if (await proveedores.ExisteCodigoAsync(codigo, excluir, ct))
            return Resultado.Falla($"Ya existe un proveedor con el codigo '{codigo}'.");

        // Se pregunta a la entidad del dominio en vez de repetir aqui las reglas del documento.
        var sonda = new Proveedor
        {
            TipoDocumento = form.TipoDocumento,
            NumeroDocumento = NormalizarDocumento(form.NumeroDocumento)
        };

        if (!sonda.DocumentoTieneFormatoValido())
            return Resultado.Falla(form.TipoDocumento switch
            {
                TipoDocumento.Cedula => "La cedula debe tener 11 digitos.",
                TipoDocumento.Rnc => "El RNC debe tener 9 digitos.",
                TipoDocumento.Pasaporte => "El pasaporte debe tener entre 5 y 20 caracteres.",
                _ => "El numero de documento no es valido para el tipo elegido."
            });

        var documento = NormalizarDocumento(form.NumeroDocumento);

        if (documento is not null && await proveedores.ExisteDocumentoAsync(documento, excluir, ct))
            return Resultado.Falla($"Ya hay un proveedor registrado con el documento '{documento}'.");

        return Resultado.Ok();
    }

    private static string? NormalizarDocumento(string? documento) =>
        string.IsNullOrWhiteSpace(documento) ? null : documento.Replace("-", string.Empty).Trim();

    private static string? Normalizar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
