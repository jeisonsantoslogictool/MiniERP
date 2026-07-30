using MiniERP.Domain.Clientes;
using Xunit;

namespace MiniERP.Tests.Clientes;

public class CobroTests
{
    [Fact]
    public void Cobro_ConservaLaFechaIndicada()
    {
        var fecha = new DateTime(2026, 7, 28);
        var cobro = new Cobro { Fecha = fecha };

        Assert.Equal(fecha, cobro.Fecha);
    }

    [Fact]
    public void AplicarCobro_DeberiaReducirBalanceYAsignarRastro()
    {
        // Arrange
        var cliente = new Cliente
        {
            LimiteCredito = 1000,
            BalanceActual = 500
        };
        var cobro = new Cobro { Monto = 200 };

        // Act
        cliente.AplicarCobro(cobro);

        // Assert
        Assert.Equal(300, cliente.BalanceActual);
        Assert.Equal(500, cobro.BalanceAnterior);
        Assert.Equal(300, cobro.BalanceResultante);
        Assert.Equal(cliente.Id, cobro.ClienteId);
    }

    [Fact]
    public void AplicarCobro_ConMontoNegativoOCero_DeberiaLanzarExcepcion()
    {
        // Arrange
        var cliente = new Cliente { BalanceActual = 500 };
        var cobroNegativo = new Cobro { Monto = -10 };
        var cobroCero = new Cobro { Monto = 0 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => cliente.AplicarCobro(cobroNegativo));
        Assert.Throws<InvalidOperationException>(() => cliente.AplicarCobro(cobroCero));
    }

    [Fact]
    public void AplicarCobro_ExcediendoDeuda_DeberiaLanzarExcepcion()
    {
        // Arrange
        var cliente = new Cliente { BalanceActual = 500 };
        var cobroExcesivo = new Cobro { Monto = 501 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => cliente.AplicarCobro(cobroExcesivo));
    }
}
