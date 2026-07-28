# Integración de los módulos

---

## 4.x.1 Qué significa aquí "integración"

El anteproyecto no promete cinco programas que se instalan juntos, sino **un sistema
integrado**: que la mercancía que entra por una compra sea la misma que sale por una venta, y
que ambas alimenten los reportes financieros sin que nadie vuelva a teclear un dato. Esta
sección documenta cómo se logró ese encadenamiento y con qué evidencia se comprobó.

La integración no se resolvió con un módulo coordinador ni con un bus de eventos —maquinaria
que un sistema de este tamaño no necesita—, sino con tres mecanismos simples: **una única
puerta para cada dato compartido**, **valores congelados que convierten un dato vivo en un
hecho histórico**, y **transacciones que garantizan que las operaciones encadenadas ocurran
completas o no ocurran**.

## 4.x.2 La cadena operativa

El flujo que atraviesa los cinco módulos es el siguiente:

Se registra una **compra** a un proveedor y, al recibirse la mercancía, el sistema genera un
movimiento de entrada por cada línea, **actualiza el costo del producto por promedio
ponderado** y congela el documento. Si la compra fue a crédito, sube el balance por pagar del
proveedor. La existencia del **inventario** queda modificada únicamente por ese movimiento, que
guarda la cantidad anterior y la resultante.

Cuando el cajero **vende**, el sistema valida la existencia, asigna de forma atómica el
siguiente comprobante fiscal, **congela el costo de cada línea**, descuenta el inventario y
emite la factura, todo dentro de una sola transacción. Si la venta fue a crédito, sube el
balance del cliente; si fue de contado, calcula el cambio.

Las deudas generadas se saldan con **cobros** (del cliente) y **pagos** (al proveedor), que
bajan los balances dejando rastro del saldo anterior y el resultante. Finalmente, el módulo de
**finanzas** lee todo lo anterior —facturas, costos congelados, egresos, cobros y pagos— y
produce el estado de resultados y el flujo de caja **sin modificar nada**.

## 4.x.3 Los contratos entre módulos

La integración descansa en cuatro acuerdos que ningún módulo puede violar, y que están fijados
por pruebas automatizadas:

- **La existencia solo cambia por `Producto.AplicarMovimiento`.** Ni la venta ni la compra
  tocan la cantidad directamente: ambas piden al módulo de inventario que aplique un
  movimiento, y ese método rechaza cualquier operación que dejaría la existencia en negativo.
  Es una sola puerta, y por eso el saldo siempre es auditable.
- **El costo de lo vendido se congela en `LineaFactura.CostoUnitario`.** Los reportes de
  rentabilidad usan ese valor y **nunca** el costo actual del producto. Sin esta regla, una
  compra posterior movería retroactivamente el margen de ventas ya realizadas y los resultados
  del capítulo siguiente cambiarían solos.
- **Un pago a proveedor no es un egreso.** Es la distinción más delicada del sistema y no
  produce ningún error visible: registrar los pagos como gastos contaría el costo de la
  mercancía dos veces y arrojaría una utilidad falsa pero creíble. El costo se reconoce cuando
  se **vende** la mercancía, no cuando se paga la factura.
- **Los balances de terceros solo se mueven por documentos.** La venta a crédito y el cobro
  mueven el balance del cliente; la compra a crédito y el pago, el del proveedor. Ninguno se
  edita a mano.

## 4.x.4 La evidencia: el encadenamiento verificado

La integración se comprobó ejecutando la cadena completa sobre el sistema real, contra la base
de datos, y no solo mediante pruebas unitarias:

Se **compraron 20 libras** de un producto y la existencia subió de 9.5 a **29.5 unidades**, con
el costo actualizado por promedio ponderado a **30.8729**. A continuación se **vendieron 3
libras** y la existencia bajó a **26.5**, con el comprobante fiscal **B0200000001** asignado
automáticamente, un total de **114.00** y un cambio de **86.00**.

Ese solo recorrido demuestra los tres eslabones que el anteproyecto se comprometió a integrar:
la compra alimenta el inventario, la venta lo descuenta y fija su costo, y el comprobante
fiscal se emite sin intervención manual. Complementariamente se verificó la contraparte
financiera: al registrar un **pago a proveedor**, el flujo de caja bajó y la **utilidad no se
movió**, que es exactamente el comportamiento correcto y la prueba de que el costo no se está
contando dos veces.

## 4.x.5 La coordinación entre desarrolladores

Un riesgo real de un sistema modular construido por tres personas en paralelo no es técnico
sino organizativo: que dos desarrolladores editen el mismo archivo y el trabajo de uno
sobrescriba al del otro. Se atacó trazando la frontera **por carpeta de módulo completa** —cada
desarrollador es dueño de carpetas enteras y no edita las de otro— en lugar de por concepto,
que fue la primera repartición y se corrigió al detectarse que obligaba a dos personas a
escribir dentro de los mismos archivos.

Quedaron tres puntos que inevitablemente comparten los tres: el contexto de datos, el registro
de servicios y la línea de migraciones. Para ellos no hay solución de diseño, sino de proceso:
cada módulo registra sus servicios en **su propio archivo parcial**
(`DependencyInjection.<Módulo>.cs`), cada módulo declara sus importaciones en **su propio**
archivo de importaciones, y las migraciones se crean **una a la vez**, integrándolas el mismo
día. Con eso, los archivos verdaderamente compartidos casi nunca se tocan.

## 4.x.6 Alcance de la integración

Lo que el sistema integra y lo que deliberadamente no:

- **Sí integra** los cinco módulos del anteproyecto en una sola base de datos y una sola
  aplicación, sin recaptura de datos entre ellos.
- **No integra** sistemas externos: no hay conexión con comercio electrónico, ni con la
  interfaz de la Dirección General de Impuestos Internos —el comprobante electrónico se genera
  pero no se transmite—, ni con sistemas contables de terceros. Las tres exclusiones están
  declaradas por escrito en el alcance del anteproyecto.
