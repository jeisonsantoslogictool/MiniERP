using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

public class EgresoTests
{
    [Fact]
    public void Registrar_valido_crea_el_egreso()
    {
        var fecha = new DateTime(2026, 7, 17);

        var egreso = Egreso.Registrar(
            monto: 5000m, fecha: fecha, categoriaEgresoId: 3,
            descripcion: "Luz de enero", usuarioId: "tester");

        Assert.Equal(5000m, egreso.Monto);
        Assert.Equal(fecha, egreso.Fecha);
        Assert.Equal(3, egreso.CategoriaEgresoId);
        Assert.Equal("Luz de enero", egreso.Descripcion);
        Assert.Equal("tester", egreso.UsuarioId);
    }

    [Fact]
    public void Registrar_sin_descripcion_deja_la_nota_nula()
    {
        var egreso = Egreso.Registrar(5000m, new DateTime(2026, 7, 17), 3, "   ", "tester");

        Assert.Null(egreso.Descripcion);
    }
}
