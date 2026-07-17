using MiniERP.Application.Clientes.Contracts;
using MiniERP.Application.Clientes.Dtos;
using MiniERP.Application.Common;
using MiniERP.Domain.Clientes;

namespace MiniERP.Application.Clientes.Services;

public interface IClienteService
{
    Task<PaginaDe<ClienteListaDto>> BuscarAsync(FiltroClientes filtro, CancellationToken ct = default);

    Task<ResumenCarteraDto> ObtenerResumenAsync(CancellationToken ct = default);

    Task<ClienteFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default);

    Task<ClienteFormDto> NuevoAsync(CancellationToken ct = default);

    Task<Resultado<int>> CrearAsync(ClienteFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<Resultado> ActualizarAsync(ClienteFormDto form, string? usuarioId, CancellationToken ct = default);

    Task<IReadOnlyList<TransaccionEstadoCuentaDto>> ObtenerEstadoCuentaAsync(int clienteId, CancellationToken ct = default);
}

/// <summary>
/// Casos de uso del modulo de clientes.
/// </summary>
public class ClienteService(IClienteRepositorio clientes) : IClienteService
{
    public Task<PaginaDe<ClienteListaDto>> BuscarAsync(FiltroClientes filtro, CancellationToken ct = default) =>
        clientes.BuscarAsync(filtro, ct);

    public Task<ResumenCarteraDto> ObtenerResumenAsync(CancellationToken ct = default) =>
        clientes.ObtenerResumenAsync(ct);

    public async Task<ClienteFormDto> NuevoAsync(CancellationToken ct = default) =>
        new() { Codigo = await clientes.SugerirCodigoAsync(ct) };

    public async Task<ClienteFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default)
    {
        var cliente = await clientes.ObtenerPorIdAsync(id, ct);

        if (cliente is null)
            return null;

        return new ClienteFormDto
        {
            Id = cliente.Id,
            Codigo = cliente.Codigo,
            Nombre = cliente.Nombre,
            TipoDocumento = cliente.TipoDocumento,
            NumeroDocumento = cliente.NumeroDocumento,
            TipoComprobante = cliente.TipoComprobante,
            Telefono = cliente.Telefono,
            Email = cliente.Email,
            Direccion = cliente.Direccion,
            LimiteCredito = cliente.LimiteCredito,
            DiasCredito = cliente.DiasCredito,
            Activo = cliente.Activo,
            BalanceActual = cliente.BalanceActual
        };
    }

    public async Task<Resultado<int>> CrearAsync(ClienteFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var validacion = await ValidarAsync(form, ct);

        if (validacion.Fallo)
            return Resultado.Falla<int>(validacion.Error!);

        var cliente = new Cliente
        {
            Codigo = form.Codigo.Trim().ToUpperInvariant(),
            Nombre = form.Nombre.Trim(),
            TipoDocumento = form.TipoDocumento,
            NumeroDocumento = NormalizarDocumento(form.NumeroDocumento),
            TipoComprobante = form.TipoComprobante,
            Telefono = Normalizar(form.Telefono),
            Email = Normalizar(form.Email),
            Direccion = Normalizar(form.Direccion),
            LimiteCredito = form.LimiteCredito,
            DiasCredito = form.DiasCredito,
            Activo = form.Activo,
            BalanceActual = 0,
            CreadoPor = usuarioId
        };

        clientes.Agregar(cliente);
        await clientes.GuardarAsync(ct);

        return Resultado.Ok(cliente.Id);
    }

    public async Task<Resultado> ActualizarAsync(ClienteFormDto form, string? usuarioId, CancellationToken ct = default)
    {
        var cliente = await clientes.ObtenerPorIdAsync(form.Id, ct);

        if (cliente is null)
            return Resultado.Falla("El cliente no existe.");

        var validacion = await ValidarAsync(form, ct);

        if (validacion.Fallo)
            return validacion;

        // Bajar el limite por debajo de lo que el cliente ya debe lo dejaria excedido
        // sin haber comprado nada. Es una decision del negocio, no un error: se avisa
        // y se permite, porque a veces es justo lo que se quiere hacer.
        cliente.Codigo = form.Codigo.Trim().ToUpperInvariant();
        cliente.Nombre = form.Nombre.Trim();
        cliente.TipoDocumento = form.TipoDocumento;
        cliente.NumeroDocumento = NormalizarDocumento(form.NumeroDocumento);
        cliente.TipoComprobante = form.TipoComprobante;
        cliente.Telefono = Normalizar(form.Telefono);
        cliente.Email = Normalizar(form.Email);
        cliente.Direccion = Normalizar(form.Direccion);
        cliente.LimiteCredito = form.LimiteCredito;
        cliente.DiasCredito = form.DiasCredito;
        cliente.Activo = form.Activo;
        cliente.FechaModificacion = DateTime.UtcNow;
        cliente.ModificadoPor = usuarioId;

        // El balance no se toca: lo mueven las ventas a credito y los cobros.
        await clientes.GuardarAsync(ct);

        return Resultado.Ok();
    }

    public async Task<IReadOnlyList<TransaccionEstadoCuentaDto>> ObtenerEstadoCuentaAsync(int clienteId, CancellationToken ct = default)
    {
        // NOTA: Para no violar la separacion de capas y dado que no hay un repositorio de Facturas todavia,
        // podemos utilizar una consulta sobre el repositorio de cobros y extender el repositorio de clientes.
        // Pero lo mas directo y eficiente para el Estado de Cuenta es exponerlo consultando la base de datos
        // o implementando una consulta consolidada. 
        // Como Dionis tiene acceso a IClienteRepositorio, agregamos un metodo en IClienteRepositorio para obtener el historico.
        return await clientes.ObtenerEstadoCuentaAsync(clienteId, ct);
    }

    private async Task<Resultado> ValidarAsync(ClienteFormDto form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Codigo))
            return Resultado.Falla("El codigo es obligatorio.");

        if (string.IsNullOrWhiteSpace(form.Nombre))
            return Resultado.Falla("El nombre es obligatorio.");

        if (form.LimiteCredito < 0)
            return Resultado.Falla("El limite de credito no puede ser negativo.");

        if (form.DiasCredito < 0)
            return Resultado.Falla("Los dias de credito no pueden ser negativos.");

        var excluir = form.Id == 0 ? (int?)null : form.Id;
        var codigo = form.Codigo.Trim().ToUpperInvariant();

        if (await clientes.ExisteCodigoAsync(codigo, excluir, ct))
            return Resultado.Falla($"Ya existe un cliente con el codigo '{codigo}'.");

        // Se valida sobre la entidad del dominio para no duplicar aqui las reglas
        // del documento y del comprobante.
        var sonda = new Cliente
        {
            TipoDocumento = form.TipoDocumento,
            NumeroDocumento = NormalizarDocumento(form.NumeroDocumento),
            TipoComprobante = form.TipoComprobante
        };

        if (!sonda.DocumentoTieneFormatoValido())
            return Resultado.Falla(ErrorDeDocumento(form.TipoDocumento));

        if (!sonda.ComprobanteEstaSustentado)
            return Resultado.Falla(
                "El comprobante de credito fiscal exige un RNC: sin el, la DGII no lo acepta.");

        var documento = NormalizarDocumento(form.NumeroDocumento);

        if (documento is not null && await clientes.ExisteDocumentoAsync(documento, excluir, ct))
            return Resultado.Falla($"Ya hay un cliente registrado con el documento '{documento}'.");

        return Resultado.Ok();
    }

    private static string ErrorDeDocumento(TipoDocumento tipo) => tipo switch
    {
        TipoDocumento.Cedula => "La cedula debe tener 11 digitos.",
        TipoDocumento.Rnc => "El RNC debe tener 9 digitos.",
        TipoDocumento.Pasaporte => "El pasaporte debe tener entre 5 y 20 caracteres.",
        _ => "El numero de documento no es valido para el tipo elegido."
    };

    /// <summary>Guarda el documento sin guiones, para que el indice unico los detecte iguales.</summary>
    private static string? NormalizarDocumento(string? documento) =>
        string.IsNullOrWhiteSpace(documento)
            ? null
            : documento.Replace("-", string.Empty).Trim();

    private static string? Normalizar(string? texto) =>
        string.IsNullOrWhiteSpace(texto) ? null : texto.Trim();
}
