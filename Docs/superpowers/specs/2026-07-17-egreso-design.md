# Diseño — Egreso (Finanzas #1)

- **Fecha:** 2026-07-17
- **Autor:** Samuel Sánchez · rama `samuel/finanzas`
- **Módulo:** Finanzas (nuevo) — Trabajo Final de Grado, Grupo 6
- **Estado:** Aprobado, pendiente de implementar

---

## 1. Qué es y para qué sirve

Un **egreso** es un gasto operativo del comercio que **no pasa por el inventario**: alquiler,
luz, agua, sueldos, transporte. Esta funcionalidad le permite al comerciante registrarlos con
su fecha, categoría, monto y una nota.

Sirve para que los reportes de finanzas puedan **restar** esos gastos y mostrar la ganancia
real. El estado de resultados (tarea #4) resta los egresos del período; el flujo de caja
(tarea #5) resta los egresos pagados. Es la primera pieza del módulo `Finanzas/`, que hasta
hoy está vacío.

### La trampa que este diseño evita

Un **pago a proveedor NO es un egreso.** El egreso es un gasto operativo; el pago a proveedor
liquida una deuda por mercancía cuyo costo ya se contó al vender. Registrar pagos como egresos
contaría el costo dos veces y produciría una utilidad falsa. Por eso el egreso **nunca toca el
inventario ni el balance de un proveedor** — es solo un registro de gasto. (Ver CLAUDE.md,
sección "Un pago a proveedor NO es un egreso".)

---

## 2. Decisiones de diseño (material de defensa)

| Decisión | Elección | Razón |
|---|---|---|
| Categoría del gasto | **Entidad `CategoriaEgreso`** (catálogo sembrado y editable) | El comerciante querrá agregar sus propias categorías; el estado de resultados agrupa por categoría; calca el patrón de `Categoria` de productos. |
| ITBIS del gasto | **Un solo `Monto`, sin desglose de ITBIS** | El desglose solo serviría para crédito fiscal (606), que está fuera de alcance. El comerciante registra "la luz me costó 5,000". |
| Pago del gasto | **Se registra ya pagado (contado)** | El comercio no lleva cuentas por pagar de sus gastos: paga la luz, el alquiler y los sueldos en el momento. La `Fecha` es cuándo ocurrió y se pagó. |
| Siembra del catálogo | **Sembrador de arranque (`DatabaseInitializer`), no `HasData`** | Regla del proyecto: lo que el usuario edita va en el sembrador; `HasData` es solo para catálogo inmutable. |
| Alcance de layers | **Feature completa (dominio → pantalla)**, sin gating | Es una tabla nueva en un módulo nuevo, sin entidades compartidas. No depende de Dionis ni de Jeison. Solo toca los tres archivos compartidos (DbContext, DI, migración): traer `main` antes. |

---

## 3. Modelo de dominio (`MiniERP.Domain.Finanzas`)

### `CategoriaEgreso : EntidadBase`

- `Nombre` (obligatorio), `Descripcion?`, `Activo` (default true)
- `ICollection<Egreso> Egresos`
- Gemela de `Categoria` de productos.

### `Egreso : EntidadBase`

- `Fecha` (default UtcNow) — cuándo ocurrió y se pagó
- `CategoriaEgresoId` + `CategoriaEgreso?`
- `Monto` (decimal)
- `Descripcion?` (nota opcional)

### `Egreso.Registrar(...)` — guard del dominio

```csharp
static Egreso Registrar(
    decimal monto, DateTime fecha, int categoriaEgresoId,
    string? descripcion, string? usuarioId)
```

Valida los invariantes **antes de construir** el egreso y lanza `InvalidOperationException` si:

1. `monto <= 0` — un gasto no puede ser cero ni negativo.
2. `categoriaEgresoId == 0` — todo gasto debe estar clasificado.

Si pasa, devuelve un `Egreso` válido. Es el objetivo del TDD. Al **editar** un egreso, el
servicio aplica las mismas dos reglas antes de guardar.

---

## 4. Pruebas de dominio (TDD, se escriben primero) — `EgresoTests`

1. `Registrar` con monto negativo lanza.
2. `Registrar` con monto cero lanza.
3. `Registrar` sin categoría (id 0) lanza.
4. `Registrar` válido crea el egreso con su fecha, monto, categoría, descripción y usuario.
5. `Registrar` acepta descripción nula (la nota es opcional).

En verde = dominio terminado, sin base de datos.

---

## 5. Capa de aplicación (`MiniERP.Application.Finanzas`)

- **`IEgresoService`**: `BuscarAsync` (filtro por rango de fechas + categoría, con el total del
  período), `ObtenerAsync`, `GuardarAsync` (crea vía `Registrar` / edita re-validando),
  `EliminarAsync`.
- **`ICategoriaEgresoService`**: `BuscarAsync`, `ObtenerOpcionesAsync` (para el desplegable del
  formulario), `GuardarAsync`, activar/desactivar.
- Contratos de repositorio (`IEgresoRepositorio`, `ICategoriaEgresoRepositorio`) y DTOs
  (`EgresoListaDto`, `EgresoFormDto`, `FiltroEgresos`, `CategoriaEgresoDto`, …).

Los egresos se editan y se eliminan (CRUD simple): no son un rastro auditable como un
movimiento de inventario, sino un registro de gasto que el comerciante puede corregir.

---

## 6. Infraestructura + migración

- Configuración EF de las dos entidades + `DbSet` en `MiniErpDbContext`.
- Repositorios en Infrastructure.
- **Siembra del starter** en `DatabaseInitializer`: Alquiler, Luz, Agua, Sueldos, Transporte
  (idempotente, editable).
- Migración `AgregarFinanzasEgreso`. Independiente de otros módulos; solo requiere traer `main`
  antes de crearla (los tres archivos compartidos: DbContext, DI, migraciones).

---

## 7. Web (`Components/Pages/Finanzas`)

- **Registrar egreso**: formulario con fecha, categoría (desplegable), monto y nota.
- **Lista de egresos**: filtro por rango de fechas y categoría, con el total del período.
- **Administrar categorías de egreso**: alta, edición y activar/desactivar.

---

## 8. Cómo alimenta los reportes siguientes

- **Estado de resultados (#4):** `Ingresos − costo de lo vendido − SUM(Egreso.Monto)` del
  período.
- **Flujo de caja (#5):** resta los egresos pagados (que son todos, por la decisión de "pagado
  al registrar").
- Ambos pueden **agrupar por `CategoriaEgreso`** para el desglose.

---

## 9. Criterio de "terminado"

Se registra un gasto operativo con categoría y fecha (nunca compras ni pagos a proveedor); se
lista filtrado por rango con su total; y las categorías se administran. Con su sección del
Capítulo IV escrita la misma semana y sus capturas.

## 10. Notas para el Capítulo IV

Documentar: por qué la categoría es una entidad y no un enum (extensible + agrupa reportes) ·
por qué un solo `Monto` sin ITBIS (fuera de alcance fiscal) · por qué pagado al registrar (el
comercio no lleva cuentas por pagar de gastos) · por qué un pago a proveedor NO es un egreso
(la trampa del doble conteo del costo) · por qué el egreso no toca inventario ni balances.
