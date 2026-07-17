# Diseño — Devolución a proveedor (v1: inventario)

- **Fecha:** 2026-07-17
- **Autor:** Samuel Sánchez · rama `samuel/finanzas`
- **Módulo:** Compras (extensión) — Trabajo Final de Grado, Grupo 6
- **Estado:** Aprobado, pendiente de implementar

---

## 1. Contexto y alcance

El módulo de Compras ya recibe mercancía (`Compra.Recibir`): sube la existencia, promedia
el costo y, si es a crédito, sube `Proveedor.BalanceActual`. Lo que falta es lo inverso:
**devolver mercancía de una compra ya recibida.** El tipo `TipoMovimiento.DevolucionProveedor`
existe desde F2 y nadie lo usa.

Una compra recibida **no se anula** (el inventario ya se movió); se **devuelve**, con una
operación nueva que deja su propio rastro. Este documento diseña esa operación.

### Qué entra en el v1 y qué no

| Incluido (v1) | Excluido (v2, coordinado con Dionis) |
|---|---|
| Documento de devolución contra una compra recibida | Ajuste de `Proveedor.BalanceActual` |
| Movimientos `DevolucionProveedor` que bajan la existencia | Nota de crédito / rastro del saldo del proveedor |
| Devoluciones parciales y acumulables | |
| Motivo obligatorio | |

**Por qué se parte en dos:** `Proveedor.BalanceActual` es un campo **compartido** con el
módulo de cobros y pagos de Dionis (su "pago a proveedor" también lo baja). El proyecto
exige acordar el patrón de rastro del saldo **antes** de codificarlo. El lado del inventario
es 100 % del carril de Samuel y no depende de nadie, así que se construye ya; el ajuste del
saldo espera al acuerdo (ver §8).

---

## 2. Decisiones de diseño (material de defensa)

| Decisión | Elección | Razón |
|---|---|---|
| ¿Documento o movimiento suelto? | **Documento propio** (`DevolucionCompra`) | "Cada operación deja rastro" = un documento. Da número, fecha, motivo y totales. Es el rastro del que colgará la baja del saldo en el v2, sin retrabajo. |
| ¿Contra qué se devuelve? | Contra una **compra recibida** concreta | La validación "no más de lo comprado" sale gratis y el costo sale de la línea de compra. |
| Costo de la devolución | El **costo congelado de la línea de compra** | Es lo que se pagó y lo que el proveedor acredita. No se inventa un costo nuevo. |
| ¿Se repromedia el costo del producto al devolver? | **No** | Una salida no aporta información de costo nuevo; repromediar distorsionaría sin fundamento. |
| Motivo | **Obligatorio** | Una salida discrecional exige explicación escrita, igual que la merma. |
| Parcial / acumulable | **Sí** | Realista: se devuelve lo dañado o vencido, casi nunca el pedido entero. |
| Transacción | **Un solo `SaveChanges`** | Igual que `Recibir`: o entra todo (documento + movimientos + existencia) o nada. |
| Migración | **Al final, en ventana segura** | `MiniErpDbContext` y `ModelSnapshot` son compartidos por tres devs: crear la migración fuera de una ventana coordinada es zona de conflictos. |

---

## 3. Modelo de dominio (`MiniERP.Domain.Compras`)

### `EstadoDevolucion`

`Borrador = 1`, `Confirmada = 2`. Solo lo **confirmado** mueve inventario y cuenta como
"lo ya devuelto".

### `DevolucionCompra : EntidadBase`

- `Numero`, `CompraId` + `Compra?`, `ProveedorId` + `Proveedor?`, `Fecha`
- `Motivo` — **obligatorio**
- `Estado`, `Subtotal`, `Itbis`, `Total` (congelados), `Lineas`
- `EsEditable => Estado == Borrador`
- `Recalcular()` — rearma los totales desde las líneas con `RetailConstants.RedondearImporte`

### `LineaDevolucionCompra : EntidadBase`

- `DevolucionCompraId`, **`LineaCompraId`** (línea de compra origen), `ProductoId`
- `Descripcion`, `Cantidad`, `CostoUnitario`, `TasaItbis` — **congelados desde la línea de compra**
- `Subtotal` / `Itbis` / `Total` computados, idénticos a `LineaCompra`

---

## 4. El corazón — `Confirmar`

```csharp
IReadOnlyList<MovimientoInventario> Confirmar(
    IReadOnlyDictionary<int, Producto> productos,
    IReadOnlyDictionary<int, decimal> devolvibleMaximoPorLineaCompra, // LineaCompraId → aún devolvible
    string? usuarioId)
```

**Valida, en orden, antes de tocar nada:**

1. `Estado == Borrador` (no se confirma dos veces).
2. Tiene al menos una línea.
3. `Motivo` no vacío.
4. Por línea: el producto existe en `productos`; `Cantidad > 0`; `Cantidad ≤ devolvibleMáximo`
   de su línea de compra origen.

**Luego, por línea:** crea el movimiento `DevolucionProveedor`
(`ReferenciaTipo = "DEVOLUCION_COMPRA"`, `ReferenciaId = Id`,
`Motivo = $"Devolución {Numero}: {Motivo}"`, `CostoUnitario` de la línea) y llama
`producto.AplicarMovimiento(...)`, que **baja la existencia y bloquea dejarla en negativo**.
Al final, `Estado = Confirmada`.

**Dos invariantes, dos redes:** no se devuelve más de lo comprado (validación 4) *ni* más de
lo que queda físicamente en el estante (guard de `AplicarMovimiento`, por si ya se vendió).
El costo del producto **no** se repromedia.

El cálculo de "lo ya devuelto" vive en el **servicio** (suma las devoluciones confirmadas
desde el repositorio y lo pasa como `devolvibleMáximo`). El dominio solo hace cumplir el
invariante, y así se mantiene puro y testeable sin base de datos.

---

## 5. Capa de aplicación (`MiniERP.Application.Compras`)

### `IDevolucionCompraService`

- `NuevaDesdeCompraAsync(compraId)` — arma el form con las líneas de la compra y su cantidad
  aún devolvible.
- `GuardarAsync(form, usuarioId)` — persiste como `Borrador`.
- `ConfirmarAsync(devolucionId, usuarioId)` — carga con seguimiento, calcula el devolvible por
  línea, llama `Confirmar`, `AgregarMovimientos`, y persiste **todo en un solo `SaveChanges`**
  (misma transacción atómica que `RecibirAsync`).
- `BuscarAsync(filtro)`.

### `IDevolucionCompraRepositorio`

`ObtenerConLineasAsync`, `ObtenerProductosDeAsync`, `ObtenerDevueltoPorLineaCompraAsync(compraId)`,
`SugerirNumeroAsync`, `Agregar`, `AgregarMovimientos`, `GuardarAsync`. La compra origen se lee
con el `ICompraRepositorio` existente.

### DTOs

`DevolucionFormDto`, `LineaDevolucionFormDto`, `DevolucionListaDto`, `FiltroDevoluciones`.

---

## 6. Persistencia y UI (gated tras la migración)

- Configuración EF de las dos entidades + `DbSet` en `MiniErpDbContext`.
- `DevolucionCompraRepositorio` en Infrastructure.
- **Migración** `AgregarDevolucionCompra` — **solo cuando la ventana esté segura**. Estado al
  2026-07-17: ni Samuel ni Dionis han creado su migración todavía, y el `ModelSnapshot` es uno
  solo. El Plan Maestro pone a Dionis primero en F3, así que el orden acordado es: **Dionis
  fusiona sus cobros/pagos con su migración a `main` primero; luego Samuel trae `main` y crea la
  suya encima.** El dominio y la aplicación de la devolución no dependen de esto.
- Pantalla Blazor: lista de devoluciones + crear desde una compra recibida + confirmar.

Nada de las capas Domain y Application modifica el `ModelSnapshot`. El 70 % del trabajo —el
que importa para la defensa— avanza sin depender de la ventana de migración.

---

## 7. Pruebas de dominio — se escriben primero (`DevolucionCompraTests`)

Criterio de aceptación del dominio. En verde = dominio terminado, sin migración.

1. Baja el inventario (20 → devuelvo 3 → 17).
2. Congela el documento (`Confirmada`, no editable).
3. El movimiento es `DevolucionProveedor` con su referencia y su motivo.
4. El movimiento usa el **costo de la compra**.
5. No se devuelve más de lo comprado.
6. Las devoluciones parciales **acumulan** (compré 10, ya devolví 7 → solo quedan 3).
7. No se devuelve más de lo que hay en existencia (ya vendido).
8. Sin motivo no se confirma.
9. Sin líneas no se confirma.
10. Sin el producto de la línea, falla antes de tocar nada.
11. No se confirma dos veces.
12. Los totales se arman desde las líneas (redondeo `AwayFromZero`).
13. El costo del producto **no se repromedia** al devolver.

---

## 8. El seam del v2 (balance) — patrón ya definido por el código de Dionis

El documento ya tiene `Total` congelado. El v2 baja `Proveedor.BalanceActual` al confirmar la
devolución.

**El patrón de rastro ya está decidido** por el código de Dionis en `dionis/cobros-pagos`
(revisado el 2026-07-17): su entidad `Pago` guarda `BalanceAnterior`/`BalanceResultante`, y
`Proveedor.AplicarPago(pago)` muta el saldo y estampa el rastro en el documento —exactamente
como `MovimientoInventario` y `Producto.AplicarMovimiento` hacen con la existencia. Es "rastro
por documento", enriquecido con el balance antes/después.

El v2 de la devolución **espeja ese patrón**: un `Proveedor.AplicarDevolucion(devolucion)` que
baje el saldo y estampe antes/después en el documento de devolución. No se introduce un libro
mayor aparte.

**Matiz a afinar con Dionis:** `AplicarPago` prohíbe pagar más que la deuda. Una devolución en
efectivo (compra de contado) dejaría el saldo en negativo —el proveedor pasaría a deberle al
comercio—, caso que ese invariante no contempla. Ese borde se cierra junto con Dionis en el v2,
sin romper su `AplicarPago`.

---

## 9. Criterio de "terminado" (según `Docs/Asignaciones.md`)

La tarea completa exige que "el inventario baja y el balance del proveedor se ajusta". El v1
cierra la primera mitad (inventario, con pruebas y navegador). La segunda mitad (balance)
cierra en el v2. Además, un módulo no está terminado hasta que su **sección del Capítulo IV**
existe, escrita la misma semana y con capturas.

## 10. Notas para el Capítulo IV

Documentar: por qué documento y no movimiento suelto · por qué el costo sale de la compra y no
se repromedia · por qué motivo obligatorio · por qué un solo `SaveChanges` · por qué la
migración va al final (disciplina del `ModelSnapshot` compartido) · el seam del balance y la
coordinación con Dionis.
