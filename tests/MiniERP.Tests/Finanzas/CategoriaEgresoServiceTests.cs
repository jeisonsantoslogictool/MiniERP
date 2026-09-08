using MiniERP.Application.Common;
using MiniERP.Application.Finanzas.Contracts;
using MiniERP.Application.Finanzas.Dtos;
using MiniERP.Application.Finanzas.Services;
using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

/// <summary>
/// Que guarda y que rechaza el catalogo de categorias de gasto.
/// </summary>
/// <remarks>
/// La categoria es lo que hace legible el reporte de gastos: sin ella, el estado de
/// resultados dice cuanto se gasto pero no en que. El catalogo lo edita el comerciante,
/// asi que el servicio cuida que no le entren nombres vacios ni repetidos, y que
/// desactivar una categoria no borre los gastos que ya cuelgan de ella.
/// </remarks>
public class CategoriaEgresoServiceTests
{
    private static CategoriaEgreso Existente() => new()
    {
        Id = 4,
        Nombre = "Transporte",
        Descripcion = "Combustible y fletes",
        Activo = true,
        CreadoPor = "jeison"
    };

    private static CategoriaEgresoFormDto Formulario(int id = 0, string nombre = "Transporte") => new()
    {
        Id = id,
        Nombre = nombre,
        Descripcion = "Combustible y fletes",
        Activo = true
    };

    private static CategoriaEgresoService Servicio(
        CategoriaEgreso? existente, out CategoriaEgresoRepositorioFalso repo, bool nombreRepetido = false)
    {
        repo = new CategoriaEgresoRepositorioFalso(existente, nombreRepetido);
        return new CategoriaEgresoService(repo);
    }

    // ---------- Crear ----------

    [Fact]
    public async Task Crear_una_categoria_valida_la_agrega_y_la_guarda()
    {
        var servicio = Servicio(existente: null, out var repo);

        var resultado = await servicio.GuardarAsync(Formulario(), "samuel");

        Assert.True(resultado.Exito);
        Assert.Equal("Transporte", repo.Agregada!.Nombre);
        Assert.Equal("samuel", repo.Agregada.CreadoPor);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task Crear_sin_nombre_falla()
    {
        var servicio = Servicio(existente: null, out var repo);

        var resultado = await servicio.GuardarAsync(Formulario(nombre: "   "), "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("El nombre de la categoría es obligatorio.", resultado.Error);
        Assert.Null(repo.Agregada);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact]
    public async Task Crear_con_un_nombre_que_ya_existe_falla()
    {
        var servicio = Servicio(existente: null, out var repo, nombreRepetido: true);

        var resultado = await servicio.GuardarAsync(Formulario(), "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("Ya existe una categoría llamada 'Transporte'.", resultado.Error);
        Assert.Null(repo.Agregada);
    }

    /// <summary>
    /// El nombre se recorta antes de compararlo y antes de guardarlo: si no, " Luz" y "Luz"
    /// pasarian la comprobacion de duplicado y el reporte quedaria con dos renglones iguales.
    /// </summary>
    [Fact]
    public async Task El_nombre_se_guarda_recortado()
    {
        var servicio = Servicio(existente: null, out var repo);

        await servicio.GuardarAsync(Formulario(nombre: "  Transporte  "), "samuel");

        Assert.Equal("Transporte", repo.Agregada!.Nombre);
        Assert.Equal("Transporte", repo.NombreConsultado);
    }

    [Fact]
    public async Task Crear_sin_descripcion_la_deja_nula()
    {
        var servicio = Servicio(existente: null, out var repo);
        var form = Formulario();
        form.Descripcion = "   ";

        await servicio.GuardarAsync(form, "samuel");

        Assert.Null(repo.Agregada!.Descripcion);
    }

    // ---------- Editar ----------

    [Fact]
    public async Task Editar_una_categoria_guarda_los_cambios_y_sella_quien_edito()
    {
        var categoria = Existente();
        var servicio = Servicio(categoria, out var repo);
        var form = Formulario(id: categoria.Id, nombre: "Transporte y fletes");

        var resultado = await servicio.GuardarAsync(form, "samuel");

        Assert.True(resultado.Exito);
        Assert.Equal("Transporte y fletes", categoria.Nombre);
        Assert.Equal("jeison", categoria.CreadoPor);
        Assert.Equal("samuel", categoria.ModificadoPor);
        Assert.NotNull(categoria.FechaModificacion);
        Assert.Null(repo.Agregada);
        Assert.Equal(1, repo.Guardados);
    }

    /// <summary>
    /// Al editar, la comprobacion de duplicado tiene que excluir a la propia categoria.
    /// Sin eso, guardar sin cambiarle el nombre chocaria consigo misma.
    /// </summary>
    [Fact]
    public async Task Editar_excluye_la_propia_categoria_al_buscar_duplicados()
    {
        var categoria = Existente();
        var servicio = Servicio(categoria, out var repo);

        await servicio.GuardarAsync(Formulario(id: categoria.Id), "samuel");

        Assert.Equal(categoria.Id, repo.ExcluirIdConsultado);
    }

    [Fact]
    public async Task Al_crear_no_se_excluye_ningun_id()
    {
        var servicio = Servicio(existente: null, out var repo);

        await servicio.GuardarAsync(Formulario(), "samuel");

        Assert.Null(repo.ExcluirIdConsultado);
    }

    [Fact]
    public async Task Editar_una_categoria_que_no_existe_falla()
    {
        var servicio = Servicio(existente: null, out var repo);

        var resultado = await servicio.GuardarAsync(Formulario(id: 999), "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("La categoría no existe.", resultado.Error);
        Assert.Equal(0, repo.Guardados);
    }

    // ---------- Estado ----------

    [Fact]
    public async Task Desactivar_una_categoria_la_deja_inactiva_sin_borrarla()
    {
        var categoria = Existente();
        var servicio = Servicio(categoria, out var repo);

        var resultado = await servicio.CambiarEstadoAsync(categoria.Id, activo: false, "samuel");

        Assert.True(resultado.Exito);
        Assert.False(categoria.Activo);
        Assert.Equal("samuel", categoria.ModificadoPor);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact]
    public async Task Cambiar_el_estado_de_una_categoria_que_no_existe_falla()
    {
        var servicio = Servicio(existente: null, out var repo);

        var resultado = await servicio.CambiarEstadoAsync(999, activo: false, "samuel");

        Assert.True(resultado.Fallo);
        Assert.Equal("La categoría no existe.", resultado.Error);
        Assert.Equal(0, repo.Guardados);
    }

    // ---------- Consulta ----------

    [Fact]
    public async Task Obtener_para_editar_mapea_la_categoria()
    {
        var categoria = Existente();
        var servicio = Servicio(categoria, out _);

        var form = await servicio.ObtenerParaEditarAsync(categoria.Id);

        Assert.NotNull(form);
        Assert.Equal(categoria.Id, form.Id);
        Assert.Equal("Transporte", form.Nombre);
        Assert.Equal("Combustible y fletes", form.Descripcion);
        Assert.True(form.Activo);
    }

    [Fact]
    public async Task Obtener_para_editar_una_categoria_que_no_existe_da_nulo()
    {
        var servicio = Servicio(existente: null, out _);

        Assert.Null(await servicio.ObtenerParaEditarAsync(999));
    }

    /// <summary>
    /// Anota el nombre y el id que recibio la comprobacion de duplicados, que es la parte
    /// del contrato que las pruebas necesitan ver. Lo demas lanza a proposito.
    /// </summary>
    private sealed class CategoriaEgresoRepositorioFalso(CategoriaEgreso? existente, bool nombreRepetido)
        : ICategoriaEgresoRepositorio
    {
        public CategoriaEgreso? Agregada { get; private set; }

        public string? NombreConsultado { get; private set; }

        public int? ExcluirIdConsultado { get; private set; }

        public int Guardados { get; private set; }

        public Task<CategoriaEgreso?> ObtenerAsync(int id, CancellationToken ct = default) =>
            Task.FromResult(existente?.Id == id ? existente : null);

        public Task<bool> ExisteNombreAsync(string nombre, int? excluirId = null, CancellationToken ct = default)
        {
            NombreConsultado = nombre;
            ExcluirIdConsultado = excluirId;
            return Task.FromResult(nombreRepetido);
        }

        public void Agregar(CategoriaEgreso categoria)
        {
            categoria.Id = 88;
            Agregada = categoria;
        }

        public Task<int> GuardarAsync(CancellationToken ct = default)
        {
            Guardados++;
            return Task.FromResult(1);
        }

        public Task<PaginaDe<CategoriaEgresoListaDto>> BuscarAsync(
            FiltroCategoriasEgreso filtro, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<OpcionDto>> ObtenerOpcionesAsync(CancellationToken ct = default) =>
            throw new NotSupportedException();
    }
}
