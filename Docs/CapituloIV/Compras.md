# Capítulo IV — Módulo de Compras

Trabajo Final de Grado · Grupo 6 · Universidad Dominicana O&M  
Responsable del módulo: **Dionis José Castro Gómez**

## 4.x.1 Propósito

El módulo controla el ingreso de mercancía desde el proveedor hasta el inventario y el
ciclo de la deuda asociada. Incluye proveedores, compras, recepción, pagos, cuentas por
pagar y devoluciones a proveedor.

## 4.x.2 Arquitectura

| Capa | Componentes principales |
|---|---|
| Domain | `Proveedor`, `Compra`, `LineaCompra`, `Pago` y `DevolucionCompra`. |
| Application | Servicios y contratos de proveedores, compras, pagos y devoluciones. |
| Infrastructure | Configuraciones EF Core, repositorios y migraciones. |
| Web | Proveedores, compras, pagos, cuentas por pagar y devoluciones. |

## 4.x.3 Proveedores

El proveedor conserva su identificación, contacto, plazo de crédito y balance. El balance
representa cuánto debe el comercio; un valor negativo representa crédito a favor del
comercio después de devolver mercancía ya pagada.

Los cambios de saldo no se realizan editando el número directamente. Una compra a crédito,
un pago o una devolución dejan el documento que explica el cambio.

## 4.x.4 Compra y recepción

Una compra inicia como borrador. Sus líneas congelan producto, descripción, cantidad,
costo e ITBIS. Al recibirla:

1. se valida el documento completo;
2. cada producto recibe un movimiento de entrada;
3. la existencia aumenta;
4. el costo se recalcula mediante promedio ponderado;
5. si es a crédito, aumenta el balance del proveedor;
6. todo se guarda mediante un solo `SaveChanges`.

Después de recibida la compra queda congelada. No se anula porque ya produjo movimientos;
para revertir mercancía se registra una devolución independiente.

## 4.x.5 Pagos y cuentas por pagar

Un pago reduce `Proveedor.BalanceActual` y guarda el saldo anterior y resultante. No se
permite pagar más que la deuda. La pantalla de cuentas por pagar agrupa los proveedores
con saldo pendiente y calcula vencimientos usando sus días de crédito.

El pago es una salida de efectivo, pero no un egreso operativo. El flujo de caja lo resta;
el estado de resultados no lo vuelve a contar como gasto.

## 4.x.6 Devolución a proveedor

La devolución se crea desde una compra recibida. Cada línea referencia la línea original,
por lo que hereda el costo congelado y conoce la cantidad máxima devolvible. Admite
devoluciones parciales y acumulables.

Al confirmar:

1. valida motivo, productos, cantidades compradas y devoluciones anteriores;
2. impide devolver más de la existencia física;
3. crea movimientos `DevolucionProveedor` que bajan el inventario;
4. mantiene el costo promedio del producto, porque una salida no aporta costo nuevo;
5. reduce el saldo del proveedor y registra balance anterior/resultante;
6. congela el documento y persiste todo en una sola operación.

Si la mercancía ya estaba pagada, el saldo puede ser negativo. Ese valor representa un
crédito que el proveedor debe aplicar o reembolsar al comercio.

La persistencia utiliza `DevolucionesCompra` y `LineasDevolucionCompra`, agregadas mediante
la migración `AgregarDevolucionCompra`. La interfaz incluye lista, filtros, borrador,
edición y confirmación.

## 4.x.7 Decisiones de diseño

| Decisión | Justificación |
|---|---|
| Compra recibida inmutable | Preserva el documento y los movimientos originales. |
| Devolución como documento propio | Deja número, fecha, motivo, líneas, totales y auditoría. |
| Costo tomado de la compra | Es el valor que el proveedor reconoce. |
| No repromediar en una salida | La devolución no aporta información de costo nuevo. |
| Un solo `SaveChanges` | Documento, inventario y saldo cambian juntos. |
| Saldo negativo permitido al devolver | Representa crédito real por mercancía ya pagada. |

## 4.x.8 Pruebas

Las pruebas de devolución cubren:

- disminución del inventario;
- costo congelado y costo vivo sin modificación;
- tope comprado, acumulado y existencia física;
- motivo y líneas obligatorias;
- confirmación única;
- totales e ITBIS;
- ajuste del balance y crédito a favor;
- servicio completo con una sola llamada de guardado.

## 4.x.9 Evidencia funcional pendiente

La compilación y las pruebas automáticas quedaron verificadas. La aplicación de la
migración y las capturas requieren iniciar SQL Server en el ambiente del piloto.

Capturas que deben incorporarse:

1. Compra recibida con el botón “Devolver mercancía”.
2. Borrador mostrando comprado, devolvible y cantidad.
3. Confirmación exitosa.
4. Kardex con movimiento “Devolución a proveedor”.
5. Existencia antes/después.
6. Saldo del proveedor antes/después.
7. Lista de devoluciones con estado Confirmada.

## 4.x.10 Alcance

No se genera una nota de crédito fiscal electrónica del proveedor ni conciliación
bancaria. El módulo registra el efecto operativo y financiero necesario para el piloto.

