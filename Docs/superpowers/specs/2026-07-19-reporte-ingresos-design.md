# Diseño — Reporte de ingresos (Finanzas #2)

- **Fecha:** 2026-07-19
- **Autor:** Samuel Sánchez · rama `samuel/finanzas`
- **Módulo:** Finanzas — Trabajo Final de Grado, Grupo 6
- **Estado:** Aprobado, pendiente de implementar

---

## 1. Qué es y para qué sirve

El reporte de ingresos responde: **"¿cuánto vendió el negocio en un período?"**, contado
de forma que el comerciante entienda el número. Separa tres cosas que no son iguales:

- **Ventas de contado vs. ventas a crédito.** Vender al contado es dinero en la mano;
  vender a crédito es una venta que aún no se ha cobrado. Se muestran por separado.
- **El ITBIS aparte.** El impuesto que el cliente paga no es del comercio: lo recauda y lo
  entrega a la DGII. El ingreso "de verdad" es el **subtotal** (venta sin ITBIS); el ITBIS
  va en su propia columna.
- **Sin las facturas anuladas.** Una venta anulada no es un ingreso; se excluye.

Los datos salen de las **facturas** que emite el POS (`Domain/Ventas/Factura.cs`), que ya
guardan `Subtotal`, `Itbis`, `Total`, `Condicion` (contado/crédito) y `Estado`
(emitida/anulada). Este reporte **solo lee**: no crea ninguna tabla, no necesita migración.

---

## 2. Decisiones de diseño (material de defensa)

| Decisión | Elección | Razón |
|---|---|---|
| Contenido | **Resumen del período** (contado/crédito, subtotal/ITBIS/total, cantidad) | Es lo que pide la asignación y lo que el comerciante mira de un vistazo. La lista de facturas queda para un incremento posterior. |
| Dónde vive la lógica | **Calculadora de dominio con TDD** (`ResumenIngresos.Calcular`) | El gate exige que el reporte "cuadre al centavo"; una calculadora probada es la prueba automática de que cuadra. Sienta el patrón para los reportes más complejos que siguen. |
| Persistencia | **Solo lectura, sin migración** | El reporte agrega sobre `Factura`, que ya existe. No añade estado. |
| Rango de fechas | **`Hasta` inclusivo del día completo** | Si se elige "hasta el 31", entran las ventas del 31 a cualquier hora, no solo hasta la medianoche. |

---

## 3. Modelo de dominio (`MiniERP.Domain.Finanzas`)

Un objeto de valor con la fábrica que hace las cuentas. No hay entidad persistida: es un
resultado calculado, no un registro.

```csharp
public record ResumenIngresos(
    ResumenIngresosPorCondicion Contado,
    ResumenIngresosPorCondicion Credito)
{
    public int Cantidad => Contado.Cantidad + Credito.Cantidad;
    public decimal Subtotal => Contado.Subtotal + Credito.Subtotal;
    public decimal Itbis => Contado.Itbis + Credito.Itbis;
    public decimal Total => Contado.Total + Credito.Total;

    public static ResumenIngresos Calcular(IEnumerable<Factura> facturas);
}

public record ResumenIngresosPorCondicion(int Cantidad, decimal Subtotal, decimal Itbis, decimal Total);
```

`Calcular` recibe las facturas del período (emitidas y anuladas), **descarta las anuladas**
(`f.EstaAnulada`), **separa** por `Condicion` (contado/crédito) y **suma** `Subtotal`,
`Itbis` y `Total` de cada grupo con `RetailConstants.RedondearImporte`, contando cuántas
facturas hay en cada uno. Recibe la entidad real `Factura`, no una copia: el reporte lee
las facturas de verdad.

---

## 4. Pruebas de dominio (TDD, se escriben primero) — `ResumenIngresosTests`

1. Un período **vacío** da ceros en todo.
2. **Separa** contado de crédito (cada bloque con lo suyo).
3. **Excluye las anuladas**: una factura anulada no suma ni cuenta.
4. **Suma** correctamente subtotal, ITBIS y total.
5. **Cuenta** las facturas por condición.
6. Los **totales generales** son la suma de contado y crédito.

En verde = la lógica del reporte queda probada y defendible, sin base de datos.

---

## 5. Capa de aplicación (`MiniERP.Application.Finanzas`)

- **`IReporteIngresosService`**: `ObtenerAsync(FiltroReporteIngresos filtro)` → `ResumenIngresos`.
  Pide las facturas del período al repositorio y se las pasa a `ResumenIngresos.Calcular`.
- **`IReporteIngresosRepositorio`**: `ObtenerFacturasDelPeriodoAsync(DateTime desde, DateTime hasta)`
  → `IReadOnlyList<Factura>` (encabezados del período, sin líneas).
- **`FiltroReporteIngresos`**: `record` con `Desde` y `Hasta` (fechas). La pantalla lo
  inicializa al mes en curso.

El servicio devuelve el objeto de dominio `ResumenIngresos` directamente: es un valor puro
(solo números), sin comportamiento que filtrar, así que no hace falta un DTO redundante.

---

## 6. Infraestructura

- **`ReporteIngresosRepositorio`**: `contexto.Facturas.AsNoTracking().Where(f => f.Fecha >= desde && f.Fecha < finDelDia).ToListAsync()`,
  donde `finDelDia = hasta.Date.AddDays(1)` (para que `Hasta` incluya el día completo).
  Trae los encabezados (sin `Include` de líneas: este reporte solo necesita los totales).
- Registro del servicio y el repositorio en `DependencyInjection`.
- **Sin migración**: no se crea ninguna tabla.

---

## 7. Web (`Components/Pages/Finanzas`)

- **Reporte de ingresos** (`/finanzas/ingresos`): selector de **Desde/Hasta** (por defecto,
  el mes en curso) y una **tabla-resumen** con filas **Contado**, **Crédito** y **Total
  general**, y columnas **Cantidad**, **Subtotal**, **ITBIS**, **Total**.
- Enlace desde la portada de Finanzas (`/finanzas`).

---

## 8. Relación con los reportes siguientes

- El **subtotal** de este reporte (venta sin ITBIS) es el "Ingresos" del **estado de
  resultados** (#4).
- Las **ventas de contado** de aquí alimentan el **flujo de caja** (#5).
- La **calculadora de dominio probada** es el patrón que reusan la **rentabilidad** (#3) y
  el **estado de resultados** (#4), donde la matemática es más delicada.

---

## 9. Alcance y limitaciones

- **Solo resumen**, no lista de facturas ni desglose por día (quedan para incrementos
  posteriores si se piden).
- **Zona horaria:** las fechas de las facturas se guardan en horario universal (UTC). En el
  borde exacto de un mes, una venta de la noche podría contar en el mes vecino por el
  desfase de UTC−4. Es una imprecisión menor, aceptable para el piloto; se afinaría
  convirtiendo el rango local a límites UTC si hiciera falta.

## 10. Notas para el Capítulo IV

Documentar: por qué se separan contado y crédito (dinero en mano vs. venta no cobrada) · por
qué el ITBIS va aparte (no es del comercio) · por qué se excluyen las anuladas · por qué la
lógica va en una calculadora de dominio probada (el gate "cuadra al centavo") · por qué este
reporte no necesita migración (solo lee facturas).
