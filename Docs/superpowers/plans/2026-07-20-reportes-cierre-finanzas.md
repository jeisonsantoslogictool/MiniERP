# Reportes de cierre de Finanzas — Plan de implementación (Fase Dominio)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir el dominio del estado de resultados y del flujo de caja (dos records con sus fórmulas), con TDD, sin tocar la base de datos.

**Architecture:** Dos objetos de valor (`EstadoResultados`, `FlujoCaja`) con propiedades calculadas que encierran las fórmulas del anteproyecto. C# puro (dominio), probable sin base de datos. El panel (#6) no lleva dominio nuevo: ensambla en la capa de aplicación.

**Tech Stack:** .NET 10 · C# · xUnit · capa `MiniERP.Domain` (sin dependencias).

## Global Constraints

- **El dominio no conoce EF Core.** Los records y sus pruebas no tocan la base.
- **El estado de resultados NO acepta pagos ni cobros:** `Utilidad = Ingresos − CostoVendido − Egresos`.
- **El flujo de caja:** `EfectivoNeto = (VentasContado + Cobros) − (Egresos + Pagos)`.
- **Pruebas automatizadas solo del dominio** (convención del repo).
- **Sin migración**: los reportes solo leen datos que ya existen.

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `src/MiniERP.Domain/Finanzas/EstadoResultados.cs` (crear) | El record del estado de resultados y sus fórmulas. |
| `src/MiniERP.Domain/Finanzas/FlujoCaja.cs` (crear) | El record del flujo de caja y sus fórmulas. |
| `tests/MiniERP.Tests/Finanzas/EstadoResultadosTests.cs` (crear) | 3 pruebas. |
| `tests/MiniERP.Tests/Finanzas/FlujoCajaTests.cs` (crear) | 3 pruebas. |

---

## Task 1: `EstadoResultados` — la utilidad

**Files:**
- Create: `src/MiniERP.Domain/Finanzas/EstadoResultados.cs`
- Test: `tests/MiniERP.Tests/Finanzas/EstadoResultadosTests.cs`

**Interfaces:**
- Produces: `EstadoResultados(decimal Ingresos, decimal CostoVendido, decimal Egresos)` con `MargenBruto` (Ingresos − CostoVendido) y `Utilidad` (MargenBruto − Egresos).

- [ ] **Step 1: Write the failing tests**

Crear `tests/MiniERP.Tests/Finanzas/EstadoResultadosTests.cs`:

```csharp
using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

public class EstadoResultadosTests
{
    [Fact]
    public void El_margen_bruto_es_ingresos_menos_costo()
    {
        var er = new EstadoResultados(Ingresos: 1000, CostoVendido: 600, Egresos: 0);

        Assert.Equal(400m, er.MargenBruto);
    }

    [Fact]
    public void La_utilidad_resta_los_egresos()
    {
        var er = new EstadoResultados(Ingresos: 1000, CostoVendido: 600, Egresos: 250);

        Assert.Equal(150m, er.Utilidad); // 1000 - 600 - 250
    }

    [Fact]
    public void La_utilidad_puede_ser_negativa()
    {
        var er = new EstadoResultados(Ingresos: 1000, CostoVendido: 600, Egresos: 500);

        Assert.Equal(-100m, er.Utilidad);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter FullyQualifiedName~EstadoResultadosTests`
Expected: FALLA de compilación — `EstadoResultados` no existe.

- [ ] **Step 3: Write minimal implementation**

`src/MiniERP.Domain/Finanzas/EstadoResultados.cs`:

```csharp
namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Estado de resultados de un periodo: cuanto gana el negocio.
/// No entran cobros ni pagos: el costo de la mercancia ya se conto al vender.
/// </summary>
public record EstadoResultados(decimal Ingresos, decimal CostoVendido, decimal Egresos)
{
    public decimal MargenBruto => Ingresos - CostoVendido;

    public decimal Utilidad => MargenBruto - Egresos;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~EstadoResultadosTests`
Expected: PASAN (3 pruebas).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Finanzas/EstadoResultados.cs tests/MiniERP.Tests/Finanzas/EstadoResultadosTests.cs
git commit -m "Estado de resultados: record de dominio (utilidad = ingresos - costo - egresos)"
```

---

## Task 2: `FlujoCaja` — el efectivo neto

**Files:**
- Create: `src/MiniERP.Domain/Finanzas/FlujoCaja.cs`
- Test: `tests/MiniERP.Tests/Finanzas/FlujoCajaTests.cs`

**Interfaces:**
- Produces: `FlujoCaja(decimal VentasContado, decimal Cobros, decimal Egresos, decimal Pagos)` con `Entradas` (VentasContado + Cobros), `Salidas` (Egresos + Pagos) y `EfectivoNeto` (Entradas − Salidas).

- [ ] **Step 1: Write the failing tests**

Crear `tests/MiniERP.Tests/Finanzas/FlujoCajaTests.cs`:

```csharp
using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

public class FlujoCajaTests
{
    [Fact]
    public void Las_entradas_son_ventas_de_contado_mas_cobros()
    {
        var f = new FlujoCaja(VentasContado: 1000, Cobros: 300, Egresos: 0, Pagos: 0);

        Assert.Equal(1300m, f.Entradas);
    }

    [Fact]
    public void Las_salidas_son_egresos_mas_pagos()
    {
        var f = new FlujoCaja(VentasContado: 0, Cobros: 0, Egresos: 200, Pagos: 500);

        Assert.Equal(700m, f.Salidas);
    }

    [Fact]
    public void El_efectivo_neto_es_entradas_menos_salidas()
    {
        var f = new FlujoCaja(VentasContado: 1000, Cobros: 300, Egresos: 200, Pagos: 500);

        Assert.Equal(600m, f.EfectivoNeto); // (1000+300) - (200+500)
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter FullyQualifiedName~FlujoCajaTests`
Expected: FALLA de compilación — `FlujoCaja` no existe.

- [ ] **Step 3: Write minimal implementation**

`src/MiniERP.Domain/Finanzas/FlujoCaja.cs`:

```csharp
namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Flujo de caja de un periodo: cuanto efectivo entra y sale de la gaveta.
/// Distinto del estado de resultados: mide efectivo, no ganancia.
/// </summary>
public record FlujoCaja(decimal VentasContado, decimal Cobros, decimal Egresos, decimal Pagos)
{
    public decimal Entradas => VentasContado + Cobros;

    public decimal Salidas => Egresos + Pagos;

    public decimal EfectivoNeto => Entradas - Salidas;
}
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~FlujoCajaTests`
Expected: PASAN (3 pruebas).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Finanzas/FlujoCaja.cs tests/MiniERP.Tests/Finanzas/FlujoCajaTests.cs
git commit -m "Flujo de caja: record de dominio (efectivo neto = entradas - salidas)"
```

---

## Cierre de la fase de dominio

- [ ] **Correr toda la suite:**

Run: `dotnet test`
Expected: 144 pruebas verdes (138 previas + 6 nuevas).

---

## Fases siguientes (se ejecutan a continuación, sin migración)

### Aplicación (`MiniERP.Application.Finanzas`)
- **`IEstadoResultadosService.ObtenerAsync(FiltroPeriodo)`** → `EstadoResultados`. Compone:
  `ingresos = (await ingresosService.ObtenerAsync(new FiltroReporteIngresos(desde, hasta))).Subtotal`;
  `costo = (await rentabilidadService.ObtenerAsync(new FiltroPeriodo(desde, hasta))).Total.Costo`;
  `egresos = await egresoService.ObtenerTotalAsync(new FiltroEgresos(Desde: desde, Hasta: hasta))`;
  `return new EstadoResultados(ingresos, costo, egresos)`.
- **`IFlujoCajaService.ObtenerAsync(FiltroPeriodo)`** → `FlujoCaja`. Compone:
  `contado = (await ingresosService.ObtenerAsync(...)).Contado.Total`;
  `cobros = await cajaRepo.SumarCobrosAsync(desde, hasta)`; `egresos = await egresoService.ObtenerTotalAsync(...)`;
  `pagos = await cajaRepo.SumarPagosAsync(desde, hasta)`; `return new FlujoCaja(contado, cobros, egresos, pagos)`.
- **`IFlujoCajaRepositorio`**: `SumarCobrosAsync`, `SumarPagosAsync` (en `IFinanzasRepositorios.cs`).
- **`IPanelFinanzasService.ObtenerAsync()`** → `PanelFinanzasDto(VentasDia, MargenDia, EgresosMes, UtilidadMes, EfectivoMes)`, calculando "hoy" y "mes en curso" con los servicios de arriba.
- DTOs (`PanelFinanzasDto`) en `ReportesDtos.cs`.

### Infraestructura
- **`FlujoCajaRepositorio`** (en `FinanzasRepositorios.cs`): `SumarCobrosAsync = contexto.Cobros.Where(c => c.Fecha >= desde && c.Fecha < hasta.Date.AddDays(1)).Sum(c => (decimal?)c.Monto) ?? 0`; igual para `Pagos`. Registro en `DependencyInjection.Finanzas.cs`. **Sin migración.**

### Web (`Components/Pages/Finanzas`)
- **`EstadoResultados.razor`** (`/finanzas/estado-resultados`): fechas + cascada (Ingresos, − Costo, = Margen bruto, − Egresos, = Utilidad).
- **`FlujoCaja.razor`** (`/finanzas/flujo-caja`): fechas + (+ contado, + cobros, − egresos, − pagos, = Efectivo neto).
- **`Finanzas.razor`** (portada): KPIs del panel (día/mes) arriba, enlaces a los reportes debajo, con el enlace a los dos nuevos.

### Verificación
- Levantar la app y ver los tres con la venta real: estado de resultados con utilidad = ingresos − costo − egresos; el panel con ventas/margen del día.
- **Gate #4:** registrar un pago a proveedor y comprobar que la utilidad no cambia.

### Capítulo IV
- La sección del módulo de Finanzas completo, con capturas.
