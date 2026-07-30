# Módulo de clientes

---

## 4.x.1 Qué resuelve y qué problema atacaba

El módulo de clientes administra a los **compradores registrados** del comercio y, sobre todo,
**el crédito que se les otorga**. En un minimarket la mayoría de las ventas son de mostrador y
al contado; el cliente se registra por una razón concreta: **porque lleva fiado**. Ese es el
motivo por el que el límite de crédito y el balance viven en esta entidad y no en otra parte.

El planteamiento del problema describe el **cuaderno de fiados** como uno de los instrumentos
que el comercio usa hoy, con las consecuencias previsibles: no se sabe con certeza cuánto debe
cada quien, desde cuándo, ni cuánto crédito queda disponible; una hoja mojada o extraviada
borra la deuda; y no hay forma de saber si un cliente ya se pasó de lo que se le puede fiar.

Este módulo sustituye ese cuaderno por un registro donde **cada movimiento del balance queda
respaldado por un documento**: la venta a crédito lo sube, el cobro lo baja, y ambos guardan el
saldo anterior y el resultante. También resuelve un requisito fiscal: qué comprobante le
corresponde a cada cliente y con qué documento se sustenta.

## 4.x.2 Decisiones de diseño y su justificación

### Decisión 1 — El límite de crédito en cero define al cliente de contado

No existe una bandera de "cliente a crédito". Lo determina el propio límite: **con límite en
cero, el cliente es solo de contado**. Se eligió así porque evita mantener dos datos que pueden
contradecirse —una bandera que diga "tiene crédito" y un límite en cero sería un estado
inconsistente— y porque refleja la operación real: otorgar crédito **es** ponerle un techo.

### Decisión 2 — El crédito disponible se calcula, nunca se guarda

El crédito disponible es una propiedad **derivada** (`límite − balance`), no un campo
almacenado. Guardarlo obligaría a actualizarlo en cada venta y en cada cobro, con el riesgo de
que quedara desincronizado del balance real. Al derivarlo, siempre es correcto por
construcción.

Se acota además en cero: **nunca es negativo**. Un cliente que debe más de su límite no tiene
"crédito disponible negativo" —eso no significa nada operativamente—, tiene **cero**. La
condición de haberse pasado del techo se expresa aparte, con una propiedad propia que la
interfaz muestra como una advertencia.

### Decisión 3 — Que un cliente exceda su límite es una situación válida, no un error

El sistema **detecta** al cliente cuyo balance supera su límite, pero no lo trata como una
inconsistencia. Ocurre de forma legítima: basta con que el comerciante **baje el límite** a
alguien que ya debía más. Bajar el techo no borra la deuda contraída. Por eso el sistema no
impide la operación —sería impedir una decisión de negocio válida—, sino que **señala la
situación** para que el comerciante la vea.

### Decisión 4 — El balance solo cambia por un documento, con rastro

El balance del cliente **nunca se edita a mano**. Lo mueven dos operaciones: la venta a crédito,
que lo sube, y el cobro, que lo baja. El cobro se aplica mediante `Cliente.AplicarCobro`, que
guarda en el propio cobro el **balance anterior y el resultante** antes de modificar nada.

Con eso, la deuda de cualquier cliente es **reconstruible hacia atrás** y el estado de cuenta se
puede armar movimiento por movimiento. Es exactamente lo que el cuaderno de fiados no permite,
y la razón por la que este módulo puede sustituirlo con confianza. La regla es simétrica a la
que gobierna el balance del proveedor en el módulo de compras y la existencia en inventario:
**ningún saldo se edita**.

### Decisión 5 — Un cobro no puede exceder la deuda

`AplicarCobro` rechaza un cobro mayor que el balance vigente, y también uno de monto cero o
negativo. Permitir un cobro mayor dejaría al cliente con **balance negativo**, que en la
práctica significaría que el comercio le debe a él —un anticipo—, y los anticipos de clientes
son un concepto distinto que el alcance del proyecto no cubre. Es preferible rechazar la
operación a producir un saldo que después nadie sabría interpretar.

### Decisión 6 — El documento se valida por formato, no contra la Dirección General de Impuestos Internos

El sistema valida que la **cédula** tenga once dígitos, el **RNC** nueve y el **pasaporte**
entre cinco y veinte caracteres, y guarda el número **sin guiones** para que el índice único
detecte como iguales dos capturas del mismo documento escritas de distinta forma.

Lo que **no** hace es verificar el documento en línea contra la Dirección General de Impuestos
Internos. Esa consulta depende de un servicio externo y de una integración que el alcance
excluye. Validar el formato atrapa el error más frecuente —el dígito de más o de menos— sin
prometer una verificación que el sistema no puede sostener.

### Decisión 7 — El crédito fiscal exige RNC

Un cliente al que se le emitirá **comprobante de crédito fiscal** debe tener RNC registrado. La
regla vive en el dominio como una propiedad que indica si el comprobante **está sustentado**, y
el sistema impide guardar la combinación inválida.

No es una preferencia: sin el RNC del comprador, la Dirección General de Impuestos Internos no
acepta el comprobante, y el comercio se enteraría del problema cuando ya emitió la factura.
Atajarlo en el registro del cliente es atajarlo antes de que llegue a la caja.

### Decisión 8 — Los códigos del tipo de comprobante son los oficiales

La enumeración de tipos de comprobante lleva los códigos de la Dirección General de Impuestos
Internos —**01** crédito fiscal, **02** consumo, **14** régimen especial, **15**
gubernamental— porque esos dígitos **forman parte del número de comprobante fiscal** que se
emite. Hay una prueba dedicada a fijarlos, para que una renumeración accidental no invalide los
comprobantes.

### Resumen de decisiones

| # | Decisión | Elección | Razón corta |
|---|----------|----------|-------------|
| 1 | Contado o crédito | Lo define el límite en cero | Evita dos datos que se contradigan |
| 2 | Crédito disponible | Derivado y acotado en cero | Siempre correcto; un disponible negativo no significa nada |
| 3 | Exceder el límite | Situación válida que se señala | Bajar el techo no borra la deuda contraída |
| 4 | Balance | Solo por documento, con rastro | Sustituye al cuaderno de fiados y es auditable |
| 5 | Cobro | No puede exceder la deuda | Un balance negativo sería un anticipo, fuera de alcance |
| 6 | Documento | Validación de formato, no en línea | La consulta a la DGII está fuera de alcance |
| 7 | Crédito fiscal | Exige RNC | Sin él la DGII no acepta el comprobante |
| 8 | Tipos | Códigos oficiales de la DGII | Forman parte del número de comprobante |

## 4.x.3 Modelo de dominio

El dominio vive en `src/MiniERP.Domain/Clientes/` y es deliberadamente pequeño: dos entidades y
dos enumeraciones.

- **`Cliente`** — código único, nombre, tipo y número de documento, tipo de comprobante
  preferido, datos de contacto, **límite de crédito**, **balance actual**, días de crédito y
  estado activo. Expone las propiedades derivadas `TieneCredito`, `CreditoDisponible`,
  `ExcedeLimite` y `ComprobanteEstaSustentado`, además del método `PuedeAsumirCredito`, que
  responde si el cliente puede asumir un cargo por un monto dado —comprobando de una sola vez
  que esté activo, que tenga crédito y que el monto quepa en lo disponible.
- **`Cobro`** — el registro inmutable del abono: cliente, fecha, monto, **balance anterior y
  resultante**, observación y usuario responsable.
- **`TipoDocumento`** (ninguno, cédula, RNC, pasaporte) y **`TipoComprobante`** con los códigos
  oficiales.

### `Cliente.AplicarCobro(...)` — el guard del módulo

```csharp
public void AplicarCobro(Cobro cobro)
{
    if (cobro.Monto <= 0)
        throw new InvalidOperationException("El monto del cobro debe ser positivo.");

    if (cobro.Monto > BalanceActual)
        throw new InvalidOperationException(
            $"El cobro de {cobro.Monto} excede la deuda de {BalanceActual}.");

    cobro.BalanceAnterior = BalanceActual;
    cobro.BalanceResultante = RetailConstants.RedondearImporte(BalanceActual - cobro.Monto);
    cobro.ClienteId = Id;

    BalanceActual = cobro.BalanceResultante;
}
```

Valida los dos invariantes, **estampa el rastro en el propio cobro** y solo entonces modifica el
balance. Es el espejo exacto de `Proveedor.AplicarPago` en el módulo de compras y de
`Producto.AplicarMovimiento` en inventario: **una sola puerta por la que el saldo cambia**.

## 4.x.4 Pruebas de dominio

El módulo se cubrió con **quince pruebas**, escritas antes del código.

**Sobre el crédito (12 pruebas).** Que sin límite el cliente es solo de contado; que el
disponible **descuenta lo que ya debe** y **nunca es negativo**; que se detecta al cliente que
pasó su límite; que el cargo a crédito **se topa en el disponible**; que un cliente **inactivo
no puede fiar**; que el documento se valida por largo según su tipo; que el cliente de contado
**no necesita documento**; que el crédito fiscal **sin RNC no se sustenta** y **con RNC sí**;
que el comprobante de consumo no exige documento; y que **los códigos de la enumeración son los
de la Dirección General de Impuestos Internos**.

**Sobre el cobro (3 pruebas).** Que aplicar un cobro **reduce el balance y asigna el rastro**
del saldo anterior y resultante; y que se rechaza un cobro de monto cero o negativo, o que
**exceda la deuda**.

## 4.x.5 Implementación por capas

```
MiniERP.Domain/Clientes          Cliente, Cobro y sus reglas: AplicarCobro,
      ^                           PuedeAsumirCredito, validación del documento.
MiniERP.Application/Clientes      Servicios de cliente y de cobros, contratos de repositorio,
      ^                           DTOs del listado, del estado de cuenta y de cuentas por cobrar.
MiniERP.Infrastructure           Configuración de las tablas, repositorios y migraciones.
      ^
MiniERP.Web/…/Clientes           Las pantallas del módulo.
```

En persistencia, el código del cliente lleva **índice único** y el número de documento un
**índice único filtrado**, que impide duplicados pero permite que muchos registros compartan el
valor nulo —el cliente de contado sin documento—. Los importes usan dos decimales y el balance,
como todo saldo del sistema, solo se escribe desde el dominio.

## 4.x.6 Interfaz de usuario

Las pantallas viven en `src/MiniERP.Web/Components/Pages/Clientes/`:

- **Clientes** — el listado con búsqueda por código, nombre, documento o teléfono, y filtros
  para ver **solo los que llevan crédito** o **solo los que deben**. Cada fila muestra el
  límite, lo que debe y el disponible, y señala con un distintivo al cliente que **excedió su
  límite**. Un panel superior resume la cartera: cuántos clientes hay, cuántos con crédito,
  cuánto suma la deuda total y cuántos están excedidos.
- **Editor de cliente** — el formulario de alta y edición, con el bloque de crédito y el aviso
  cuando el crédito fiscal carece de RNC. Incluye el **estado de cuenta** del cliente: las
  facturas a crédito y los abonos, en orden, cada uno con su balance resultante.
- **Registrar cobro** — el abono a la deuda, que exige monto positivo y no mayor que lo debido.
- **Cuentas por cobrar** — quién debe, cuánto y desde hace cuántos días, usando los días de
  crédito acordados para marcar lo vencido.

## 4.x.7 Verificación

El comportamiento del crédito se verificó de punta a punta sobre el sistema en ejecución: un
cliente compra a crédito y su balance sube por el monto de la factura; se le registra un abono
y el balance baja exactamente esa cantidad; el estado de cuenta muestra las tres líneas —el
cargo, el abono y el saldo— y la pantalla de cuentas por cobrar lo lista con los días
transcurridos. El encadenamiento con la venta a crédito forma parte del recorrido integrado que
documenta la sección 4.7.

## 4.x.8 Alcance y limitaciones

- **No hay anticipos de clientes**: el balance no puede quedar en negativo, de modo que un pago
  por adelantado no está modelado.
- **No se consulta el RNC en línea** contra la Dirección General de Impuestos Internos; la
  validación es de formato.
- **No hay historial de cambios del límite de crédito**: se guarda el límite vigente, no la
  secuencia de aumentos y reducciones.
- **No hay cálculo de intereses ni de mora** sobre la deuda vencida: el sistema informa los días
  transcurridos, pero el comercio no cobra intereses.
