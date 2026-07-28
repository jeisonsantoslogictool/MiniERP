# Módulo de compras

---

## 4.x.1 Qué resuelve y qué problema atacaba

El módulo de compras gobierna la **entrada de mercancía** al comercio: a quién se le compra,
qué se pidió, qué se recibió, a qué costo, cuánto se le debe al proveedor y qué se le devuelve
cuando algo llega mal. Es el eslabón que alimenta el inventario y el que fija el costo con el
que después se calcula toda la rentabilidad del negocio.

En el minimarket piloto, antes del sistema, la compra se controlaba con la factura del
proveedor y la memoria del dueño. De ahí salían tres carencias que este módulo resuelve. La
primera es que **no se conocía el costo real de la mercancía**: al mezclarse compras de
distintos precios, el comerciante fijaba precios de venta sobre un costo aproximado, que es
justamente la dificultad que señala el planteamiento del problema al hablar de no conocer *los
costos reales de adquisición*. La segunda es que **la deuda con los proveedores no tenía
respaldo**: se sabía a quién se le debía, pero no desde cuándo ni cuánto exactamente. Y la
tercera es que **la mercancía devuelta no se descontaba**, de modo que el inventario mostraba
existencias que ya no estaban en el estante.

## 4.x.2 Decisiones de diseño y su justificación

### Decisión 1 — La compra nace como orden y se congela al recibirse

Una compra tiene dos momentos que el sistema distingue con un estado: **borrador**, mientras es
una orden que todavía se puede modificar, y **recibida**, cuando la mercancía llegó. Al
recibirse, el documento queda congelado y ya no admite cambios.

La separación no es burocrática: **solo al recibir se mueve el inventario**. Mientras la orden
está en borrador, el comerciante puede corregir cantidades y costos sin que eso altere las
existencias ni el costo de los productos. Si ambas cosas ocurrieran a la vez, cualquier
corrección de una orden pendiente movería el stock de mercancía que aún no ha llegado.

### Decisión 2 — Costo promedio ponderado, no último costo

Es la decisión más importante del módulo. Al recibir mercancía, el costo del producto se
recalcula como **promedio ponderado** entre lo que ya había y lo que entra:

```
costo = (existencia × costo_actual + cantidad_recibida × costo_recibido)
        ÷ (existencia + cantidad_recibida)
```

La alternativa habitual —tomar el **último costo**— es más simple pero distorsiona el margen.
Si el comercio compra diez unidades a 30 y luego diez a 40, con último costo las **veinte**
quedarían valoradas a 40: eso infla el costo del inventario y **esconde la ganancia real** que
se obtuvo en las diez primeras. Con promedio ponderado el costo queda en 35, que es lo que de
verdad costó cada unidad en el estante.

Esta elección es la que responde a la pregunta del planteamiento del problema sobre los costos
reales de adquisición, y es la base de que el reporte de rentabilidad del módulo de finanzas
sea creíble. Dos casos límite se resuelven de forma explícita: un producto **sin existencia**
(o con existencia negativa) no tiene con qué promediar y toma el costo recibido tal cual; y un
**servicio**, que no maneja inventario, tampoco promedia nada.

### Decisión 3 — El costo se actualiza antes de mover la existencia

Dentro de la recepción, el costo promedio se calcula **antes** de aplicar el movimiento de
entrada. El orden importa y no es evidente: el promedio necesita saber **cuánta existencia
había y a qué costo**, y aplicar el movimiento primero ya habría modificado esa existencia,
produciendo un promedio calculado sobre el saldo equivocado. Es un error que no lanzaría
ninguna excepción —simplemente daría un costo distinto— y por eso el orden quedó documentado en
el propio código.

### Decisión 4 — Una compra recibida no se anula: se devuelve

Anular una compra solo es posible mientras es una orden en borrador. Una vez recibida, el
intento de anulación **se rechaza con un mensaje que indica el camino correcto**: registrar una
devolución al proveedor.

La razón es de integridad histórica. Al recibirse, la compra ya movió el inventario y modificó
el costo del producto; borrarla dejaría movimientos huérfanos y un costo promedio calculado
sobre una compra que "no existe". La devolución, en cambio, **es una operación nueva** que baja
la existencia y deja su propio rastro, conservando la historia completa de lo que entró y lo
que salió.

### Decisión 5 — La devolución es un documento propio, con motivo obligatorio

La devolución al proveedor tiene su propia entidad, con encabezado, líneas, totales y un
**motivo escrito obligatorio**. Genera movimientos del tipo `DevolucionProveedor`, distinto de
un ajuste negativo, para que el historial del producto **diga por qué** bajó la existencia.

Además valida dos límites antes de tocar nada: no se puede devolver **más de lo que se compró**
en esa línea —descontando lo ya devuelto en devoluciones anteriores— ni **más de lo que hay en
existencia**. Ambas comprobaciones se hacen sobre **todas** las líneas antes de aplicar el
primer movimiento, de modo que una devolución inválida no deja el inventario a medio modificar.

### Decisión 6 — Devolver no re-promedia el costo

Al confirmar una devolución, la existencia baja pero **el costo del producto no se recalcula**.
Podría parecer natural "deshacer" el promedio, pero sería incorrecto: el costo promedio
representa lo que costó la mercancía **que quedó**, y sacar unidades del estante no cambia lo
que costaron las que permanecen. Recalcular hacia atrás introduciría un costo artificial que no
corresponde a ninguna compra real. El movimiento de devolución usa el **costo congelado de la
compra**, que es lo que efectivamente se le devuelve al proveedor.

### Decisión 7 — Se guarda el comprobante fiscal del proveedor

La compra almacena el **NCF que emite el proveedor**. Ese número es lo que sustenta ante la
Dirección General de Impuestos Internos el ITBIS que el comercio pagó. Se guarda aunque el
reporte formal de compras (606) quede fuera del alcance del proyecto, porque el dato se captura
una sola vez —al recibir la factura— y reconstruirlo después sería imposible.

### Decisión 8 — Un pago a proveedor no es un egreso

El pago al proveedor **liquida una deuda**; no es un gasto operativo. Registrarlo como egreso
contaría el costo de la mercancía dos veces —una al vender, en el costo de lo vendido, y otra
al pagar— y produciría una utilidad falsa. El gasto ocurrió cuando se **vendió** la mercancía,
no cuando se pagó la factura. Esta distinción se desarrolla en la sección del módulo de
finanzas, pero se hace cumplir aquí: el pago solo mueve el balance del proveedor y el efectivo,
nunca la utilidad.

### Resumen de decisiones

| # | Decisión | Elección | Razón corta |
|---|----------|----------|-------------|
| 1 | Ciclo | Orden en borrador, congelada al recibir | Solo al recibir se mueve el inventario |
| 2 | Costo | Promedio ponderado | Responde al costo real; el último costo esconde el margen |
| 3 | Orden interno | Promediar antes de mover | El promedio necesita el saldo previo |
| 4 | Compra recibida | No se anula: se devuelve | El inventario ya se movió; no se borra historia |
| 5 | Devolución | Documento propio con motivo | El kardex dice por qué bajó la existencia |
| 6 | Costo al devolver | No se re-promedia | Sacar unidades no cambia lo que costaron las que quedan |
| 7 | NCF del proveedor | Se guarda | Sustenta el ITBIS pagado; no se puede reconstruir |
| 8 | Pago | No es egreso | Contarlo como gasto duplica el costo |

## 4.x.3 Modelo de dominio

El dominio vive en `src/MiniERP.Domain/Compras/` y lo forman seis entidades y dos
enumeraciones de estado:

- **`Proveedor`** — quien vende mercancía al comercio: código, nombre, documento (RNC o
  cédula), contacto, días de crédito y **balance actual**, que es lo que el comercio le debe.
- **`Compra`** y **`LineaCompra`** — el documento y sus renglones. La compra guarda número,
  proveedor, fechas de emisión y recepción, NCF del proveedor, estado, condición de pago y los
  **totales congelados**. Cada línea guarda descripción, cantidad, costo unitario y tasa de
  ITBIS, también congelados al momento de comprar.
- **`DevolucionCompra`** y **`LineaDevolucionCompra`** — la devolución, con su motivo
  obligatorio, sus líneas —que apuntan a la línea de compra que devuelven— y sus totales.
- **`Pago`** — el registro inmutable de un abono al proveedor, con **balance anterior y
  resultante**, para que la deuda sea auditable en cualquier punto del tiempo.
- **`EstadoCompra`** (borrador, recibida, anulada) y **`EstadoDevolucion`** (borrador,
  confirmada).

El método `Compra.Recibir` concentra la operación crítica: valida el estado y las líneas,
calcula el nuevo costo promedio de cada producto, genera un movimiento de entrada por línea
—llamando a `Producto.AplicarMovimiento`, la única puerta del inventario— y congela el
documento.

## 4.x.4 Pruebas de dominio

El módulo se cubrió con **treinta y cinco pruebas** escritas antes del código, siguiendo la
metodología de desarrollo guiado por pruebas.

**Sobre la recepción y el costo (19 pruebas).** Que recibir suma al inventario y congela el
documento; que el movimiento generado **apunta a la compra que lo originó**; que un producto
sin existencia toma el costo recibido; que el costo **se promedia ponderando por las unidades**
y no como promedio simple de los precios; que el promedio soporta cantidades con decimales; que
un servicio no promedia nada; que una compra **no se recibe dos veces**, ni sin líneas, ni
faltando el producto de alguna línea —y que en ese caso **falla antes de tocar nada**—; que una
compra recibida **no se anula** pero una orden pendiente sí; que los totales se arman desde las
líneas mezclando renglones gravados y exentos; y que el redondeo del ITBIS es **aritmético y no
bancario**.

**Sobre la devolución (13 pruebas).** Que confirmar baja el inventario y congela el documento;
que el movimiento generado es del tipo devolución a proveedor y **usa el costo congelado de la
compra**; que **no se devuelve más de lo comprado** ni más de lo que hay en existencia; que sí
se puede devolver exactamente lo que queda pendiente; que una devolución sin motivo o sin
líneas no se confirma, ni se confirma dos veces; y que **el costo del producto no se
re-promedia al devolver**.

**Sobre el pago (3 pruebas).** Que aplicar un pago reduce el balance del proveedor y **deja
rastro** del saldo anterior y resultante; y que se rechaza un pago de monto cero o negativo, o
que exceda la deuda.

## 4.x.5 Implementación por capas

```
MiniERP.Domain/Compras          Compra, LineaCompra, Proveedor, Pago, DevolucionCompra
      ^                          y sus reglas: Recibir, Anular, Confirmar, AplicarPago.
MiniERP.Application/Compras      Servicios de compra, proveedor, pago y devolución; contratos
      ^                          de repositorio y DTOs.
MiniERP.Infrastructure          Configuración de las tablas, repositorios y migraciones.
      ^
MiniERP.Web/…/Compras           Las pantallas del módulo.
```

La capa de aplicación orquesta lo que el dominio decide: al recibir una compra, el servicio
carga los productos implicados, invoca `Compra.Recibir`, persiste los movimientos generados y
—si la compra fue a crédito— sube el balance del proveedor, **todo dentro de una sola
transacción**, para que una recepción no quede a medias. En la devolución, el servicio calcula
previamente cuánto queda por devolver de cada línea y se lo entrega al dominio, que es quien
aplica el límite.

## 4.x.6 Interfaz de usuario

Las pantallas viven en `src/MiniERP.Web/Components/Pages/Compras/`:

- **Compras** — el listado de órdenes y compras recibidas, con su estado.
- **Editor de compra** — el armado de la orden con sus líneas, y la acción de **recibir**, que
  es la que mueve el inventario.
- **Proveedores** y **editor de proveedor** — el catálogo de terceros con su crédito.
- **Registrar pago** — el abono a la deuda del proveedor.
- **Cuentas por pagar** — quién cobra, cuánto y desde hace cuántos días, usando los días de
  crédito acordados para señalar lo vencido.

## 4.x.7 Verificación

La recepción quedó demostrada dentro del recorrido integrado descrito en la sección 4.7: se
compraron **20 libras** de un producto, la existencia subió de 9.5 a **29.5** y el costo se
recalculó por promedio ponderado a **30.8729**. Ese número es la evidencia de que el promedio
ponderado opera sobre datos reales y no solo en pruebas unitarias.

## 4.x.8 Alcance y limitaciones

- **No se emite el reporte 606** de compras a la Dirección General de Impuestos Internos; el
  NCF del proveedor se captura, pero el archivo formal queda fuera del alcance.
- **No hay recepción parcial**: la compra se recibe completa. Recibir una parte hoy y el resto
  mañana exigiría un estado intermedio que la operación del piloto no requiere.
- **No hay órdenes de compra automáticas** por punto de reorden: el sistema **avisa** qué
  productos están por agotarse, pero la orden la arma el encargado.
