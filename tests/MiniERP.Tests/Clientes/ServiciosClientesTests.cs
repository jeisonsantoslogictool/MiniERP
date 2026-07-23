using MiniERP.Application.Clientes.Contracts;
using MiniERP.Application.Clientes.Dtos;
using MiniERP.Application.Clientes.Services;
using MiniERP.Application.Common;
using MiniERP.Domain.Clientes;

namespace MiniERP.Tests.Clientes;

public class ServiciosClientesTests
{
    [Fact]
    public async Task Crear_normaliza_documento_y_no_permite_balance_inicial()
    {
        var repo = new ClienteRepoFalso();
        var servicio = new ClienteService(repo);

        var resultado = await servicio.CrearAsync(new ClienteFormDto
        {
            Codigo = " cli-900 ",
            Nombre = " Cliente prueba ",
            TipoDocumento = TipoDocumento.Cedula,
            NumeroDocumento = "001-1234567-8",
            TipoComprobante = TipoComprobante.Consumo,
            LimiteCredito = 5_000,
            BalanceActual = 999,
            DiasCredito = 15
        }, "dionis@test");

        Assert.False(resultado.Fallo);
        Assert.NotNull(repo.Cliente);
        Assert.Equal("CLI-900", repo.Cliente.Codigo);
        Assert.Equal("00112345678", repo.Cliente.NumeroDocumento);
        Assert.Equal(0, repo.Cliente.BalanceActual);
        Assert.Equal("dionis@test", repo.Cliente.CreadoPor);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task Crear_rechaza_codigo_duplicado_y_credito_fiscal_sin_rnc()
    {
        var repo = new ClienteRepoFalso { CodigoExiste = true };
        var servicio = new ClienteService(repo);
        var form = new ClienteFormDto { Codigo = "CLI-1", Nombre = "Duplicado" };

        var duplicado = await servicio.CrearAsync(form, null);

        Assert.True(duplicado.Fallo);
        Assert.Contains("Ya existe", duplicado.Error);

        repo.CodigoExiste = false;
        form.TipoComprobante = TipoComprobante.CreditoFiscal;
        var sinRnc = await servicio.CrearAsync(form, null);

        Assert.True(sinRnc.Fallo);
        Assert.Contains("RNC", sinRnc.Error);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task Actualizar_no_modifica_el_balance()
    {
        var repo = new ClienteRepoFalso
        {
            Cliente = new Cliente
            {
                Id = 7,
                Codigo = "CLI-7",
                Nombre = "Original",
                BalanceActual = 350
            }
        };
        var servicio = new ClienteService(repo);

        var resultado = await servicio.ActualizarAsync(new ClienteFormDto
        {
            Id = 7,
            Codigo = "CLI-7",
            Nombre = "Actualizado",
            BalanceActual = 0
        }, "dionis@test");

        Assert.False(resultado.Fallo);
        Assert.Equal("Actualizado", repo.Cliente.Nombre);
        Assert.Equal(350, repo.Cliente.BalanceActual);
    }

    [Fact]
    public async Task Registrar_cobro_persiste_rastro_y_rechaza_exceso()
    {
        var clientes = new ClienteRepoFalso
        {
            Cliente = new Cliente { Id = 8, BalanceActual = 500 }
        };
        var cobros = new CobroRepoFalso();
        var servicio = new CobrosService(cobros, clientes);

        var correcto = await servicio.RegistrarCobroAsync(new CobroFormDto
        {
            ClienteId = 8,
            Monto = 200,
            Observacion = " transferencia "
        }, "dionis@test");

        Assert.False(correcto.Fallo);
        Assert.Equal(300, clientes.Cliente.BalanceActual);
        Assert.Equal(500, cobros.Cobro!.BalanceAnterior);
        Assert.Equal(300, cobros.Cobro.BalanceResultante);
        Assert.Equal("transferencia", cobros.Cobro.Observacion);
        Assert.Equal(1, cobros.Guardados);

        var excesivo = await servicio.RegistrarCobroAsync(
            new CobroFormDto { ClienteId = 8, Monto = 301 }, "dionis@test");

        Assert.True(excesivo.Fallo);
        Assert.Equal(300, clientes.Cliente.BalanceActual);
        Assert.Equal(1, cobros.Guardados);
    }

    private sealed class ClienteRepoFalso : IClienteRepositorio
    {
        public Cliente? Cliente { get; set; }
        public bool CodigoExiste { get; set; }
        public bool DocumentoExiste { get; set; }
        public int Guardados { get; private set; }

        public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Cliente?.Id == id ? Cliente : null);

        public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
            Task.FromResult(CodigoExiste);

        public Task<bool> ExisteDocumentoAsync(string numeroDocumento, int? excluirId = null, CancellationToken ct = default) =>
            Task.FromResult(DocumentoExiste);

        public Task<string> SugerirCodigoAsync(CancellationToken ct = default) =>
            Task.FromResult("CLI-0001");

        public void Agregar(Cliente cliente)
        {
            Cliente = cliente;
            Cliente.Id = 1;
        }

        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }

        public Task<PaginaDe<ClienteListaDto>> BuscarAsync(FiltroClientes filtro, CancellationToken ct = default) =>
            Task.FromResult(new PaginaDe<ClienteListaDto>([], 0, 1, 25));

        public Task<ResumenCarteraDto> ObtenerResumenAsync(CancellationToken ct = default) =>
            Task.FromResult(new ResumenCarteraDto(0, 0, 0, 0, 0, 0));

        public Task<IReadOnlyList<TransaccionEstadoCuentaDto>> ObtenerEstadoCuentaAsync(
            int clienteId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<TransaccionEstadoCuentaDto>>([]);

        public Task<IReadOnlyList<CuentasPorCobrarDto>> ObtenerCuentasPorCobrarAsync(
            CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CuentasPorCobrarDto>>([]);
    }

    private sealed class CobroRepoFalso : ICobrosRepositorio
    {
        public Cobro? Cobro { get; private set; }
        public int Guardados { get; private set; }

        public Task<Cobro?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(Cobro?.Id == id ? Cobro : null);

        public Task<IReadOnlyList<Cobro>> ObtenerPorClienteAsync(
            int clienteId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Cobro>>(Cobro is null ? [] : [Cobro]);

        public void Agregar(Cobro cobro)
        {
            Cobro = cobro;
            Cobro.Id = 1;
        }

        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }
    }
}
