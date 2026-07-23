# Capítulo IV — Módulo de Clientes

Trabajo Final de Grado · Grupo 6 · Universidad Dominicana O&M  
Responsable del módulo: **Dionis José Castro Gómez**

## 4.x.1 Propósito

El módulo sustituye el cuaderno de clientes y ventas fiadas por un registro centralizado.
Permite identificar al cliente, establecer su plazo de crédito, consultar cuánto debe y
reconstruir su estado de cuenta mediante las facturas y los cobros que originaron el saldo.

## 4.x.2 Arquitectura

La implementación sigue las cuatro capas del proyecto:

| Capa | Responsabilidad |
|---|---|
| Domain | `Cliente` y `Cobro`; reglas del balance y formato del documento. |
| Application | DTO, contratos y servicios para clientes, cobros y cuentas por cobrar. |
| Infrastructure | Configuración EF Core y repositorios de clientes y cobros. |
| Web | Catálogo, editor, registro de cobros y cuentas por cobrar. |

Esta separación mantiene las reglas de negocio fuera de la interfaz. Por ejemplo,
`Cliente.AplicarCobro` valida el monto y registra el balance anterior y resultante antes
de modificar el saldo.

## 4.x.3 Catálogo de clientes

Cada cliente conserva código, nombre, tipo y número de documento, contacto, dirección,
tipo de comprobante preferido, días de crédito y estado activo. El RNC y la cédula se
validan según su longitud; el documento es opcional para compradores informales.

La pantalla `/clientes` permite buscar y administrar el catálogo. El editor presenta el
balance y el historial del cliente junto con sus datos generales, evitando navegar entre
fuentes distintas para responder cuánto debe y por qué.

## 4.x.4 Venta a crédito y balance

Cuando el POS emite una factura a crédito, el total aumenta `Cliente.BalanceActual`. La
factura queda como documento origen de la deuda. Una venta de contado no modifica ese
balance.

El saldo no se edita manualmente: cambia mediante operaciones documentadas. Esta regla
evita que una corrección borre el rastro de lo ocurrido.

## 4.x.5 Cobros

La pantalla `/clientes/cobros/nuevo` registra abonos. Cada `Cobro` conserva:

- cliente, fecha, monto, forma de pago y referencia;
- balance anterior y balance resultante;
- usuario y fechas de auditoría.

No se aceptan montos negativos, cero ni superiores a la deuda. El cobro y el nuevo saldo
se persisten juntos, por lo que no puede quedar un abono sin efecto en el cliente.

## 4.x.6 Estado de cuenta e historial

El estado de cuenta combina las facturas a crédito y los cobros en una secuencia
cronológica. Las facturas aumentan la deuda y los cobros la reducen. Se muestra desde la
ficha del cliente y desde el acceso “Historial” de cuentas por cobrar.

Esta vista responde las tres preguntas operativas principales: qué originó la deuda,
cuánto se ha abonado y cuál es el saldo vigente.

## 4.x.7 Cuentas por cobrar

La pantalla `/clientes/cuentas-por-cobrar` lista solamente clientes con balance positivo.
Calcula los vencimientos usando `Cliente.DiasCredito` y la antigüedad de las facturas
pendientes. Prioriza los montos vencidos para apoyar la gestión diaria de cobro.

## 4.x.8 Decisiones de diseño

| Decisión | Justificación |
|---|---|
| Balance en el cliente + rastro por documento | Consulta rápida sin perder auditoría. |
| Documento opcional | El comercio también atiende consumidores informales. |
| Cobro separado de factura | Permite abonos parciales y múltiples. |
| Estado de cuenta calculado | Evita duplicar información que ya existe en facturas y cobros. |
| Días de crédito por cliente | El vencimiento depende del acuerdo comercial individual. |

## 4.x.9 Pruebas y evidencia

Las pruebas automáticas cubren registro, documento, crédito, cobros, topes y balances.
Durante la revisión del 22 de julio de 2026 la solución completa compiló sin advertencias
y todas sus pruebas pasaron.

Capturas que deben incorporarse al documento final durante la verificación funcional:

1. Catálogo y editor de un cliente con RNC.
2. Venta a crédito reflejada en el balance.
3. Cobro parcial y nuevo saldo.
4. Estado de cuenta con factura y abono.
5. Cuentas por cobrar mostrando días vencidos.

## 4.x.10 Alcance

El módulo administra crédito comercial básico. No incluye intereses, gestión judicial,
límites de crédito automáticos ni integración bancaria, porque no forman parte del alcance
del Mini ERP.

