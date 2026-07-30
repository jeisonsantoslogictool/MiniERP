using MiniERP.Domain.Compras;
using Xunit;

namespace MiniERP.Tests.Compras;

public class PagoTests
{
    [Fact]
    public void Pago_ConservaLaFechaIndicada()
    {
        var fecha = new DateTime(2026, 7, 28);
        var pago = new Pago { Fecha = fecha };

        Assert.Equal(fecha, pago.Fecha);
    }

    [Fact]
    public void AplicarPago_DeberiaReducirBalanceYAsignarRastro()
    {
        // Arrange
        var proveedor = new Proveedor
        {
            BalanceActual = 600
        };
        var pago = new Pago { Monto = 250 };

        // Act
        proveedor.AplicarPago(pago);

        // Assert
        Assert.Equal(350, proveedor.BalanceActual);
        Assert.Equal(600, pago.BalanceAnterior);
        Assert.Equal(350, pago.BalanceResultante);
        Assert.Equal(proveedor.Id, pago.ProveedorId);
    }

    [Fact]
    public void AplicarPago_ConMontoNegativoOCero_DeberiaLanzarExcepcion()
    {
        // Arrange
        var proveedor = new Proveedor { BalanceActual = 600 };
        var pagoNegativo = new Pago { Monto = -5 };
        var pagoCero = new Pago { Monto = 0 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => proveedor.AplicarPago(pagoNegativo));
        Assert.Throws<InvalidOperationException>(() => proveedor.AplicarPago(pagoCero));
    }

    [Fact]
    public void AplicarPago_ExcediendoDeuda_DeberiaLanzarExcepcion()
    {
        // Arrange
        var proveedor = new Proveedor { BalanceActual = 600 };
        var pagoExcesivo = new Pago { Monto = 601 };

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => proveedor.AplicarPago(pagoExcesivo));
    }
}
