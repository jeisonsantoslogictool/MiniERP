using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MiniERP.Application.Clientes.Contracts;
using MiniERP.Application.Clientes.Dtos;
using MiniERP.Application.Common;
using MiniERP.Domain.Clientes;

namespace MiniERP.Infrastructure.Persistence.Repositories;

public class ClienteRepositorio(MiniErpDbContext contexto) : IClienteRepositorio
{
    /// <summary>
    /// Proyeccion del listado. Va como Expression para que EF la traduzca a SQL y no
    /// traiga la cartera completa a memoria. Las propiedades calculadas del dominio se
    /// repiten aqui en terminos de columnas por la misma razon.
    /// </summary>
    private static readonly Expression<Func<Cliente, ClienteListaDto>> Proyeccion =
        c => new ClienteListaDto(
            c.Id,
            c.Codigo,
            c.Nombre,
            c.TipoDocumento,
            c.NumeroDocumento,
            c.TipoComprobante,
            c.Telefono,
            c.LimiteCredito,
            c.BalanceActual,
            c.LimiteCredito - c.BalanceActual < 0 ? 0 : c.LimiteCredito - c.BalanceActual,
            c.LimiteCredito > 0,
            c.BalanceActual > c.LimiteCredito,
            c.Activo);

    public Task<Cliente?> ObtenerPorIdAsync(int id, CancellationToken ct = default) =>
        contexto.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<PaginaDe<ClienteListaDto>> BuscarAsync(FiltroClientes filtro, CancellationToken ct = default)
    {
        var consulta = contexto.Clientes.AsNoTracking();

        if (filtro.SoloActivos)
            consulta = consulta.Where(c => c.Activo);

        if (filtro.SoloConCredito)
            consulta = consulta.Where(c => c.LimiteCredito > 0);

        if (filtro.SoloConDeuda)
            consulta = consulta.Where(c => c.BalanceActual > 0);

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            // El usuario puede teclear el documento con guiones aunque se guarde sin
            // ellos, asi que se busca por ambas formas.
            var texto = filtro.Texto.Trim();
            var sinGuiones = texto.Replace("-", string.Empty);

            consulta = consulta.Where(c =>
                c.Codigo.Contains(texto) ||
                c.Nombre.Contains(texto) ||
                (c.NumeroDocumento != null && c.NumeroDocumento.Contains(sinGuiones)) ||
                (c.Telefono != null && c.Telefono.Contains(texto)));
        }

        var total = await consulta.CountAsync(ct);

        var items = await consulta
            .OrderBy(c => c.Nombre)
            .Skip(filtro.SaltarRegistros)
            .Take(filtro.TamanoEfectivo)
            .Select(Proyeccion)
            .ToListAsync(ct);

        return new PaginaDe<ClienteListaDto>(items, total, filtro.Pagina, filtro.TamanoEfectivo);
    }

    public async Task<ResumenCarteraDto> ObtenerResumenAsync(CancellationToken ct = default)
    {
        // Una sola consulta agregada en vez de traer la cartera y contar en memoria.
        var activos = contexto.Clientes.AsNoTracking().Where(c => c.Activo);

        var datos = await activos
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                ConCredito = g.Count(c => c.LimiteCredito > 0),
                ConDeuda = g.Count(c => c.BalanceActual > 0),
                Excedidos = g.Count(c => c.BalanceActual > c.LimiteCredito),
                Deuda = g.Sum(c => c.BalanceActual),
                Otorgado = g.Sum(c => c.LimiteCredito)
            })
            .FirstOrDefaultAsync(ct);

        return datos is null
            ? new ResumenCarteraDto(0, 0, 0, 0, 0, 0)
            : new ResumenCarteraDto(
                datos.Total, datos.ConCredito, datos.ConDeuda,
                datos.Excedidos, datos.Deuda, datos.Otorgado);
    }

    public Task<bool> ExisteCodigoAsync(string codigo, int? excluirId = null, CancellationToken ct = default) =>
        contexto.Clientes
            .AsNoTracking()
            .AnyAsync(c => c.Codigo == codigo && (excluirId == null || c.Id != excluirId), ct);

    public Task<bool> ExisteDocumentoAsync(string numeroDocumento, int? excluirId = null, CancellationToken ct = default) =>
        contexto.Clientes
            .AsNoTracking()
            .AnyAsync(c => c.NumeroDocumento == numeroDocumento && (excluirId == null || c.Id != excluirId), ct);

    /// <summary>
    /// Propone el proximo correlativo con el formato CLI-0001. Es una sugerencia que el
    /// usuario puede cambiar; el indice unico es lo que realmente impide duplicados.
    /// </summary>
    public async Task<string> SugerirCodigoAsync(CancellationToken ct = default)
    {
        var ultimo = await contexto.Clientes
            .AsNoTracking()
            .Where(c => c.Codigo.StartsWith("CLI-"))
            .OrderByDescending(c => c.Id)
            .Select(c => c.Codigo)
            .FirstOrDefaultAsync(ct);

        if (ultimo is null || !int.TryParse(ultimo[4..], out var numero))
            return "CLI-0001";

        return $"CLI-{numero + 1:D4}";
    }

    public void Agregar(Cliente cliente) => contexto.Clientes.Add(cliente);

    public Task<int> GuardarAsync(CancellationToken ct = default) => contexto.SaveChangesAsync(ct);

    public async Task<IReadOnlyList<TransaccionEstadoCuentaDto>> ObtenerEstadoCuentaAsync(int clienteId, CancellationToken ct = default)
    {
        // 1. Obtener todas las facturas a crédito de este cliente (que no estén anuladas)
        var facturas = await contexto.Facturas
            .AsNoTracking()
            .Where(f => f.ClienteId == clienteId && f.Condicion == Domain.Shared.CondicionPago.Credito && f.Estado != Domain.Ventas.EstadoFactura.Anulada)
            .Select(f => new
            {
                f.Fecha,
                Referencia = f.Ncf ?? f.Numero,
                Concepto = "Venta a Crédito",
                Debito = f.Total,
                Credito = 0m
            })
            .ToListAsync(ct);

        // 2. Obtener todos los cobros aplicados a este cliente
        var cobros = await contexto.Cobros
            .AsNoTracking()
            .Where(c => c.ClienteId == clienteId)
            .Select(c => new
            {
                c.Fecha,
                Referencia = "COB-" + c.Id.ToString().PadLeft(6, '0'),
                Concepto = string.IsNullOrEmpty(c.Observacion) ? "Cobro / Abono" : c.Observacion,
                Debito = 0m,
                Credito = c.Monto
            })
            .ToListAsync(ct);

        // 3. Unir ambos listados y ordenar cronológicamente
        var transaccionesCombinadas = facturas.Concat(cobros)
            .OrderBy(t => t.Fecha)
            .ToList();

        // 4. Calcular el balance acumulado resultante paso a paso
        var resultado = new List<TransaccionEstadoCuentaDto>();
        decimal balanceAcumulado = 0m;

        foreach (var t in transaccionesCombinadas)
        {
            balanceAcumulado += t.Debito - t.Credito;
            resultado.Add(new TransaccionEstadoCuentaDto(
                t.Fecha,
                t.Referencia,
                t.Concepto,
                t.Debito,
                t.Credito,
                balanceAcumulado
            ));
        }

        // Devolver ordenado de más reciente a más antiguo para que el estado de cuenta
        // muestre arriba los movimientos más nuevos.
        resultado.Reverse();
        return resultado;
    }

    public async Task<IReadOnlyList<CuentasPorCobrarDto>> ObtenerCuentasPorCobrarAsync(CancellationToken ct = default)
    {
        var clientesConDeuda = await contexto.Clientes
            .AsNoTracking()
            .Where(c => c.Activo && c.BalanceActual > 0)
            .Select(c => new
            {
                c.Id,
                c.Codigo,
                c.Nombre,
                c.Telefono,
                c.BalanceActual,
                c.DiasCredito
            })
            .ToListAsync(ct);

        if (clientesConDeuda.Count == 0)
            return [];

        var ids = clientesConDeuda.Select(c => c.Id).ToList();

        var facturas = await contexto.Facturas
            .AsNoTracking()
            .Where(f => f.ClienteId != null && ids.Contains(f.ClienteId.Value) && f.Condicion == Domain.Shared.CondicionPago.Credito && f.Estado != Domain.Ventas.EstadoFactura.Anulada)
            .Select(f => new { ClienteId = f.ClienteId.Value, f.Fecha, f.Total })
            .ToListAsync(ct);

        var cobros = await contexto.Cobros
            .AsNoTracking()
            .Where(cb => ids.Contains(cb.ClienteId))
            .GroupBy(cb => cb.ClienteId)
            .Select(g => new { ClienteId = g.Key, TotalCobrado = g.Sum(c => c.Monto) })
            .ToListAsync(ct);

        var facturasPorCliente = facturas.GroupBy(f => f.ClienteId).ToDictionary(g => g.Key, g => g.OrderBy(f => f.Fecha).ToList());
        var cobrosPorCliente = cobros.ToDictionary(c => c.ClienteId, c => c.TotalCobrado);

        var resultado = new List<CuentasPorCobrarDto>(clientesConDeuda.Count);
        var fechaActual = DateTime.UtcNow.Date;

        foreach (var c in clientesConDeuda)
        {
            decimal totalCobrado = cobrosPorCliente.TryGetValue(c.Id, out var tc) ? tc : 0m;
            int diasVencidos = 0;
            decimal montoVencido = 0m;

            if (facturasPorCliente.TryGetValue(c.Id, out var facturasCliente))
            {
                foreach (var f in facturasCliente)
                {
                    if (totalCobrado >= f.Total)
                    {
                        totalCobrado -= f.Total;
                        continue;
                    }

                    var montoPendiente = f.Total - totalCobrado;
                    totalCobrado = 0;

                    var fechaVencimiento = f.Fecha.Date.AddDays(c.DiasCredito);
                    var dias = (fechaActual - fechaVencimiento).Days;

                    if (dias > 0)
                    {
                        montoVencido += montoPendiente;
                        if (dias > diasVencidos)
                            diasVencidos = dias;
                    }
                }
            }

            resultado.Add(new CuentasPorCobrarDto(
                c.Id,
                c.Codigo,
                c.Nombre,
                c.Telefono,
                c.BalanceActual,
                c.DiasCredito,
                diasVencidos,
                montoVencido
            ));
        }

        return resultado.OrderByDescending(r => r.MontoVencido).ThenByDescending(r => r.DiasVencidos).ThenBy(r => r.Nombre).ToList();
    }
}
