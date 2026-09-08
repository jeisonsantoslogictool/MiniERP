using MiniERP.Application.Common;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Finanzas.Services;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

/// <summary>
/// Que guarda y que rechaza el caso de uso de gastos operativos.
/// </summary>
/// <remarks>
/// El egreso es la unica cifra de finanzas que se teclea: las ventas y el costo vienen de
/// las facturas, pero el alquiler y la luz los escribe una persona. Por eso el servicio
/// valida antes de guardar, y por eso importa que el gasto conserve quien lo registro
/// aunque despues alguien le corrija la nota.
/// </remarks>
public class EgresoServiceTests
{
    private const int CategoriaLuz = 3;

    private static Egreso Existente() => new()
    {
        Id = 12,
        Fecha = new DateTime(2026, 3, 5),
        CategoriaEgresoId = CategoriaLuz,
        Monto = 2500m,
        Descripcion = "Factura de luz de febrero",
        UsuarioId = "samuel",
        CreadoPor = "samuel"
    };

    private static EgresoFormDto Formulario(int id = 0, decimal monto = 2500m) => new()
    {
        Id = id,
        Fecha = new DateTime(2026, 3, 5),
        CategoriaEgresoId = CategoriaLuz,
        Monto = monto,
        Descripcion = "Factura de luz de febrero"
    };

    private static EgresoService Servicio(
        Egreso? existente, out EgresoRepositorioFalso repo, bool categoriaExiste = true)
    {
        repo = new EgresoRepositorioFalso(existente);
        return new EgresoService(repo, new CategoriaEgresoRepositorioFalso(categoriaExiste));
    }

    // ---------- Crear ----------

    [Fact]
    public async Task Crear_un_egreso_valido_lo_agrega_y_lo_guarda()
    {
        var servicio = Servicio(existente: null, out var repo);

        var resultado = await servicio.GuardarAsync(Formulario(), "samuel");

        Assert.True(resultado.Exito);
        Assert.Equal(2500m, repo.Agregado!.Monto);
        Assert.Equal(CategoriaLuz, repo.Agregado.CategoriaEgresoId);
        Assert.Equal(1, repo.Guardados);
    }

    /// <summary>
    /// El responsable del gasto queda en <c>UsuarioId</c> y en <c>CreadoPor</c>: sin eso,
    /// un gasto en efectivo no tiene a quien preguntarle.
    /// </summary>
    [Fact]
    public async Task Crear_un_egreso_deja_escrito_quien_lo_registro()
    {
        var servicio = Servicio(existente: null, out var repo);

        await servicio.GuardarAsync(Formulario(), "samuel");

        Assert.Equal("samuel", repo.Agregado!.UsuarioId);
        Assert.Equal("samuel", repo.Agregado.CreadoPor);
    }

    [Fact]
    public async Task Crear_sin_categoria_falla_y_no_guarda_nada()
    {
        var servicio = Servicio(existente: null, out var repo);
        var form = Formulario();
        form.CategoriaEgresoId = 0;

        var resultado = await servicio.GuardarAsync(form, "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("Selecciona una categoría.", resultado.Error);
        Assert.Null(repo.Agregado);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task Crear_con_una_categoria_que_no_existe_falla()
    {
        var servicio = Servicio(existente: null, out var repo, categoriaExiste: false);

        var resultado = await servicio.GuardarAsync(Formulario(), "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("La categoría no existe.", resultado.Error);
        Assert.Null(repo.Agregado);
    }

    /// <summary>
    /// El monto lo valida la fabrica del dominio. El servicio traduce esa excepcion a un
    /// resultado fallido para que la pantalla lo muestre en vez de reventar.
    /// </summary>
    [Fact]
    public async Task Crear_con_monto_cero_devuelve_el_mensaje_del_dominio_sin_explotar()
    {
        var servicio = Servicio(existente: null, out var repo);

        var resultado = await servicio.GuardarAsync(Formulario(monto: 0m), "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("El monto del egreso debe ser mayor que cero.", resultado.Error);
        Assert.Null(repo.Agregado);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task Crear_con_monto_negativo_falla()
    {
        var servicio = Servicio(existente: null, out _);

        var resultado = await servicio.GuardarAsync(Formulario(monto: -100m), "samuel");

        Assert.True(resultado.Fallo);
    }

    [Fact]
    public async Task Crear_sin_descripcion_deja_la_nota_nula()
    {
        var servicio = Servicio(existente: null, out var repo);
        var form = Formulario();
        form.Descripcion = "   ";

        await servicio.GuardarAsync(form, "samuel");

        Assert.Null(repo.Agregado!.Descripcion);
    }

    // ---------- Actualizar ----------

    [Fact]
    public async Task Editar_un_egreso_guarda_los_cambios()
    {
        var egreso = Existente();
        var servicio = Servicio(egreso, out var repo);
        var form = Formulario(id: egreso.Id, monto: 2750m);
        form.Descripcion = "Factura de luz de febrero (recargo)";

        var resultado = await servicio.GuardarAsync(form, "jeison");

        Assert.True(resultado.Exito);
        Assert.Equal(2750m, egreso.Monto);
        Assert.Equal("Factura de luz de febrero (recargo)", egreso.Descripcion);
        Assert.Equal(1, repo.Guardados);
        Assert.Null(repo.Agregado);
    }

    /// <summary>
    /// Corregir la nota de un gasto no cambia de quien fue el gasto. Quien edita queda en
    /// <c>ModificadoPor</c>, y el que lo registro sigue respondiendo por el.
    /// </summary>
    [Fact]
    public async Task Editar_un_egreso_no_cambia_quien_lo_registro()
    {
        var egreso = Existente();
        var servicio = Servicio(egreso, out _);

        await servicio.GuardarAsync(Formulario(id: egreso.Id), "jeison");

        Assert.Equal("samuel", egreso.UsuarioId);
        Assert.Equal("samuel", egreso.CreadoPor);
        Assert.Equal("jeison", egreso.ModificadoPor);
        Assert.NotNull(egreso.FechaModificacion);
    }

    [Fact]
    public async Task Editar_con_monto_cero_falla_y_no_toca_el_egreso()
    {
        var egreso = Existente();
        var servicio = Servicio(egreso, out var repo);

        var resultado = await servicio.GuardarAsync(Formulario(id: egreso.Id, monto: 0m), "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("El monto debe ser mayor que cero.", resultado.Error);
        Assert.Equal(2500m, egreso.Monto);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task Editar_un_egreso_que_no_existe_falla()
    {
        var servicio = Servicio(existente: null, out var repo);

        var resultado = await servicio.GuardarAsync(Formulario(id: 999), "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("El egreso no existe.", resultado.Error);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task Editar_dejando_la_nota_en_blanco_la_vuelve_nula()
    {
        var egreso = Existente();
        var servicio = Servicio(egreso, out _);
        var form = Formulario(id: egreso.Id);
        form.Descripcion = "   ";

        await servicio.GuardarAsync(form, "samuel");

        Assert.Null(egreso.Descripcion);
    }

    // ---------- Eliminar ----------

    [Fact]
    public async Task Eliminar_un_egreso_existente_lo_borra_y_guarda()
    {
        var egreso = Existente();
        var servicio = Servicio(egreso, out var repo);

        var resultado = await servicio.EliminarAsync(egreso.Id);

        Assert.True(resultado.Exito);
        Assert.Same(egreso, repo.Eliminado);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task Eliminar_un_egreso_que_no_existe_falla()
    {
        var servicio = Servicio(existente: null, out var repo);

        var resultado = await servicio.EliminarAsync(999);

        Assert.True(resultado.Fallo);
        Assert.Equal("El egreso no existe.", resultado.Error);
        Assert.Null(repo.Eliminado);
        Assert.Equal(0, repo.Guardados);
    }

    /// <summary>
    /// Solo lo que el caso de uso consulta de verdad. Lo demas lanza a proposito: si una
    /// prueba futura lo necesita, es que el servicio cambio y hay que mirarlo.
    /// </summary>
    private sealed class EgresoRepositorioFalso(Egreso? existente) : IEgresoRepositorio
    {
        public Egreso? Agregado { get; private set; }

        public Egreso? Eliminado { get; private set; }

        public int Guardados { get; private set; }

        public Task<Egreso?> ObtenerAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(existente?.Id == id ? existente : null);

        public void Agregar(Egreso egreso)
        {
            egreso.Id = 55;
            Agregado = egreso;
        }

        public void Eliminar(Egreso egreso) => Eliminado = egreso;

        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }

        public Task<PaginaDe<EgresoListaDto>> BuscarAsync(FiltroEgresos filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<EgresoFormDto?> ObtenerParaEditarAsync(int id, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<decimal> ObtenerTotalAsync(FiltroEgresos filtro, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class CategoriaEgresoRepositorioFalso(bool existe) : ICategoriaEgresoRepositorio
    {
        public Task<CategoriaEgreso?> ObtenerAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(existe ? new CategoriaEgreso { Id = id, Nombre = "Luz" } : null);

        public Task<PaginaDe<CategoriaEgresoListaDto>> BuscarAsync(
            FiltroCategoriasEgreso filtro, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public void Agregar(CategoriaEgreso categoria) => throw new NotSupportedException();

        public Task<int> GuardarAsync(CancellationToken ct = default) => throw new NotSupportedException();
    }
}
