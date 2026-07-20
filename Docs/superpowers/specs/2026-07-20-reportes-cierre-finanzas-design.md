# Diseño — Reportes de cierre de Finanzas (#4 Estado de resultados · #5 Flujo de caja · #6 Panel)

- **Fecha:** 2026-07-20
- **Autor:** Samuel Sánchez · rama `samuel/finanzas`
- **Módulo:** Finanzas — Trabajo Final de Grado, Grupo 6
- **Estado:** Aprobado, pendiente de implementar

> Las tres últimas piezas del módulo de Finanzas. **Ensamblan** lo ya construido (egresos,
> ingresos, rentabilidad) más los cobros y pagos de Dionis (ya en `main`). Ninguna crea
> tablas ni necesita migración: todas **solo leen**.

---

## 1. Propósito y las dos vistas que el anteproyecto pide por separado

- **Estado de resultados** (base devengado): cuánto **gana** el negocio. Reconoce el ingreso
  al facturar y el costo al vender. **No** entran cobros ni pagos, ni el ITBIS.
- **Flujo de caja** (base efectivo): cuánto **efectivo** entra y sale de la gaveta. Entran las
  ventas de contado y los cobros; salen los egresos y los pagos. **No** entran las ventas a
  crédito aún no cobradas.
- **Panel**: lo primero que el dueño mira al llegar — los números del día y del mes.

Un negocio puede tener utilidad y no tener efectivo (vendió fiado), o al revés. Por eso son
dos reportes distintos.

---

## 2. #4 — Estado de resultados

Fórmula (`CLAUDE.md`):

```
  Ingresos            Factura.Subtotal del período (sin ITBIS), sin anuladas
− Costo de lo vendido Factura.CostoTotal (congelado al vender)
= Margen bruto
− Egresos             Egreso del período
= Utilidad
```

### Dominio
```csharp
public record EstadoResultados(decimal Ingresos, decimal CostoVendido, decimal Egresos)
{
    public decimal MargenBruto => Ingresos - CostoVendido;
    public decimal Utilidad => MargenBruto - Egresos;
}
```
El tipo **no acepta pagos ni cobros** como parámetro: la trampa "un pago a proveedor no es
un egreso" queda imposible por construcción.

### Pruebas de dominio
1. `MargenBruto` = Ingresos − CostoVendido.
2. `Utilidad` = Ingresos − CostoVendido − Egresos.
3. La utilidad puede ser **negativa** (vender bajo costo o con muchos egresos).

### Aplicación (reusa lo existente)
`IEstadoResultadosService.ObtenerAsync(FiltroPeriodo)` compone:
- **Ingresos** = `ResumenIngresos.Subtotal` (de `IReporteIngresosService`, contado + crédito).
- **CostoVendido** = `ResumenRentabilidad.Total.Costo` (de `IReporteRentabilidadService`).
- **Egresos** = `IEgresoService.ObtenerTotalAsync`.
- Devuelve `new EstadoResultados(ingresos, costoVendido, egresos)`.

### Web
`/finanzas/estado-resultados`: selector de fechas y el estado en cascada (Ingresos, − Costo,
= Margen bruto, − Egresos, = **Utilidad**).

### Gate
Registrar un **pago a proveedor** y comprobar que la **utilidad NO se mueve** (los pagos no
entran en la fórmula).

---

## 3. #5 — Flujo de caja

Fórmula (`CLAUDE.md`):

```
+ Ventas de contado   Factura.Total de las ventas de contado (efectivo que entró)
+ Cobros a clientes   Cobro.Monto del período
− Egresos             Egreso del período
− Pagos a proveedor   Pago.Monto del período
= Efectivo neto
```

### Dominio
```csharp
public record FlujoCaja(decimal VentasContado, decimal Cobros, decimal Egresos, decimal Pagos)
{
    public decimal Entradas => VentasContado + Cobros;
    public decimal Salidas => Egresos + Pagos;
    public decimal EfectivoNeto => Entradas - Salidas;
}
```

### Pruebas de dominio
1. `Entradas` = VentasContado + Cobros.
2. `Salidas` = Egresos + Pagos.
3. `EfectivoNeto` = Entradas − Salidas (puede ser negativo).

### Aplicación
`IFlujoCajaService.ObtenerAsync(FiltroPeriodo)` compone:
- **VentasContado** = `ResumenIngresos.Contado.Total` (con ITBIS: es el efectivo real recibido).
- **Cobros** = suma de `Cobro.Monto` del período.
- **Egresos** = `IEgresoService.ObtenerTotalAsync`.
- **Pagos** = suma de `Pago.Monto` del período.

### Infraestructura
`IFlujoCajaRepositorio` con `SumarCobrosAsync(desde, hasta)` y `SumarPagosAsync(desde, hasta)`,
que suman `contexto.Cobros`/`contexto.Pagos` en el rango (Finanzas lee los módulos de Dionis;
no los modifica). **Sin migración.**

### Web
`/finanzas/flujo-caja`: selector de fechas y el flujo (+ contado, + cobros, − egresos,
− pagos, = **Efectivo neto**).

---

## 4. #6 — Panel de finanzas

Lo primero que el dueño ve. Va en la **portada `/finanzas`**, arriba de los enlaces a los
reportes. **No lleva dominio nuevo**: ensambla los servicios anteriores para rangos fijos.

- **Ventas del día** = ingresos de hoy (total).
- **Margen del día** = rentabilidad de hoy (margen).
- **Egresos del mes** = egresos del mes.
- **Utilidad del mes** = estado de resultados del mes.
- **Efectivo del mes** = flujo de caja del mes.

### Aplicación
`IPanelFinanzasService.ObtenerAsync()` → `PanelFinanzasDto(VentasDia, MargenDia, EgresosMes,
UtilidadMes, EfectivoMes)`, calculando "hoy" y "el mes en curso" con los servicios existentes.

### Web
La portada `/finanzas` muestra las cinco cifras en tarjetas (KPIs), y debajo, los enlaces a
los reportes (Egresos, Ingresos, Rentabilidad, Estado de resultados, Flujo de caja).

---

## 5. Decisiones y alcance

| Decisión | Elección | Razón |
|---|---|---|
| Dominio | Records con propiedades calculadas, TDD | Las fórmulas quedan probadas; el estado de resultados no puede recibir pagos. |
| Ensamblaje | Los servicios **reutilizan** ingresos/rentabilidad/egresos | DRY; casi no hay lógica nueva de datos. |
| Cobros/pagos | Se **leen** de las tablas de Dionis, no se modifican | Finanzas es la capa analítica que lee todo. |
| Persistencia | **Solo lectura, sin migración** | Ninguno crea tablas. |

- **Zona horaria:** las fechas se comparan en UTC; el desfase en bordes de día/mes es una
  imprecisión menor, aceptable para el piloto.

## 6. Notas para el Capítulo IV

Documentar: por qué estado de resultados y flujo de caja son **dos reportes distintos**
(ganancia vs. efectivo) · por qué los **pagos y cobros NO entran** en el estado de resultados
(el costo ya se contó al vender) · por qué el estado de resultados **no puede** contar un pago
como egreso (el tipo no lo acepta) · el panel como ensamblaje de todo lo anterior.
