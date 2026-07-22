# Diseño — Reporte de rentabilidad (Finanzas #3)

- **Fecha:** 2026-07-20
- **Autor:** Samuel Sánchez · rama `samuel/finanzas`
- **Módulo:** Finanzas — Trabajo Final de Grado, Grupo 6
- **Estado:** Aprobado, pendiente de implementar

---

## 1. Qué es y para qué sirve

El reporte de rentabilidad responde: **"¿cuánto ganó el negocio, y en qué?"**. Por cada
venta, la ganancia bruta es **Subtotal − Costo de lo vendido** (sin ITBIS, que no es del
comercio). El reporte muestra ese margen en tres cortes de un mismo período: el **total**,
por **producto** y por **categoría**.

Es el **gate del módulo de Finanzas**: si se vendieron 3 LB con margen 21.38, el reporte del
día debe decir 21.38, ni un centavo de diferencia.

### La regla que hace todo esto posible: el costo congelado

El margen se calcula con **`LineaFactura.CostoUnitario`** — el costo del producto **congelado
al momento de vender**, no el costo actual. El costo del producto cambia con cada compra (por
el promedio ponderado), así que leerlo hoy daría un margen distinto al real. Congelarlo en la
línea es lo que hace que la rentabilidad de una venta pasada no se mueva cuando el costo sube.
Este reporte **nunca lee `Producto.Costo`**.

Los datos salen de las **líneas de factura** (`Domain/Ventas/LineaFactura.cs`). El reporte
**solo lee**: no crea tablas, no necesita migración.

---

## 2. Decisiones de diseño (material de defensa)

| Decisión | Elección | Razón |
|---|---|---|
| Contenido | **Tres cortes en un reporte**: total, por producto, por categoría | Es lo que pide la asignación; el comerciante ve el total y qué le deja más. |
| Dónde vive la lógica | **Calculadora de dominio con TDD** (`ResumenRentabilidad.Calcular`) | Es el gate "cuadra al centavo": una calculadora probada es la prueba automática. Reusa el patrón del reporte de ingresos. |
| Costo | **`LineaFactura.CostoUnitario` (congelado)**, proyectado en el repositorio | La calculadora nunca ve `Producto.Costo`; el margen de ayer no cambia si el costo sube. |
| Persistencia | **Solo lectura, sin migración** | Agrega sobre líneas de factura que ya existen. |
| Agrupación por categoría | **Categoría actual del producto** | La línea congela costo y descripción, no categoría; agrupar por la categoría actual es una clasificación aceptable. |
| Orden de los cortes | **Por margen descendente** | Lo primero que interesa es qué deja más. |

---

## 3. Modelo de dominio (`MiniERP.Domain.Finanzas`)

Objetos de valor con la fábrica que hace las cuentas. No hay entidad persistida.

```csharp
public record ResumenRentabilidad(
    RentabilidadTotal Total,
    IReadOnlyList<RentabilidadPorItem> PorProducto,
    IReadOnlyList<RentabilidadPorItem> PorCategoria)
{
    public static ResumenRentabilidad Calcular(IEnumerable<RenglonVendido> renglones);
}

public record RentabilidadTotal(
    decimal Subtotal, decimal Costo, decimal Margen, decimal MargenPorcentaje);

public record RentabilidadPorItem(
    int Id, string Nombre, decimal Subtotal, decimal Costo, decimal Margen, decimal MargenPorcentaje);

/// <summary>Un renglon vendido, tal como lo proyecta el repositorio desde LineaFactura.</summary>
public record RenglonVendido(
    bool FacturaAnulada,
    int ProductoId, string Producto,
    int CategoriaId, string Categoria,
    decimal Subtotal, decimal Costo);
```

`Calcular` **descarta los renglones de facturas anuladas**, calcula el total, y agrupa por
producto y por categoría. Para cada grupo: `Subtotal` y `Costo` se suman con
`RetailConstants.RedondearImporte`; `Margen = Subtotal − Costo`; `MargenPorcentaje =
Subtotal == 0 ? 0 : Margen / Subtotal` (una fracción, p. ej. 0.40; la pantalla la formatea
como 40 %). Los cortes van ordenados por margen descendente.

---

## 4. Pruebas de dominio (TDD, se escriben primero) — `ResumenRentabilidadTests`

1. Un período **vacío** da ceros y listas vacías.
2. El **margen es subtotal − costo** (subtotal 100, costo 60 → margen 40).
3. El **margen % es sobre el subtotal** (40 / 100 = 0.40).
4. **Agrupa por producto** (dos renglones del mismo producto se suman).
5. **Agrupa por categoría** (renglones de distintos productos de una categoría se suman).
6. **Excluye las anuladas** (un renglón de factura anulada no cuenta).
7. **Ordena por margen descendente** los cortes.
8. El **total** es la suma de todos los renglones no anulados.

En verde = la lógica del gate queda probada, sin base de datos.

---

## 5. Capa de aplicación (`MiniERP.Application.Finanzas`)

- **`IReporteRentabilidadService`**: `ObtenerAsync(FiltroPeriodo filtro)` → `ResumenRentabilidad`.
  Pide los renglones del período al repositorio y los pasa a `ResumenRentabilidad.Calcular`.
- **`IReporteRentabilidadRepositorio`**: `ObtenerRenglonesDelPeriodoAsync(DateTime desde, DateTime hasta)`
  → `IReadOnlyList<RenglonVendido>`.
- **`FiltroPeriodo`**: `record` con `Desde` y `Hasta`, reusable por los reportes que vienen.

El servicio devuelve el objeto de dominio `ResumenRentabilidad` directamente (valor puro).

---

## 6. Infraestructura

- **`ReporteRentabilidadRepositorio`**: proyecta desde `LineasFactura`, uniendo a `Factura`
  (para la fecha y el estado) y a `Producto`→`Categoria` (para los nombres):
  - Filtro: `l.Factura.Fecha >= desde && l.Factura.Fecha < hasta.Date.AddDays(1)`.
  - `FacturaAnulada = l.Factura.Estado == EstadoFactura.Anulada`.
  - `Subtotal = l.Cantidad * l.PrecioUnitario`; **`Costo = l.Cantidad * l.CostoUnitario`**
    (el costo **congelado** de la línea, la regla del gate).
  - `AsNoTracking`.
- Registro del servicio y el repositorio en `DependencyInjection` (partial de Finanzas).
- **Sin migración.**

---

## 7. Web (`Components/Pages/Finanzas`)

- **Rentabilidad** (`/finanzas/rentabilidad`): selector de **Desde/Hasta** (mes en curso por
  defecto) y tres secciones:
  1. **Total del período**: subtotal, costo, margen y margen %.
  2. **Tabla por producto**: nombre, subtotal, costo, margen, margen %.
  3. **Tabla por categoría**: nombre, subtotal, costo, margen, margen %.
- Enlace desde la portada de Finanzas.

---

## 8. Relación con los reportes siguientes

- El **costo de lo vendido** que aquí se totaliza es el que resta el **estado de resultados**
  (#4): Ingresos − costo de lo vendido − egresos = utilidad.
- La calculadora reusa el patrón probado del reporte de ingresos.

---

## 9. Alcance y limitaciones

- **Solo lectura**, sin persistencia.
- **Categoría actual** del producto para la agrupación (la línea no congela la categoría).
- **Zona horaria:** las fechas de las facturas se guardan en UTC; el desfase en el borde de
  un mes es una imprecisión menor, aceptable para el piloto.

## 10. Notas para el Capítulo IV

Documentar: por qué el margen usa el **costo congelado de la línea** y nunca `Producto.Costo`
(el margen de ayer no cambia si el costo sube) · por qué se excluyen las anuladas · por qué la
lógica va en una calculadora de dominio probada (el gate "cuadra al centavo") · los tres
cortes (total, producto, categoría) · por qué no necesita migración.
