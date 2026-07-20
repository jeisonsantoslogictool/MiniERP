# Capítulo IV — Reportes de Finanzas

Trabajo Final de Grado · Grupo 6 · Universidad Dominicana O&M
Autor del módulo: **Samuel Sánchez** · rama `samuel/finanzas` · construido del 2026-07-19 al
2026-07-20.

> Esta sección documenta las cinco piezas de **reportes** del módulo de Finanzas: reporte de
> ingresos, reporte de rentabilidad, estado de resultados, flujo de caja y panel. (El
> **registro de egresos**, la sexta pieza, se documenta en `Docs/CapituloIV/Egresos.md`.)
> Los diseños validados y los planes de implementación viven en `Docs/superpowers/specs/` y
> `Docs/superpowers/plans/`.

---

## 4.x.1 Un patrón común para los cinco reportes

Los cinco reportes se construyeron sobre el mismo patrón, en cuatro capas:

1. **Una "calculadora de dominio"** (C# puro, en `MiniERP.Domain/Finanzas/`): un objeto de
   valor que recibe los datos crudos y aplica la fórmula. No conoce la base de datos ni la
   pantalla, así que se prueba con **TDD** sin levantar nada.
2. **Un servicio de aplicación** que reúne los datos y llama a la calculadora.
3. **Un repositorio de solo lectura** que consulta la base.
4. **Una pantalla Blazor**.

**Por qué la calculadora de dominio, y por qué importa:** el criterio de aceptación del
módulo —el *gate*— es que los reportes **cuadren al centavo** contra las ventas reales. Al
poner la fórmula en una calculadora pura y probarla con TDD, se obtiene una **prueba
automática** de que cuadra. En la defensa no se dice "confíen en el número"; se muestra la
prueba en verde. Ese mismo patrón, validado primero con el reporte de ingresos (el más
simple), se reutilizó en los reportes donde la aritmética es más delicada.

**Todos los reportes son de solo lectura:** ninguno crea tablas ni necesita migración. Leen
las facturas que emite el POS, los egresos, y los cobros y pagos del módulo de terceros.
Finanzas es la **capa analítica** del sistema: lee todos los módulos y no modifica ninguno.

Al cierre, la suite del sistema tenía **144 pruebas en verde** y compilaba sin advertencias.

---

## 4.x.2 El escenario de verificación: una venta real

Los cinco reportes se verificaron **en ejecución, en el navegador**, contra una misma venta
real registrada con el punto de venta (módulo de Jeison, ya integrado a `main`):

| Dato de la venta | Valor |
|---|---|
| Producto | Precio RD$ 100, costo RD$ 60, ITBIS 18 % |
| Cantidad y condición | 1 unidad, de **contado** |
| Comprobante | NCF **B0200000001** |
| Subtotal (sin ITBIS) | RD$ 100.00 |
| ITBIS | RD$ 18.00 |
| Total (efectivo recibido menos cambio) | RD$ 118.00 |
| Costo de lo vendido (congelado) | RD$ 60.00 |
| Margen | RD$ 40.00 (40 %) |

Esta única venta es el ancla de toda la verificación: cada reporte debe mostrar exactamente
la parte que le corresponde de estos números.

---

## 4.x.3 Reporte de ingresos (Finanzas #2)

### Qué es
Responde *"¿cuánto vendió el negocio en un período?"*, separando **ventas de contado de
ventas a crédito** y mostrando el **ITBIS aparte**, sin las facturas anuladas.

### Decisiones y su porqué
- **Se separan contado y crédito** porque no son lo mismo: la venta de contado es dinero en
  la mano; la de crédito es una venta que todavía no se ha cobrado.
- **El ITBIS va en su propia columna** porque no es del comercio: lo recauda y lo entrega a
  la DGII. El ingreso "de verdad" es el **subtotal** (venta sin ITBIS).
- **Se excluyen las facturas anuladas** porque una venta anulada no es un ingreso. La
  calculadora `ResumenIngresos.Calcular` descarta las anuladas y agrupa por condición de
  pago; está probada con seis pruebas de dominio.

### Verificación
La pantalla `/finanzas/ingresos` mostró, para la venta real: **Contado — 1 factura, subtotal
RD$ 100.00, ITBIS RD$ 18.00, total RD$ 118.00**; crédito en cero. Cuadró al centavo.
(Captura: `ingresos-con-venta-real.png`.)

---

## 4.x.4 Reporte de rentabilidad (Finanzas #3) — el gate

### Qué es
Responde *"¿cuánto ganó el negocio, y en qué?"*. Muestra el **margen** (Subtotal − Costo de
lo vendido, sin ITBIS) en tres cortes: **total**, **por producto** y **por categoría**.

### La regla que sostiene el gate: el costo congelado
El margen se calcula con **`LineaFactura.CostoUnitario`** — el costo **congelado al momento
de vender**, no el costo actual del producto. El costo del producto cambia con cada compra
(por el promedio ponderado); leerlo hoy daría un margen distinto al real. Congelarlo en la
línea es lo que hace que la rentabilidad de una venta pasada **no se mueva** cuando el costo
sube después. El repositorio proyecta ese costo directo de la línea (`Cantidad *
CostoUnitario`); la calculadora **nunca ve `Producto.Costo`**.

### Decisiones y su porqué
- **Tres cortes en un mismo reporte** porque el comerciante quiere ver el total y, además,
  qué productos y categorías le dejan más.
- **Ordenado por margen descendente**: lo primero que interesa es qué deja más.
- **Agrupación por la categoría actual** del producto (la línea congela el costo y la
  descripción, no la categoría): es una clasificación, no un valor del documento.

### Verificación del gate
La pantalla `/finanzas/rentabilidad` dio, para la venta real, **margen RD$ 40.00 (40 %)
exacto** en los tres cortes (total, producto "Producto de prueba", categoría "Víveres"), con
el costo de RD$ 60.00 tomado del congelado de la línea. **El gate quedó demostrado contra una
venta de verdad.** (Captura: `rentabilidad-gate-cuadra.png`.)

---

## 4.x.5 Estado de resultados (Finanzas #4)

### Qué es
Responde *"¿cuánto GANA el negocio?"* (base devengado):

```
  Ingresos            Subtotal de facturas (sin ITBIS)
− Costo de lo vendido CostoTotal congelado de facturas
= Margen bruto
− Egresos
= Utilidad
```

### La trampa evitada por construcción
La decisión más importante de defensa: el tipo de dominio
`EstadoResultados(Ingresos, CostoVendido, Egresos)` **no tiene un parámetro para pagos ni
cobros**. Un pago a proveedor no es un gasto —el costo de la mercancía ya se contó al
vender—, y aquí **no puede entrar ni por error**: el compilador no lo permite. Es una
garantía más fuerte que una prueba: es imposible por diseño. (El ITBIS tampoco entra: es de
la DGII.)

### Verificación
La pantalla `/finanzas/estado-resultados` mostró, en cascada: Ingresos RD$ 100.00 − Costo
RD$ 60.00 = Margen bruto RD$ 40.00 − Egresos RD$ 0.00 = **Utilidad RD$ 40.00**. Cuadró.

El *gate* del anteproyecto ("registrar un pago a proveedor y comprobar que la utilidad no se
mueve") queda garantizado por construcción y por las pruebas de dominio: como el tipo no
admite pagos, ningún pago puede alterar la utilidad.

---

## 4.x.6 Flujo de caja (Finanzas #5)

### Qué es
Responde *"¿cuánto EFECTIVO entra y sale de la gaveta?"* (base efectivo):

```
+ Ventas de contado   (efectivo que entró, con ITBIS incluido)
+ Cobros a clientes
− Egresos
− Pagos a proveedor
= Efectivo neto
```

### Por qué es un reporte distinto del estado de resultados
El anteproyecto pide los dos por separado, y con razón: **uno mide ganancia, el otro mide
efectivo.** Un negocio puede tener utilidad y no tener efectivo (vendió fiado), o al revés.
El flujo de caja **no incluye** las ventas a crédito aún no cobradas: se venderá, pero
todavía no es dinero en la gaveta. Sí incluye el ITBIS de las ventas de contado, porque es
efectivo real que entró.

### Verificación y el contraste que lo hace evidente
La pantalla `/finanzas/flujo-caja` mostró: Ventas de contado RD$ 118.00 + Cobros RD$ 0.00 =
Entradas RD$ 118.00 − Egresos − Pagos = **Efectivo neto RD$ 118.00**.

La misma venta produjo **utilidad RD$ 40.00 (estado de resultados) y efectivo RD$ 118.00
(flujo de caja)** — números distintos a propósito. El ITBIS (RD$ 18.00) es efectivo que
entró pero no es ganancia; el margen (RD$ 40.00) es ganancia pero no todo entró como efectivo
nuevo. Es la demostración directa de por qué los dos reportes existen por separado. (Captura:
`flujo-caja-verificacion.png`.)

---

## 4.x.7 Panel de finanzas (Finanzas #6)

### Qué es
Lo primero que el dueño ve al entrar al módulo (la portada `/finanzas`): cinco cifras clave
—ventas del día, margen del día, egresos del mes, utilidad del mes y efectivo del mes— sobre
tarjetas, con los enlaces a los reportes debajo.

### Decisión de diseño
El panel **no lleva lógica nueva**: **ensambla** los servicios de los reportes anteriores
para rangos fijos (hoy, mes en curso). Reusar en vez de reescribir mantiene una sola fuente
de verdad para cada número.

### Verificación
La portada cargó los cinco servicios de un solo golpe y mostró: Ventas del día RD$ 118.00 ·
Margen del día RD$ 40.00 · Egresos del mes RD$ 0.00 · Utilidad del mes RD$ 40.00 · Efectivo
del mes RD$ 118.00. Todos cuadraron con la venta real.

---

## 4.x.8 Verificación de conjunto y trabajo en equipo

La verificación de estos reportes fue también la prueba de la **integración del equipo**: la
factura que alimenta los reportes la creó el **punto de venta de Jeison**, y el flujo de caja
lee los **cobros y pagos de Dionis** — todo sobre `main` integrado. Una sola venta real
recorrió el sistema completo (inventario → POS → factura) y se reflejó, correcta, en los
cinco reportes de Finanzas. Es, literalmente, la integración que promete el anteproyecto.

---

## 4.x.9 Decisiones transversales (resumen para la defensa)

| Decisión | Porqué |
|---|---|
| Calculadora de dominio + TDD por reporte | La prueba automática es la evidencia del gate "cuadra al centavo". |
| Reportes de solo lectura, sin migración | Finanzas es la capa analítica: lee, no modifica. |
| El estado de resultados no admite pagos por tipo | Hace imposible la trampa "un pago es un egreso". |
| Costo congelado (`LineaFactura.CostoUnitario`) en rentabilidad | El margen de ayer no cambia si el costo sube. |
| Estado de resultados y flujo de caja separados | Ganancia ≠ efectivo; el anteproyecto pide ambos. |
| El panel ensambla, no recalcula | Una sola fuente de verdad por cifra. |

### Limitación conocida
Las fechas de las facturas se guardan en horario universal (UTC); en el borde exacto de un
día o un mes, el desfase de UTC−4 podría contar una venta de la noche en el período vecino.
Es una imprecisión menor, aceptable para el piloto; se afinaría convirtiendo el rango local a
límites UTC si hiciera falta.

---

*Capturas de la verificación (con datos reales):* `ingresos-con-venta-real.png` ·
`rentabilidad-gate-cuadra.png` · `flujo-caja-verificacion.png` · y el panel de la portada.
