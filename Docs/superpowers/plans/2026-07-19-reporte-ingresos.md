# Reporte de ingresos — Plan de implementación (Fase Dominio)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir la calculadora de dominio del reporte de ingresos (`ResumenIngresos.Calcular`), con TDD, sin tocar la base de datos.

**Architecture:** Un objeto de valor `ResumenIngresos` con una fábrica estática que recibe las facturas de un período, descarta las anuladas, separa contado de crédito y suma subtotal/ITBIS/total. Es C# puro (dominio), probable sin base de datos. No hay entidad persistida: es un resultado calculado.

**Tech Stack:** .NET 10 · C# · xUnit · capa `MiniERP.Domain` (sin dependencias).

## Global Constraints

- **El dominio no conoce EF Core.** La calculadora y sus pruebas no tocan la base.
- **Se excluyen las facturas anuladas** (`Factura.EstaAnulada`): una venta anulada no es ingreso.
- **Se separan contado y crédito** por `Factura.Condicion` (`CondicionPago`).
- **El ITBIS va aparte** del subtotal: no se mezcla en el ingreso.
- **Redondeo al totalizar** con `RetailConstants.RedondearImporte`.
- **Pruebas automatizadas solo del dominio** (convención del repo).
- **Sin migración**: el reporte solo lee `Factura`, que ya existe.

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `src/MiniERP.Domain/Finanzas/ResumenIngresos.cs` (crear) | El resumen (`ResumenIngresos` + `ResumenIngresosPorCondicion`) y la fábrica `Calcular`. |
| `tests/MiniERP.Tests/Finanzas/ResumenIngresosTests.cs` (crear) | Las 6 pruebas de dominio. |

---

## Task 1: `ResumenIngresos.Calcular` — separar y sumar

**Files:**
- Create: `src/MiniERP.Domain/Finanzas/ResumenIngresos.cs`
- Test: `tests/MiniERP.Tests/Finanzas/ResumenIngresosTests.cs`

**Interfaces:**
- Consumes: `MiniERP.Domain.Ventas.Factura` (con `Condicion`, `Subtotal`, `Itbis`, `Total`, `EstaAnulada`, `Estado`); `MiniERP.Domain.Shared.CondicionPago`, `RetailConstants`; `MiniERP.Domain.Ventas.EstadoFactura`.
- Produces: `ResumenIngresos.Calcular(IEnumerable<Factura>)` → `ResumenIngresos` con `Contado`/`Credito` (cada uno `ResumenIngresosPorCondicion` con `Cantidad`, `Subtotal`, `Itbis`, `Total`) y los totales generales `Cantidad`/`Subtotal`/`Itbis`/`Total`.

- [ ] **Step 1: Write the failing tests**

Crear `tests/MiniERP.Tests/Finanzas/ResumenIngresosTests.cs`:

```csharp
using MiniERP.Domain.Finanzas;
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Tests.Finanzas;

public class ResumenIngresosTests
{
    private static Factura Factura(
        CondicionPago condicion, decimal subtotal, decimal itbis,
        EstadoFactura estado = EstadoFactura.Emitida) => new()
    {
        Condicion = condicion,
        Subtotal = subtotal,
        Itbis = itbis,
        Total = subtotal + itbis,
        Estado = estado
    };

    [Fact]
    public void Un_periodo_vacio_da_ceros()
    {
        var resumen = ResumenIngresos.Calcular([]);

        Assert.Equal(0, resumen.Cantidad);
        Assert.Equal(0m, resumen.Subtotal);
        Assert.Equal(0m, resumen.Itbis);
        Assert.Equal(0m, resumen.Total);
    }

    [Fact]
    public void Separa_contado_de_credito()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, subtotal: 1000, itbis: 180),
            Factura(CondicionPago.Credito, subtotal: 500, itbis: 90),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(1000m, resumen.Contado.Subtotal);
        Assert.Equal(500m, resumen.Credito.Subtotal);
    }

    [Fact]
    public void Suma_subtotal_itbis_y_total()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, 1000, 180),
            Factura(CondicionPago.Contado, 500, 90),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(1500m, resumen.Contado.Subtotal);
        Assert.Equal(270m, resumen.Contado.Itbis);
        Assert.Equal(1770m, resumen.Contado.Total);
    }

    [Fact]
    public void Cuenta_las_facturas_por_condicion()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, 100, 18),
            Factura(CondicionPago.Contado, 200, 36),
            Factura(CondicionPago.Credito, 300, 54),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(2, resumen.Contado.Cantidad);
        Assert.Equal(1, resumen.Credito.Cantidad);
    }

    [Fact]
    public void Los_totales_generales_suman_contado_y_credito()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, 1000, 180),
            Factura(CondicionPago.Credito, 500, 90),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(1500m, resumen.Subtotal);
        Assert.Equal(270m, resumen.Itbis);
        Assert.Equal(1770m, resumen.Total);
        Assert.Equal(2, resumen.Cantidad);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter FullyQualifiedName~ResumenIngresosTests`
Expected: FALLA de compilación — `ResumenIngresos` no existe.

- [ ] **Step 3: Write minimal implementation**

`src/MiniERP.Domain/Finanzas/ResumenIngresos.cs`:

```csharp
using MiniERP.Domain.Shared;
using MiniERP.Domain.Ventas;

namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Resumen de ventas de un periodo, separando contado de credito. Es un valor calculado,
/// no un registro: se arma leyendo las facturas y no se guarda.
/// </summary>
public record ResumenIngresos(
    ResumenIngresosPorCondicion Contado,
    ResumenIngresosPorCondicion Credito)
{
    public int Cantidad => Contado.Cantidad + Credito.Cantidad;
    public decimal Subtotal => Contado.Subtotal + Credito.Subtotal;
    public decimal Itbis => Contado.Itbis + Credito.Itbis;
    public decimal Total => Contado.Total + Credito.Total;

    /// <summary>
    /// Arma el resumen desde las facturas del periodo, separando por condicion de pago.
    /// </summary>
    public static ResumenIngresos Calcular(IEnumerable<Factura> facturas)
    {
        ArgumentNullException.ThrowIfNull(facturas);

        var lista = facturas.ToList();

        return new ResumenIngresos(
            Resumir(lista.Where(f => f.Condicion == CondicionPago.Contado)),
            Resumir(lista.Where(f => f.Condicion == CondicionPago.Credito)));
    }

    private static ResumenIngresosPorCondicion Resumir(IEnumerable<Factura> facturas)
    {
        var lista = facturas.ToList();

        return new ResumenIngresosPorCondicion(
            lista.Count,
            RetailConstants.RedondearImporte(lista.Sum(f => f.Subtotal)),
            RetailConstants.RedondearImporte(lista.Sum(f => f.Itbis)),
            RetailConstants.RedondearImporte(lista.Sum(f => f.Total)));
    }
}

public record ResumenIngresosPorCondicion(int Cantidad, decimal Subtotal, decimal Itbis, decimal Total);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~ResumenIngresosTests`
Expected: PASAN (5 pruebas).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Finanzas/ResumenIngresos.cs tests/MiniERP.Tests/Finanzas/ResumenIngresosTests.cs
git commit -m "Ingresos: calculadora ResumenIngresos (separa contado/credito y suma)"
```

---

## Task 2: Excluir las facturas anuladas

**Files:**
- Modify: `src/MiniERP.Domain/Finanzas/ResumenIngresos.cs`
- Test: `tests/MiniERP.Tests/Finanzas/ResumenIngresosTests.cs`

**Interfaces:**
- Produces: `Calcular` descarta las facturas con `EstaAnulada == true` antes de sumar.

- [ ] **Step 1: Write the failing test**

Agregar a `ResumenIngresosTests.cs`:

```csharp
    [Fact]
    public void Excluye_las_facturas_anuladas()
    {
        var facturas = new[]
        {
            Factura(CondicionPago.Contado, 1000, 180),
            Factura(CondicionPago.Contado, 999, 179, estado: EstadoFactura.Anulada),
        };

        var resumen = ResumenIngresos.Calcular(facturas);

        Assert.Equal(1, resumen.Contado.Cantidad);
        Assert.Equal(1000m, resumen.Contado.Subtotal);
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~ResumenIngresosTests`
Expected: FALLA — hoy `Calcular` no excluye anuladas, así que cuenta 2 y suma 1999.

- [ ] **Step 3: Write minimal implementation**

En `Calcular`, filtrar las anuladas antes de separar. Reemplazar el cuerpo del método:

```csharp
    public static ResumenIngresos Calcular(IEnumerable<Factura> facturas)
    {
        ArgumentNullException.ThrowIfNull(facturas);

        // Una venta anulada no es un ingreso: se descarta antes de sumar.
        var emitidas = facturas.Where(f => !f.EstaAnulada).ToList();

        return new ResumenIngresos(
            Resumir(emitidas.Where(f => f.Condicion == CondicionPago.Contado)),
            Resumir(emitidas.Where(f => f.Condicion == CondicionPago.Credito)));
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~ResumenIngresosTests`
Expected: PASAN (6 pruebas).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Finanzas/ResumenIngresos.cs tests/MiniERP.Tests/Finanzas/ResumenIngresosTests.cs
git commit -m "Ingresos: la calculadora excluye las facturas anuladas"
```

---

## Cierre de la fase

- [ ] **Correr toda la suite:**

Run: `dotnet test`
Expected: 124 pruebas verdes (118 previas + 6 nuevas).

---

## Fases siguientes (planes aparte, sin bloqueo ni migración)

1. **Aplicación:** `IReporteIngresosService` (`ObtenerAsync(FiltroReporteIngresos)` → `ResumenIngresos`), `IReporteIngresosRepositorio` (`ObtenerFacturasDelPeriodoAsync(desde, hasta)` → `IReadOnlyList<Factura>`), y el `record FiltroReporteIngresos(DateTime Desde, DateTime Hasta)`.
2. **Infraestructura:** `ReporteIngresosRepositorio` (facturas del período con `f.Fecha >= desde && f.Fecha < hasta.Date.AddDays(1)`, sin `Include` de líneas) y su registro en `DependencyInjection`. **Sin migración.**
3. **Web:** pantalla `/finanzas/ingresos` con selector de fechas (por defecto el mes en curso) y la tabla-resumen (filas Contado/Crédito/Total; columnas Cantidad/Subtotal/ITBIS/Total), enlazada desde la portada de Finanzas.
4. **Capítulo IV:** la sección del reporte de ingresos, con captura.
