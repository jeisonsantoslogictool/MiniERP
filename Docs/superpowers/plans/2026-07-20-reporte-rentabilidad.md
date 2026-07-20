# Reporte de rentabilidad — Plan de implementación (Fase Dominio)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir la calculadora de dominio de la rentabilidad (`ResumenRentabilidad.Calcular`), con TDD, sin tocar la base de datos.

**Architecture:** Un objeto de valor `ResumenRentabilidad` con una fábrica estática que recibe los renglones vendidos de un período, descarta las anuladas, y calcula el margen (subtotal − costo) en tres cortes: total, por producto y por categoría. C# puro (dominio), probable sin base de datos.

**Tech Stack:** .NET 10 · C# · xUnit · capa `MiniERP.Domain` (sin dependencias).

## Global Constraints

- **El dominio no conoce EF Core.** La calculadora y sus pruebas no tocan la base.
- **El margen es `Subtotal − Costo`.** El costo llega **congelado** en el renglón (lo proyecta el repositorio desde `LineaFactura.CostoUnitario`); la calculadora nunca ve `Producto.Costo`.
- **Se excluyen las facturas anuladas** (`RenglonVendido.FacturaAnulada`).
- **El margen % es sobre el subtotal:** `Subtotal == 0 ? 0 : Margen / Subtotal`.
- **Redondeo al totalizar** con `RetailConstants.RedondearImporte`.
- **Los cortes van ordenados por margen descendente.**
- **Pruebas automatizadas solo del dominio** (convención del repo).
- **Sin migración**: el reporte solo lee líneas de factura que ya existen.

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `src/MiniERP.Domain/Finanzas/ResumenRentabilidad.cs` (crear) | Los records (`ResumenRentabilidad`, `RentabilidadTotal`, `RentabilidadPorItem`, `RenglonVendido`) y la fábrica `Calcular`. |
| `tests/MiniERP.Tests/Finanzas/ResumenRentabilidadTests.cs` (crear) | Las 8 pruebas de dominio. |

---

## Task 1: `ResumenRentabilidad.Calcular` — margen, cortes y orden

**Files:**
- Create: `src/MiniERP.Domain/Finanzas/ResumenRentabilidad.cs`
- Test: `tests/MiniERP.Tests/Finanzas/ResumenRentabilidadTests.cs`

**Interfaces:**
- Consumes: `MiniERP.Domain.Shared.RetailConstants`.
- Produces: `ResumenRentabilidad.Calcular(IEnumerable<RenglonVendido>)` → `ResumenRentabilidad` con `Total` (`RentabilidadTotal`), `PorProducto` y `PorCategoria` (`IReadOnlyList<RentabilidadPorItem>`). `RenglonVendido(bool FacturaAnulada, int ProductoId, string Producto, int CategoriaId, string Categoria, decimal Subtotal, decimal Costo)`. `RentabilidadTotal(decimal Subtotal, decimal Costo, decimal Margen, decimal MargenPorcentaje)`. `RentabilidadPorItem(int Id, string Nombre, decimal Subtotal, decimal Costo, decimal Margen, decimal MargenPorcentaje)`.

- [ ] **Step 1: Write the failing tests**

Crear `tests/MiniERP.Tests/Finanzas/ResumenRentabilidadTests.cs`:

```csharp
using MiniERP.Domain.Finanzas;

namespace MiniERP.Tests.Finanzas;

public class ResumenRentabilidadTests
{
    private static RenglonVendido Renglon(
        decimal subtotal, decimal costo,
        int productoId = 1, string producto = "Arroz",
        int categoriaId = 1, string categoria = "Viveres",
        bool anulada = false) =>
        new(anulada, productoId, producto, categoriaId, categoria, subtotal, costo);

    [Fact]
    public void Un_periodo_vacio_da_ceros()
    {
        var r = ResumenRentabilidad.Calcular([]);

        Assert.Equal(0m, r.Total.Subtotal);
        Assert.Equal(0m, r.Total.Margen);
        Assert.Empty(r.PorProducto);
        Assert.Empty(r.PorCategoria);
    }

    [Fact]
    public void El_margen_es_subtotal_menos_costo()
    {
        var r = ResumenRentabilidad.Calcular([Renglon(subtotal: 100, costo: 60)]);

        Assert.Equal(100m, r.Total.Subtotal);
        Assert.Equal(60m, r.Total.Costo);
        Assert.Equal(40m, r.Total.Margen);
    }

    [Fact]
    public void El_margen_porcentaje_es_sobre_el_subtotal()
    {
        var r = ResumenRentabilidad.Calcular([Renglon(100, 60)]);

        Assert.Equal(0.40m, r.Total.MargenPorcentaje);
    }

    [Fact]
    public void Agrupa_por_producto()
    {
        var renglones = new[]
        {
            Renglon(100, 60, productoId: 1, producto: "Arroz"),
            Renglon(50, 30, productoId: 1, producto: "Arroz"),
            Renglon(200, 100, productoId: 2, producto: "Aceite"),
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal(2, r.PorProducto.Count);
        var arroz = r.PorProducto.Single(p => p.Id == 1);
        Assert.Equal(150m, arroz.Subtotal);
        Assert.Equal(60m, arroz.Margen); // (100-60) + (50-30)
    }

    [Fact]
    public void Agrupa_por_categoria()
    {
        var renglones = new[]
        {
            Renglon(100, 60, productoId: 1, categoriaId: 1, categoria: "Viveres"),
            Renglon(200, 100, productoId: 2, categoriaId: 1, categoria: "Viveres"),
            Renglon(50, 20, productoId: 3, categoriaId: 2, categoria: "Bebidas"),
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal(2, r.PorCategoria.Count);
        var viveres = r.PorCategoria.Single(c => c.Id == 1);
        Assert.Equal(300m, viveres.Subtotal);
        Assert.Equal(140m, viveres.Margen); // (100-60) + (200-100)
    }

    [Fact]
    public void Ordena_por_margen_descendente()
    {
        var renglones = new[]
        {
            Renglon(50, 40, productoId: 1, producto: "Poco"),    // margen 10
            Renglon(200, 50, productoId: 2, producto: "Mucho"),  // margen 150
            Renglon(100, 70, productoId: 3, producto: "Medio"),  // margen 30
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal("Mucho", r.PorProducto[0].Nombre);
        Assert.Equal("Medio", r.PorProducto[1].Nombre);
        Assert.Equal("Poco", r.PorProducto[2].Nombre);
    }

    [Fact]
    public void El_total_suma_todos_los_renglones()
    {
        var renglones = new[]
        {
            Renglon(100, 60, productoId: 1, categoriaId: 1),
            Renglon(200, 100, productoId: 2, categoriaId: 2),
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal(300m, r.Total.Subtotal);
        Assert.Equal(160m, r.Total.Costo);
        Assert.Equal(140m, r.Total.Margen);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter FullyQualifiedName~ResumenRentabilidadTests`
Expected: FALLA de compilación — `ResumenRentabilidad` y `RenglonVendido` no existen.

- [ ] **Step 3: Write minimal implementation**

`src/MiniERP.Domain/Finanzas/ResumenRentabilidad.cs`:

```csharp
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Finanzas;

/// <summary>
/// Rentabilidad de un periodo en tres cortes: total, por producto y por categoria.
/// Es un valor calculado desde los renglones vendidos; no se guarda.
/// </summary>
public record ResumenRentabilidad(
    RentabilidadTotal Total,
    IReadOnlyList<RentabilidadPorItem> PorProducto,
    IReadOnlyList<RentabilidadPorItem> PorCategoria)
{
    public static ResumenRentabilidad Calcular(IEnumerable<RenglonVendido> renglones)
    {
        ArgumentNullException.ThrowIfNull(renglones);

        var vendidos = renglones.ToList();

        var porProducto = vendidos
            .GroupBy(r => (r.ProductoId, r.Producto))
            .Select(g => PorItem(g.Key.ProductoId, g.Key.Producto, g))
            .OrderByDescending(i => i.Margen)
            .ToList();

        var porCategoria = vendidos
            .GroupBy(r => (r.CategoriaId, r.Categoria))
            .Select(g => PorItem(g.Key.CategoriaId, g.Key.Categoria, g))
            .OrderByDescending(i => i.Margen)
            .ToList();

        return new ResumenRentabilidad(Totalizar(vendidos), porProducto, porCategoria);
    }

    private static RentabilidadTotal Totalizar(IEnumerable<RenglonVendido> renglones)
    {
        var subtotal = RetailConstants.RedondearImporte(renglones.Sum(r => r.Subtotal));
        var costo = RetailConstants.RedondearImporte(renglones.Sum(r => r.Costo));
        var margen = subtotal - costo;

        return new RentabilidadTotal(subtotal, costo, margen, Porcentaje(margen, subtotal));
    }

    private static RentabilidadPorItem PorItem(int id, string nombre, IEnumerable<RenglonVendido> renglones)
    {
        var subtotal = RetailConstants.RedondearImporte(renglones.Sum(r => r.Subtotal));
        var costo = RetailConstants.RedondearImporte(renglones.Sum(r => r.Costo));
        var margen = subtotal - costo;

        return new RentabilidadPorItem(id, nombre, subtotal, costo, margen, Porcentaje(margen, subtotal));
    }

    private static decimal Porcentaje(decimal margen, decimal subtotal) =>
        subtotal == 0 ? 0 : margen / subtotal;
}

public record RentabilidadTotal(decimal Subtotal, decimal Costo, decimal Margen, decimal MargenPorcentaje);

public record RentabilidadPorItem(int Id, string Nombre, decimal Subtotal, decimal Costo, decimal Margen, decimal MargenPorcentaje);

/// <summary>Un renglon vendido, tal como lo proyecta el repositorio desde LineaFactura.</summary>
public record RenglonVendido(
    bool FacturaAnulada,
    int ProductoId, string Producto,
    int CategoriaId, string Categoria,
    decimal Subtotal, decimal Costo);
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~ResumenRentabilidadTests`
Expected: PASAN (7 pruebas).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Finanzas/ResumenRentabilidad.cs tests/MiniERP.Tests/Finanzas/ResumenRentabilidadTests.cs
git commit -m "Rentabilidad: calculadora ResumenRentabilidad (margen y cortes por producto/categoria)"
```

---

## Task 2: Excluir las facturas anuladas

**Files:**
- Modify: `src/MiniERP.Domain/Finanzas/ResumenRentabilidad.cs`
- Test: `tests/MiniERP.Tests/Finanzas/ResumenRentabilidadTests.cs`

**Interfaces:**
- Produces: `Calcular` descarta los renglones con `FacturaAnulada == true` antes de totalizar y agrupar.

- [ ] **Step 1: Write the failing test**

Agregar a `ResumenRentabilidadTests.cs`:

```csharp
    [Fact]
    public void Excluye_las_facturas_anuladas()
    {
        var renglones = new[]
        {
            Renglon(100, 60),
            Renglon(999, 1, anulada: true),
        };

        var r = ResumenRentabilidad.Calcular(renglones);

        Assert.Equal(100m, r.Total.Subtotal);
        Assert.Equal(40m, r.Total.Margen);
        Assert.Single(r.PorProducto);
    }
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~ResumenRentabilidadTests`
Expected: FALLA — hoy `Calcular` no excluye anuladas, así que el subtotal daría 1099 y el margen 1038, en vez de 100 y 40.

- [ ] **Step 3: Write minimal implementation**

En `Calcular`, filtrar las anuladas al inicio. Reemplazar la línea `var vendidos = renglones.ToList();` por:

```csharp
        // Una venta anulada no cuenta.
        var vendidos = renglones.Where(r => !r.FacturaAnulada).ToList();
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~ResumenRentabilidadTests`
Expected: PASAN (8 pruebas).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Finanzas/ResumenRentabilidad.cs tests/MiniERP.Tests/Finanzas/ResumenRentabilidadTests.cs
git commit -m "Rentabilidad: la calculadora excluye las facturas anuladas"
```

---

## Cierre de la fase

- [ ] **Correr toda la suite:**

Run: `dotnet test`
Expected: 138 pruebas verdes (130 previas + 8 nuevas).

---

## Fases siguientes (planes aparte, sin bloqueo ni migración)

1. **Aplicación:** `IReporteRentabilidadService` (`ObtenerAsync(FiltroPeriodo)` → `ResumenRentabilidad`), `IReporteRentabilidadRepositorio` (`ObtenerRenglonesDelPeriodoAsync(desde, hasta)` → `IReadOnlyList<RenglonVendido>`), y el `record FiltroPeriodo(DateTime Desde, DateTime Hasta)` en `ReportesDtos.cs`.
2. **Infraestructura:** `ReporteRentabilidadRepositorio` que proyecta desde `LineasFactura` uniendo a `Factura` y `Producto`→`Categoria`: `FacturaAnulada = l.Factura.Estado == EstadoFactura.Anulada`, `Subtotal = l.Cantidad * l.PrecioUnitario`, **`Costo = l.Cantidad * l.CostoUnitario`** (congelado), filtrando `l.Factura.Fecha >= desde && l.Factura.Fecha < hasta.Date.AddDays(1)`. Registro en `DependencyInjection` (partial de Finanzas). **Sin migración.**
3. **Web:** pantalla `/finanzas/rentabilidad` con selector de fechas y los tres cortes (total, tabla por producto, tabla por categoría), enlazada desde la portada.
4. **Verificación del gate:** con la venta real ya registrada (producto RD$ 100, costo 60), la rentabilidad debe dar margen **RD$ 40 (40 %)**.
5. **Capítulo IV:** la sección del reporte de rentabilidad, con captura.
