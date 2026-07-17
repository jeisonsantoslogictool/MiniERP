# Devolución a proveedor — Plan de implementación (Fase Dominio)

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir el dominio de la devolución a proveedor (v1: inventario), con TDD, sin tocar la base de datos ni crear migración.

**Architecture:** Un documento `DevolucionCompra` con sus `LineaDevolucionCompra` referencia una compra recibida. Su método `Confirmar` valida y genera movimientos `DevolucionProveedor` que bajan la existencia vía `Producto.AplicarMovimiento`, y congela el documento. El dominio es C# puro (sin EF): sus pruebas corren sin base de datos. Réplica del patrón de `Compra.Recibir`.

**Tech Stack:** .NET 10 · C# · xUnit · capa `MiniERP.Domain` (sin dependencias).

## Global Constraints

- **El dominio no conoce EF Core.** Estas clases y sus pruebas no tocan la base ni el `MiniErpDbContext`.
- **Ningún saldo/existencia se edita a mano:** la existencia solo cambia por `Producto.AplicarMovimiento`, que deja rastro y bloquea negativos.
- **Redondeo aritmético:** `RetailConstants.RedondearImporte` (AwayFromZero) al totalizar; el ITBIS de línea con `Math.Round(..., RetailConstants.DecimalesImporte, MidpointRounding.AwayFromZero)`, idéntico a `LineaCompra`.
- **Nombres de dominio en español** (`DevolucionCompra`, `Motivo`, `Confirmar`).
- **El balance del proveedor NO se toca en esta fase** (v2, coordinado con Dionis).
- **Pruebas automatizadas solo del dominio** (convención del repo: servicios, repos y pantallas se verifican a mano).
- **Sin migración en esta fase.** La migración va gated: Dionis fusiona sus cobros/pagos a `main` primero (Plan Maestro, F3), luego Samuel.

---

## File Structure

| Archivo | Responsabilidad |
|---|---|
| `src/MiniERP.Domain/Compras/EstadoDevolucion.cs` (crear) | Enum del ciclo de vida: `Borrador`, `Confirmada`. |
| `src/MiniERP.Domain/Compras/LineaDevolucionCompra.cs` (crear) | Renglón: producto, cantidad, costo congelado, ITBIS de línea. |
| `src/MiniERP.Domain/Compras/DevolucionCompra.cs` (crear) | Documento: `Recalcular` + `Confirmar` (el corazón). |
| `tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs` (crear) | Las 13 pruebas de dominio. |

---

## Task 1: Entidades + `Recalcular` (totales)

**Files:**
- Create: `src/MiniERP.Domain/Compras/EstadoDevolucion.cs`
- Create: `src/MiniERP.Domain/Compras/LineaDevolucionCompra.cs`
- Create: `src/MiniERP.Domain/Compras/DevolucionCompra.cs`
- Test: `tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs`

**Interfaces:**
- Consumes: `MiniERP.Domain.Shared.EntidadBase`, `RetailConstants`; `MiniERP.Domain.Inventario.Producto`, `MovimientoInventario`, `TipoMovimiento`; `MiniERP.Domain.Compras.Compra`, `LineaCompra`, `Proveedor`.
- Produces: `DevolucionCompra` con `Numero`, `CompraId`, `ProveedorId`, `Fecha`, `Motivo`, `Estado`, `Subtotal`, `Itbis`, `Total`, `Lineas`, `EsEditable`, `Recalcular()`, y `Confirmar(IReadOnlyDictionary<int,Producto>, IReadOnlyDictionary<int,decimal>, string?)`. `LineaDevolucionCompra` con `LineaCompraId`, `ProductoId`, `Descripcion`, `Cantidad`, `CostoUnitario`, `TasaItbis`, `Subtotal`, `Itbis`, `Total`. `EstadoDevolucion { Borrador=1, Confirmada=2 }`.

- [ ] **Step 1: Write the failing test**

Crear `tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs` con los helpers y la primera prueba:

```csharp
using MiniERP.Domain.Compras;
using MiniERP.Domain.Inventario;

namespace MiniERP.Tests.Compras;

public class DevolucionCompraTests
{
    private static Producto Producto(decimal existencia = 50, decimal costo = 30) => new()
    {
        Id = 1,
        Codigo = "ARR-001",
        Descripcion = "Arroz selecto",
        Existencia = existencia,
        Costo = costo,
        ManejaInventario = true
    };

    private static DevolucionCompra Devolucion(
        decimal cantidad,
        decimal costoUnitario = 30,
        string motivo = "Producto vencido",
        decimal itbis = 0m,
        int lineaCompraId = 100)
    {
        var dev = new DevolucionCompra
        {
            Id = 5,
            Numero = "DEV-0005",
            CompraId = 7,
            ProveedorId = 1,
            Motivo = motivo
        };

        dev.Lineas.Add(new LineaDevolucionCompra
        {
            LineaCompraId = lineaCompraId,
            ProductoId = 1,
            Descripcion = "Arroz selecto",
            Cantidad = cantidad,
            CostoUnitario = costoUnitario,
            TasaItbis = itbis
        });

        return dev;
    }

    private static Dictionary<int, Producto> Catalogo(Producto p) => new() { [p.Id] = p };

    private static Dictionary<int, decimal> Devolvible(decimal cantidad, int lineaCompraId = 100) =>
        new() { [lineaCompraId] = cantidad };

    [Fact]
    public void Los_totales_se_arman_desde_las_lineas()
    {
        var dev = Devolucion(cantidad: 10, costoUnitario: 30, itbis: 0.18m);

        dev.Recalcular();

        Assert.Equal(300m, dev.Subtotal);
        Assert.Equal(54m, dev.Itbis);
        Assert.Equal(354m, dev.Total);
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test --filter FullyQualifiedName~DevolucionCompraTests`
Expected: FALLA de compilación — `DevolucionCompra`, `LineaDevolucionCompra`, `EstadoDevolucion` no existen.

- [ ] **Step 3: Write minimal implementation**

`src/MiniERP.Domain/Compras/EstadoDevolucion.cs`:

```csharp
namespace MiniERP.Domain.Compras;

/// <summary>Punto del ciclo de vida en que está una devolución a proveedor.</summary>
public enum EstadoDevolucion
{
    /// <summary>Se está capturando. Único estado editable; aún no movió inventario.</summary>
    Borrador = 1,

    /// <summary>La mercancía salió. El documento queda congelado y el inventario ya bajó.</summary>
    Confirmada = 2
}
```

`src/MiniERP.Domain/Compras/LineaDevolucionCompra.cs`:

```csharp
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Compras;

/// <summary>
/// Renglón de una devolución: qué producto, cuánto y a qué costo (el de la compra origen).
/// </summary>
public class LineaDevolucionCompra : EntidadBase
{
    public int DevolucionCompraId { get; set; }
    public DevolucionCompra? Devolucion { get; set; }

    /// <summary>Línea de la compra que se devuelve. Ancla el costo y el tope devolvible.</summary>
    public int LineaCompraId { get; set; }
    public LineaCompra? LineaCompra { get; set; }

    public int ProductoId { get; set; }
    public Producto? Producto { get; set; }

    public string Descripcion { get; set; } = string.Empty;

    public decimal Cantidad { get; set; }

    /// <summary>Costo congelado desde la línea de compra: lo que el proveedor acredita.</summary>
    public decimal CostoUnitario { get; set; }

    public decimal TasaItbis { get; set; }

    public decimal Subtotal => Cantidad * CostoUnitario;

    public decimal Itbis => Math.Round(Subtotal * TasaItbis, RetailConstants.DecimalesImporte, MidpointRounding.AwayFromZero);

    public decimal Total => Subtotal + Itbis;
}
```

`src/MiniERP.Domain/Compras/DevolucionCompra.cs` (sin `Confirmar` todavía — se agrega en la Task 2):

```csharp
using MiniERP.Domain.Inventario;
using MiniERP.Domain.Shared;

namespace MiniERP.Domain.Compras;

/// <summary>
/// Devolución de mercancía de una compra recibida al proveedor. No anula la compra:
/// es una operación nueva que baja la existencia y deja su propio rastro.
/// </summary>
public class DevolucionCompra : EntidadBase
{
    public string Numero { get; set; } = string.Empty;

    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int ProveedorId { get; set; }
    public Proveedor? Proveedor { get; set; }

    public DateTime Fecha { get; set; } = DateTime.UtcNow;

    /// <summary>Por qué se devuelve. Obligatorio: una salida discrecional exige explicación.</summary>
    public string Motivo { get; set; } = string.Empty;

    public EstadoDevolucion Estado { get; set; } = EstadoDevolucion.Borrador;

    /// <summary>Totales congelados en el documento.</summary>
    public decimal Subtotal { get; set; }
    public decimal Itbis { get; set; }
    public decimal Total { get; set; }

    public ICollection<LineaDevolucionCompra> Lineas { get; set; } = [];

    public bool EsEditable => Estado == EstadoDevolucion.Borrador;

    /// <summary>Rearma los totales del encabezado desde las líneas.</summary>
    public void Recalcular()
    {
        Subtotal = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Subtotal));
        Itbis = RetailConstants.RedondearImporte(Lineas.Sum(l => l.Itbis));
        Total = Subtotal + Itbis;
    }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test --filter FullyQualifiedName~DevolucionCompraTests`
Expected: PASA (1 prueba).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Compras/EstadoDevolucion.cs src/MiniERP.Domain/Compras/LineaDevolucionCompra.cs src/MiniERP.Domain/Compras/DevolucionCompra.cs tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs
git commit -m "Devolucion: entidades y totales (Recalcular)"
```

---

## Task 2: `Confirmar` — camino feliz

**Files:**
- Modify: `src/MiniERP.Domain/Compras/DevolucionCompra.cs`
- Test: `tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs`

**Interfaces:**
- Consumes: `Producto.AplicarMovimiento(MovimientoInventario)`; `TipoMovimiento.DevolucionProveedor`.
- Produces: `DevolucionCompra.Confirmar(...)` que devuelve `IReadOnlyList<MovimientoInventario>` y pone `Estado = Confirmada`.

- [ ] **Step 1: Write the failing tests**

Agregar a `DevolucionCompraTests.cs`:

```csharp
    [Fact]
    public void Confirmar_baja_el_inventario()
    {
        var producto = Producto(existencia: 20);
        var dev = Devolucion(cantidad: 3);

        dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        Assert.Equal(17, producto.Existencia);
    }

    [Fact]
    public void Confirmar_congela_el_documento()
    {
        var producto = Producto();
        var dev = Devolucion(3);

        dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        Assert.Equal(EstadoDevolucion.Confirmada, dev.Estado);
        Assert.False(dev.EsEditable);
    }

    [Fact]
    public void El_movimiento_generado_es_una_devolucion_a_proveedor()
    {
        var producto = Producto();
        var dev = Devolucion(3, motivo: "Producto vencido");

        var movimientos = dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        var mov = Assert.Single(movimientos);
        Assert.Equal(TipoMovimiento.DevolucionProveedor, mov.Tipo);
        Assert.Equal("DEVOLUCION_COMPRA", mov.ReferenciaTipo);
        Assert.Equal(dev.Id, mov.ReferenciaId);
        Assert.Equal("Devolución DEV-0005: Producto vencido", mov.Motivo);
        Assert.Equal("tester", mov.UsuarioId);
    }

    [Fact]
    public void El_movimiento_usa_el_costo_congelado_de_la_compra()
    {
        // El costo vivo del producto (99) no importa: se devuelve al costo de la compra (30).
        var producto = Producto(costo: 99);
        var dev = Devolucion(3, costoUnitario: 30);

        var movimientos = dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        Assert.Equal(30, Assert.Single(movimientos).CostoUnitario);
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter FullyQualifiedName~DevolucionCompraTests`
Expected: FALLA de compilación — `Confirmar` no existe.

- [ ] **Step 3: Write minimal implementation**

Agregar el método a `DevolucionCompra` (dentro de la clase, tras `Recalcular`):

```csharp
    /// <summary>
    /// Confirma la devolución: valida, genera los movimientos de salida (que bajan la
    /// existencia) y congela el documento.
    /// </summary>
    /// <param name="productos">Productos de las líneas, indexados por Id, con seguimiento.</param>
    /// <param name="devolvibleMaximoPorLineaCompra">
    /// Cuánto queda por devolver de cada línea de compra (comprado − ya devuelto confirmado),
    /// indexado por <c>LineaCompraId</c>. Lo calcula el servicio.
    /// </param>
    /// <param name="usuarioId">Responsable de la devolución.</param>
    /// <returns>Los movimientos generados, para que la capa de datos los persista.</returns>
    public IReadOnlyList<MovimientoInventario> Confirmar(
        IReadOnlyDictionary<int, Producto> productos,
        IReadOnlyDictionary<int, decimal> devolvibleMaximoPorLineaCompra,
        string? usuarioId)
    {
        ArgumentNullException.ThrowIfNull(productos);
        ArgumentNullException.ThrowIfNull(devolvibleMaximoPorLineaCompra);

        var movimientos = new List<MovimientoInventario>(Lineas.Count);

        foreach (var linea in Lineas)
        {
            var producto = productos[linea.ProductoId];

            var movimiento = new MovimientoInventario
            {
                Tipo = TipoMovimiento.DevolucionProveedor,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                Motivo = $"Devolución {Numero}: {Motivo}",
                ReferenciaTipo = "DEVOLUCION_COMPRA",
                ReferenciaId = Id,
                UsuarioId = usuarioId,
                CreadoPor = usuarioId
            };

            producto.AplicarMovimiento(movimiento);
            movimientos.Add(movimiento);
        }

        Estado = EstadoDevolucion.Confirmada;

        return movimientos;
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~DevolucionCompraTests`
Expected: PASAN (5 pruebas).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Compras/DevolucionCompra.cs tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs
git commit -m "Devolucion: Confirmar baja inventario y genera el movimiento"
```

---

## Task 3: `Confirmar` — invariantes y validaciones

**Files:**
- Modify: `src/MiniERP.Domain/Compras/DevolucionCompra.cs`
- Test: `tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs`

**Interfaces:**
- Produces: `Confirmar` lanza `InvalidOperationException` cuando: ya está confirmada, no tiene líneas, falta motivo, falta el producto, la línea no pertenece a la compra, la cantidad excede lo devolvible, o el movimiento dejaría la existencia negativa (vía `AplicarMovimiento`).

- [ ] **Step 1: Write the failing tests**

Agregar a `DevolucionCompraTests.cs`:

```csharp
    [Fact]
    public void No_se_devuelve_mas_de_lo_comprado()
    {
        var producto = Producto(existencia: 20);
        var dev = Devolucion(cantidad: 6);

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(Catalogo(producto), Devolvible(5), "tester"));

        Assert.Contains("quedan 5", ex.Message);
        Assert.Equal(20, producto.Existencia); // no tocó nada
    }

    [Fact]
    public void Se_puede_devolver_exactamente_lo_que_queda_por_devolver()
    {
        // Compré 10, ya devolví 7 → quedan 3. Devuelvo 3: pasa justo en el borde.
        var producto = Producto(existencia: 20);
        var dev = Devolucion(cantidad: 3);

        dev.Confirmar(Catalogo(producto), Devolvible(3), "tester");

        Assert.Equal(17, producto.Existencia);
        Assert.Equal(EstadoDevolucion.Confirmada, dev.Estado);
    }

    [Fact]
    public void No_se_devuelve_mas_de_lo_que_hay_en_existencia()
    {
        // El proveedor deja devolver 10, pero ya se vendieron y solo quedan 2 en el estante.
        var producto = Producto(existencia: 2);
        var dev = Devolucion(cantidad: 5);

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(Catalogo(producto), Devolvible(10), "tester"));

        Assert.Contains("negativa", ex.Message);
        Assert.Equal(2, producto.Existencia);
    }

    [Fact]
    public void Una_devolucion_sin_motivo_no_se_confirma()
    {
        var producto = Producto();
        var dev = Devolucion(3, motivo: "   ");

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(Catalogo(producto), Devolvible(10), "tester"));

        Assert.Contains("motivo", ex.Message);
        Assert.Equal(50, producto.Existencia);
    }

    [Fact]
    public void Una_devolucion_sin_lineas_no_se_confirma()
    {
        var dev = new DevolucionCompra { Numero = "DEV-0001", Motivo = "x" };

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(new Dictionary<int, Producto>(), new Dictionary<int, decimal>(), "tester"));

        Assert.Contains("no tiene líneas", ex.Message);
    }

    [Fact]
    public void Confirmar_sin_el_producto_de_la_linea_falla_antes_de_tocar_nada()
    {
        var dev = Devolucion(3);

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(new Dictionary<int, Producto>(), Devolvible(10), "tester"));

        Assert.Contains("Falta el producto", ex.Message);
        Assert.Equal(EstadoDevolucion.Borrador, dev.Estado);
    }

    [Fact]
    public void Una_devolucion_no_se_confirma_dos_veces()
    {
        var producto = Producto(existencia: 20);
        var dev = Devolucion(3);
        dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        var ex = Assert.Throws<InvalidOperationException>(
            () => dev.Confirmar(Catalogo(producto), Devolvible(10), "tester"));

        Assert.Contains("confirmada", ex.Message);
        Assert.Equal(17, producto.Existencia); // no volvió a bajar
    }
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test --filter FullyQualifiedName~DevolucionCompraTests`
Expected: FALLAN las validaciones nuevas — tope devolvible, motivo, líneas vacías, producto faltante (hoy lanza `KeyNotFoundException` en vez de `InvalidOperationException`) y doble confirmación. **Dos ya pasan** con el código de la Task 2: `Se_puede_devolver_exactamente...` (camino feliz) y `No_se_devuelve_mas_de_lo_que_hay_en_existencia` (el guard de `AplicarMovimiento` ya bloquea negativos).

- [ ] **Step 3: Write minimal implementation**

Reemplazar el cuerpo de `Confirmar` para validar **antes de mover nada**. El método completo queda así:

```csharp
    public IReadOnlyList<MovimientoInventario> Confirmar(
        IReadOnlyDictionary<int, Producto> productos,
        IReadOnlyDictionary<int, decimal> devolvibleMaximoPorLineaCompra,
        string? usuarioId)
    {
        ArgumentNullException.ThrowIfNull(productos);
        ArgumentNullException.ThrowIfNull(devolvibleMaximoPorLineaCompra);

        if (Estado != EstadoDevolucion.Borrador)
            throw new InvalidOperationException(
                $"La devolución {Numero} está {Estado.ToString().ToLowerInvariant()} y no se puede confirmar.");

        if (Lineas.Count == 0)
            throw new InvalidOperationException($"La devolución {Numero} no tiene líneas.");

        if (string.IsNullOrWhiteSpace(Motivo))
            throw new InvalidOperationException($"La devolución {Numero} exige un motivo.");

        // Se valida todo antes de mover el inventario: si algo falla, no se tocó nada.
        foreach (var linea in Lineas)
        {
            if (!productos.ContainsKey(linea.ProductoId))
                throw new InvalidOperationException(
                    $"Falta el producto de la línea '{linea.Descripcion}'.");

            if (!devolvibleMaximoPorLineaCompra.TryGetValue(linea.LineaCompraId, out var maximo))
                throw new InvalidOperationException(
                    $"La línea '{linea.Descripcion}' no corresponde a la compra que se devuelve.");

            if (linea.Cantidad > maximo)
                throw new InvalidOperationException(
                    $"No puedes devolver {linea.Cantidad} de '{linea.Descripcion}': solo quedan {maximo} por devolver.");
        }

        var movimientos = new List<MovimientoInventario>(Lineas.Count);

        foreach (var linea in Lineas)
        {
            var producto = productos[linea.ProductoId];

            var movimiento = new MovimientoInventario
            {
                Tipo = TipoMovimiento.DevolucionProveedor,
                Cantidad = linea.Cantidad,
                CostoUnitario = linea.CostoUnitario,
                Motivo = $"Devolución {Numero}: {Motivo}",
                ReferenciaTipo = "DEVOLUCION_COMPRA",
                ReferenciaId = Id,
                UsuarioId = usuarioId,
                CreadoPor = usuarioId
            };

            producto.AplicarMovimiento(movimiento);
            movimientos.Add(movimiento);
        }

        Estado = EstadoDevolucion.Confirmada;

        return movimientos;
    }
```

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test --filter FullyQualifiedName~DevolucionCompraTests`
Expected: PASAN (12 pruebas).

- [ ] **Step 5: Commit**

```bash
git add src/MiniERP.Domain/Compras/DevolucionCompra.cs tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs
git commit -m "Devolucion: validaciones de Confirmar (tope devolvible, motivo, estado, existencia)"
```

---

## Task 4: El costo del producto no se repromedia (guarda de decisión)

**Files:**
- Test: `tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs`

**Interfaces:**
- Consumes: comportamiento existente de `Confirmar` (no toca `Producto.Costo`).

- [ ] **Step 1: Write the test**

Agregar a `DevolucionCompraTests.cs`:

```csharp
    [Fact]
    public void El_costo_del_producto_no_se_repromedia_al_devolver()
    {
        var producto = Producto(existencia: 20, costo: 35);
        var dev = Devolucion(3, costoUnitario: 30);

        dev.Confirmar(Catalogo(producto), Devolvible(10), "tester");

        Assert.Equal(35, producto.Costo); // el costo vivo no cambia
    }
```

- [ ] **Step 2: Run the test**

Run: `dotnet test --filter FullyQualifiedName~DevolucionCompraTests`
Expected: PASA de una — `Confirmar` nunca escribe `Producto.Costo`. Es una prueba de regresión que fija la decisión "una salida no aporta costo nuevo".

- [ ] **Step 3: Commit**

```bash
git add tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs
git commit -m "Devolucion: guarda de que el costo del producto no se repromedia"
```

---

## Cierre de la fase

- [ ] **Correr toda la suite** para confirmar que no se rompió nada del resto:

Run: `dotnet test`
Expected: 113 pruebas verdes (100 previas + 13 nuevas).

- [ ] **Escribir la sección del Capítulo IV** de la devolución (misma semana, con capturas cuando exista la pantalla), según exige `Docs/Asignaciones.md`.

---

## Fases siguientes (planes aparte)

Estas capas se planifican y ejecutan después; no forman parte de esta fase.

1. **Aplicación (escribible ya, sin migración):** `IDevolucionCompraService` + `DevolucionCompraService` (con `ConfirmarAsync` que calcula el devolvible desde el repo, llama `Confirmar`, `AgregarMovimientos` y persiste en **un solo `SaveChanges`**), `IDevolucionCompraRepositorio`, DTOs. Se verifica a mano (convención del repo).
2. **Infraestructura + migración (GATED):** configuración EF, repositorio, `DbSet`, y la migración `AgregarDevolucionCompra`. Se dispara **cuando la ventana esté segura**: Dionis fusiona sus cobros/pagos con su migración a `main` primero (Plan Maestro, F3); luego Samuel trae `main` y crea la suya encima.
3. **Web (GATED):** pantalla de devoluciones (lista + crear desde una compra recibida + confirmar). Se prueba en el navegador tras la migración.
4. **v2 — balance del proveedor:** espeja `Proveedor.AplicarPago` de Dionis con un `AplicarDevolucion` (rastro con balance antes/después), afinando el borde de la devolución de contado. Se coordina con Dionis.
