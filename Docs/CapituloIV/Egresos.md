# Capítulo IV — Módulo de Egresos (Finanzas)

Trabajo Final de Grado · Grupo 6 · Universidad Dominicana O&M
Autor del módulo: **Samuel Sánchez** · rama `samuel/finanzas` · construido el 2026-07-17
al 2026-07-19.

> Esta sección documenta la primera pieza del módulo **Finanzas**: el registro de
> **egresos** (gastos operativos). Se escribe con el detalle y la justificación de cada
> decisión, para poder defenderla en la sustentación. El diseño validado vive en
> `Docs/superpowers/specs/2026-07-17-egreso-design.md` y el plan de implementación en
> `Docs/superpowers/plans/2026-07-17-egreso.md`.

---

## 4.x.1 Qué es un egreso y qué problema resuelve

Un **egreso** es un gasto operativo del comercio que **no pasa por el inventario**: el
alquiler del local, la factura de la luz, el agua, los sueldos de los empleados, el
transporte de la mercancía. Son los desembolsos necesarios para mantener el negocio
abierto, distintos de la compra de la mercancía que se revende.

El comerciante del caso piloto —el minimarket de Jeison— hoy no tiene forma de saber
cuánto gasta en operar. Anota las ventas, pero los gastos viven en la memoria o en
papeles sueltos. Sin registrarlos, es imposible calcular la ganancia real: un negocio
puede vender mucho y aun así perder si el alquiler y los sueldos se comen el margen. El
planteamiento del problema pide, textualmente, *"calcular sus márgenes"*; y no hay margen
verdadero sin restar los gastos de operación.

El módulo de egresos resuelve esa carencia: le da al comerciante una pantalla donde
anota cada gasto con su **fecha, categoría, monto y una nota**, y guarda **quién lo
registró**. Con eso, los reportes posteriores del módulo de Finanzas —el estado de
resultados y el flujo de caja— pueden **restar** esos gastos y mostrar cuánto gana y
cuánto efectivo le queda de verdad.

### La trampa que este módulo evita a propósito: un pago a proveedor NO es un egreso

Esta es la decisión conceptual más importante del módulo, y la razón por la que un egreso
se diseñó separado de todo lo demás. Existen tres operaciones que parecen la misma cosa
—"sale dinero del negocio"— pero son distintas y afectan la utilidad de forma diferente:

| Concepto | Dueño | Qué es | ¿Afecta la utilidad? |
|---|---|---|---|
| **Egreso** | Samuel · `Finanzas/` | Gasto operativo que no pasa por inventario | **Sí** |
| **Pago a proveedor** | Dionis · `Compras/` | Saldar una deuda por mercancía | **No** — liquida una deuda |
| **Cobro a cliente** | Dionis · `Clientes/` | Saldar una deuda del cliente | **No** — el ingreso ya se reconoció al facturar |

El error costoso —y silencioso, porque no lanza ningún fallo y produce números creíbles—
sería registrar los pagos a proveedor como egresos. Eso contaría el costo de la mercancía
**dos veces**:

```
El comercio compra arroz por 640, lo vende en 1,000, y luego le paga al proveedor.

  Ingresos                          1,000
− Costo de lo vendido                −640    ← el costo ya está contado aquí
− Egresos (pago al proveedor)        −640    ← contado otra vez, por error
= Utilidad                            −280   ← FALSO. En realidad ganó 360.
```

El gasto de la mercancía ocurrió cuando se **vendió** (ahí se reconoce el costo de lo
vendido), no cuando se pagó la factura del proveedor. Por eso el egreso, en este sistema,
**nunca toca el inventario ni el balance de un proveedor**: es solamente un registro de un
gasto operativo. Esta separación es el "gate" de validación del módulo de Finanzas:
registrar un pago a proveedor y comprobar que la utilidad **no** se mueve.

---

## 4.x.2 Decisiones de diseño y su justificación

Antes de escribir una línea de código se tomaron seis decisiones de diseño. Cada una se
justifica abajo, no como preferencia de estilo sino por lo que responde en el problema.

### Decisión 1 — La categoría del gasto es una entidad, no un enum ni texto libre

Se evaluaron tres formas de representar la categoría de un egreso (Alquiler, Luz, etc.):

- **Texto libre** (un `string` en el egreso): la más rápida, pero pésima para reportes.
  Cada error de escritura —"Alquiler", "alquiler", "Alkiler"— crea una categoría nueva, y
  los totales por categoría dejan de cuadrar.
- **Enum fijo** (un conjunto cerrado en el código): simple y sin tabla, pero **no se puede
  ampliar sin tocar el código**. El comerciante que mañana quiera una categoría
  "Publicidad" o "Mantenimiento del freezer" tendría que esperar a un programador.
- **Entidad `CategoriaEgreso`** (un catálogo en la base de datos): **la elegida.**

Se eligió la entidad porque es la única que satisface las tres condiciones a la vez: el
estado de resultados **agrupa los egresos por categoría** de forma limpia (sin typos), el
comerciante **puede agregar sus propias categorías**, y **calca un patrón que el sistema ya
usa** para las categorías de productos (`Categoria` en el módulo de Inventario). Reusar un
patrón existente hace el módulo más fácil de defender: no es una invención suelta, es la
misma solución aplicada a un problema gemelo.

### Decisión 2 — Un solo monto, sin desglose de ITBIS

Un egreso guarda un único campo `Monto` con el total del gasto. No se desglosa el ITBIS.
La razón es de alcance: desglosar el ITBIS de un gasto solo serviría para reclamar el
**crédito fiscal** ante la DGII, y la certificación fiscal está **fuera del alcance** del
proyecto por escrito. El comerciante razona en términos simples —"la luz me costó
5,000"— y el módulo respeta esa forma de pensar. Agregar un desglose que nadie va a usar
sería complejidad sin beneficio (principio YAGNI: *no lo construyas hasta que lo
necesites*).

### Decisión 3 — El egreso se registra ya pagado (base de efectivo)

El egreso guarda una sola `Fecha`, que representa **cuándo ocurrió y se pagó** el gasto. No
existe un estado de "pendiente de pago" ni una fecha de pago separada. La justificación es
la realidad del negocio: un minimarket **no lleva cuentas por pagar de sus gastos
operativos** —paga la luz, el alquiler y los sueldos en el momento—. Modelar una
contabilidad de acumulación (registrar el gasto cuando se incurre y pagarlo después) sería
maquinaria contable que el comercio no usa, y complicaría los dos reportes de Finanzas sin
ningún beneficio para el piloto.

Esta decisión tiene una consecuencia elegante: como el egreso **ocurre y se paga el mismo
día**, entra igual en el **estado de resultados** (que cuenta el gasto cuando se incurre) y
en el **flujo de caja** (que lo cuenta cuando se paga). La `Fecha` sirve para ambos.

### Decisión 4 — Se guarda quién registró el gasto (`UsuarioId`)

Cada egreso guarda un campo `UsuarioId` con el usuario responsable de haberlo registrado,
y ese dato se muestra en la lista como "registrado por". Esta decisión surgió de una
revisión del diseño: un registro de finanzas sin responsable es un registro anónimo, y en
un negocio importa saber quién anotó cada gasto.

Es importante distinguir dos cosas que podrían confundirse:

- **`CreadoPor`** lo aporta la clase base `EntidadBase` a **todas** las entidades del
  sistema; es un campo de **auditoría de infraestructura** (quién creó la fila), no algo
  que el módulo trate como propio.
- **`UsuarioId`** es un campo **de primera clase del dominio**: "el responsable del gasto",
  que se muestra en pantalla y es parte de la información del negocio.

Se eligió agregar `UsuarioId` explícito —además del `CreadoPor` de auditoría— porque es
exactamente el patrón que el proyecto **ya usa** para sus registros con responsable: la
entidad `Pago` (del módulo de Dionis) y `MovimientoInventario` (del módulo de Jeison)
guardan ambos un `UsuarioId` con esa misma intención. Otra vez, no se inventó un patrón:
se copió el que el sistema ya tenía para un caso análogo. La fábrica `Registrar` fija los
dos campos (`UsuarioId` y `CreadoPor`) con el mismo usuario.

### Decisión 5 — La validación vive en el dominio (`Egreso.Registrar`)

En lugar de un constructor abierto que permita crear cualquier egreso, la creación pasa por
una **fábrica estática** `Egreso.Registrar(...)` que valida los invariantes **antes** de
construir el objeto. Un egreso no puede tener monto cero ni negativo, ni quedar sin
categoría, y ese cumplimiento vive en el dominio, no disperso en la pantalla. La razón es
la filosofía del proyecto —*"el dominio decide"*—: las reglas de negocio se prueban con
TDD en la capa de dominio, que no depende de la base de datos ni de la interfaz. Poner la
validación ahí permite probarla sin levantar nada, y garantiza que ninguna otra parte del
sistema pueda crear un egreso inválido saltándose la regla.

### Decisión 6 — El módulo se construye completo (dominio a pantalla), sin bloqueos

A diferencia de otras piezas que dependen de trabajo de otros desarrolladores, el egreso
es una **tabla nueva en un módulo nuevo (`Finanzas/`) sin entidades compartidas**. No lee
ni modifica el inventario, ni los proveedores, ni las facturas. Por eso se pudo llevar de
punta a punta —dominio, aplicación, base de datos y pantallas— sin esperar a nadie. Lo
único que toca del código compartido son los tres archivos que **todos** los
desarrolladores tocan (el contexto de base de datos, el registro de servicios y las
migraciones), y para eso la regla del proyecto es "traer `main` antes", que se cumplió.

### Resumen de decisiones

| # | Decisión | Elección | Razón corta |
|---|----------|----------|-------------|
| 1 | Categoría | Entidad `CategoriaEgreso` | Extensible + agrupa reportes + calca `Categoria` |
| 2 | ITBIS | Un solo `Monto` | El desglose solo sirve a fiscalidad, fuera de alcance |
| 3 | Pago | Pagado al registrar | El comercio no lleva cuentas por pagar de gastos |
| 4 | Responsable | `UsuarioId` explícito | Como `Pago` y `MovimientoInventario`; auditable |
| 5 | Validación | Fábrica `Registrar` en el dominio | El dominio decide y se prueba sin base de datos |
| 6 | Alcance | Feature completa sin gating | Módulo nuevo sin entidades compartidas |

---

## 4.x.3 Modelo de dominio

El dominio del módulo vive en `src/MiniERP.Domain/Finanzas/` y está formado por dos
entidades y un enum implícito de estado (aquí no hace falta estado: un egreso es un hecho,
no un documento con ciclo de vida).

### `CategoriaEgreso`

Catálogo de tipos de gasto. Hereda de `EntidadBase` (que aporta `Id`, `FechaCreacion`,
`CreadoPor`, `FechaModificacion`, `ModificadoPor`).

| Campo | Tipo | Por qué |
|-------|------|---------|
| `Nombre` | `string` | El nombre visible: "Alquiler", "Luz". Obligatorio y único. |
| `Descripcion` | `string?` | Detalle opcional. |
| `Activo` | `bool` | Permite **desactivar** una categoría sin borrarla (ver más abajo). |
| `Egresos` | colección | Los egresos que la usan; permite contar cuántos tiene. |

Es deliberadamente gemela de la `Categoria` de productos: misma forma, mismo criterio.

### `Egreso`

El gasto en sí. Hereda de `EntidadBase`.

| Campo | Tipo | Por qué |
|-------|------|---------|
| `Fecha` | `DateTime` | Cuándo ocurrió y se pagó el gasto (base de efectivo, decisión 3). |
| `CategoriaEgresoId` | `int` | A qué categoría pertenece; obligatorio. |
| `Monto` | `decimal` | El total pagado (decisión 2). Debe ser mayor que cero. |
| `Descripcion` | `string?` | Nota opcional: "Sueldo de María, primera quincena". |
| `UsuarioId` | `string?` | El usuario responsable del registro (decisión 4). |

### `Egreso.Registrar(...)` — el guard del dominio

```csharp
public static Egreso Registrar(
    decimal monto, DateTime fecha, int categoriaEgresoId,
    string? descripcion, string? usuarioId)
{
    if (monto <= 0)
        throw new InvalidOperationException("El monto del egreso debe ser mayor que cero.");

    if (categoriaEgresoId == 0)
        throw new InvalidOperationException("El egreso debe tener una categoría.");

    return new Egreso
    {
        Monto = monto,
        Fecha = fecha,
        CategoriaEgresoId = categoriaEgresoId,
        Descripcion = string.IsNullOrWhiteSpace(descripcion) ? null : descripcion.Trim(),
        UsuarioId = usuarioId,
        CreadoPor = usuarioId
    };
}
```

Valida los dos invariantes antes de construir, deja la descripción vacía como nula (para
que un espacio en blanco no se confunda con una nota), y estampa el responsable en los dos
campos. Es el único camino por el que se crea un egreso válido.

---

## 4.x.4 Pruebas de dominio (metodología TDD)

El proyecto sigue **desarrollo guiado por pruebas (TDD)** sobre el dominio: se escribe
primero la prueba que describe el comportamiento esperado, se comprueba que falla (porque
el código aún no existe), y solo entonces se escribe el código mínimo para que pase. Esto
garantiza que cada regla tiene una prueba que la respalda, y que la prueba de verdad prueba
algo (porque se la vio fallar primero).

El dominio del egreso se cubrió con **cinco pruebas** en
`tests/MiniERP.Tests/Finanzas/EgresoTests.cs`. Cada una fija una garantía:

| Prueba | Qué garantiza |
|--------|---------------|
| `Registrar_valido_crea_el_egreso` | Un egreso válido se crea con su fecha, monto, categoría, descripción y **responsable** guardado. |
| `Registrar_sin_descripcion_deja_la_nota_nula` | Una nota vacía o en blanco queda nula, no como texto en blanco. |
| `Registrar_con_monto_cero_falla` | No se puede registrar un gasto de cero. |
| `Registrar_con_monto_negativo_falla` | No se puede registrar un gasto negativo. |
| `Registrar_sin_categoria_falla` | Todo gasto debe estar clasificado. |

Las cinco pasan, y con ellas la suite completa del sistema quedó en **118 pruebas verdes**
(las 113 previas más estas 5), sin advertencias de compilación.

---

## 4.x.5 Arquitectura en capas

El sistema es **modular en capas**: modular por módulo de negocio (Inventario, Ventas,
Compras, Clientes, Finanzas) y en capas por responsabilidad. Las capas internas nunca
dependen de las externas. El egreso se implementó respetando esa estructura, con una
carpeta `Finanzas/` en cada capa:

```
MiniERP.Domain/Finanzas          Egreso, CategoriaEgreso, Egreso.Registrar. Reglas puras.
      ^                          No conoce base de datos ni pantallas.
MiniERP.Application/Finanzas     EgresoService, CategoriaEgresoService, contratos de
      ^                          repositorio, DTOs. Los casos de uso. No conoce EF Core.
MiniERP.Infrastructure           Configuración EF, repositorios, siembra, migración.
      ^                          Lo único que habla con SQL Server.
MiniERP.Web/…/Finanzas           Las tres pantallas. Solo presentación.
```

El patrón que gobierna el módulo —heredado del resto del sistema— es: **el dominio decide,
el servicio orquesta y persiste.** La regla de que un monto no puede ser negativo vive en
el dominio (`Registrar`); el servicio la invoca y además valida la entrada del formulario
con mensajes amables; el repositorio solo lee y escribe. Esta separación es lo que permite
probar la regla sin base de datos y cambiar la interfaz sin tocar la lógica.

---

## 4.x.6 Capa de aplicación

En `src/MiniERP.Application/Finanzas/` viven los casos de uso, expresados como servicios
que la pantalla invoca sin saber nada de Entity Framework.

- **`EgresoService`** — buscar egresos por rango de fechas y categoría (con paginación),
  obtener el **total del período** (la suma de los montos que cumplen el filtro), guardar
  (crea vía `Egreso.Registrar` o edita reaplicando las mismas reglas) y eliminar.
- **`CategoriaEgresoService`** — buscar categorías, entregar las **opciones activas** para
  el desplegable del formulario, guardar (con nombre único), y **activar/desactivar**.

Los **contratos de repositorio** (`IEgresoRepositorio`, `ICategoriaEgresoRepositorio`) son
interfaces: definen qué necesita el servicio de la base de datos sin decir cómo. Los
**DTOs** (`EgresoListaDto`, `EgresoFormDto`, `FiltroEgresos`, y los de categoría) son las
formas de los datos que viajan hacia y desde la pantalla; separarlos de las entidades del
dominio evita que la interfaz dependa de detalles internos.

Un detalle de diseño digno de defensa: el "total del período" se calcula como una consulta
aparte (`ObtenerTotalAsync`), no sumando en memoria los registros de la página. Sumar solo
la página daría un total falso cuando hay más de una página; sumar en la base de datos con
el mismo filtro da el total correcto de todo el período.

---

## 4.x.7 Infraestructura y persistencia

En `src/MiniERP.Infrastructure/` está lo único que habla con SQL Server.

### Configuración de las tablas (Entity Framework)

`FinanzasConfiguration.cs` describe cómo se mapean las dos entidades a tablas:

- La tabla `Egresos` guarda el `Monto` con precisión `decimal(18,2)` (dos decimales, los
  que existen en efectivo), la nota hasta 300 caracteres, y una **llave foránea** a
  `CategoriasEgreso` con borrado **restringido** (no se puede borrar una categoría que
  tenga egresos, para no dejarlos huérfanos).
- Se crearon dos **índices** sobre `Egresos`: uno por `Fecha` y otro por
  `(CategoriaEgresoId, Fecha)`. La razón es que los reportes de egresos **filtran por rango
  de fechas y agrupan por categoría**; los índices hacen esas consultas rápidas.
- La tabla `CategoriasEgreso` tiene un índice **único** sobre el nombre, para impedir dos
  categorías iguales.

### Repositorios

`FinanzasRepositorios.cs` implementa los contratos. Las lecturas usan `AsNoTracking`
(sin seguimiento) porque no se van a modificar, lo que las hace más ligeras; las escrituras
y las cargas para editar sí traen la entidad con seguimiento. La lista de egresos proyecta
el **"registrado por"** directamente desde el `UsuarioId` del egreso.

> **Ajuste durante la verificación:** al principio el "registrado por" intentaba buscar el
> correo del usuario cruzando contra la tabla de usuarios por un identificador. Al leer el
> código de autenticación se descubrió que, en este sistema, el "usuario" que se guarda
> **ya es el correo de la sesión** (`admin@minierp.local`), no un identificador interno.
> Por eso el cruce no coincidía y habría dejado el campo en blanco. Se corrigió para usar
> el `UsuarioId` directamente. Este hallazgo es un ejemplo de por qué la verificación en
> ejecución importa: el código compilaba, pero el dato habría salido vacío.

### Siembra de datos (arranque autoconfigurable)

El sistema siembra al arrancar todo lo que necesita para operar, de forma **idempotente**
(correrlo mil veces produce lo mismo que correrlo una). Se agregó a `DatabaseInitializer`
la siembra de **cinco categorías de egreso** típicas: Alquiler, Luz, Agua, Sueldos,
Transporte. Así el comerciante no empieza con una lista vacía, pero puede cambiarlas.

Se sembraron con el **sembrador de arranque** y no con `HasData` (la siembra que EF
incrusta en la migración), por una regla del proyecto: `HasData` es solo para catálogo
**inmutable** (como las unidades de medida); lo que el usuario **editará** va en el
sembrador de arranque. Como el comerciante puede modificar sus categorías, corresponde el
sembrador.

### Migración

La tabla se crea con una **migración** de Entity Framework
(`AgregarFinanzasEgreso`), que genera `CategoriasEgreso` y `Egresos` con su relación e
índices, y nada más. La migración se dejó como **último paso** de la construcción, por una
disciplina del proyecto: los tres desarrolladores comparten un solo `MiniErpDbContext` y un
solo archivo de "instantánea del modelo" (`ModelSnapshot`); crear una migración fuera de
una ventana coordinada es zona de conflictos. Se trajo `main` antes de crearla, se
verificó que compilaba, se aplicó a la base de datos y se comprobó que creaba exactamente
las dos tablas esperadas.

---

## 4.x.8 Interfaz de usuario

Las tres pantallas viven en `src/MiniERP.Web/Components/Pages/Finanzas/`, construidas con
Blazor Server siguiendo el mismo estilo (Bootstrap) del resto del sistema.

1. **Egresos** (`/finanzas/egresos`) — la pantalla principal. Arriba, un formulario para
   **registrar** un gasto (fecha, categoría en desplegable, monto, nota); en el medio, un
   **filtro** por rango de fechas y categoría, con el **total del período** a la derecha; y
   abajo, la **lista** de egresos con su fecha, categoría, monto, nota, **"registrado por"**
   y botones de editar y eliminar. El mismo formulario sirve para editar: al pulsar
   "Editar", carga el egreso y cambia a "Guardar cambios".

2. **Categorías de egreso** (`/finanzas/egresos/categorias`) — administra el catálogo:
   agregar, renombrar y **activar/desactivar**. Se desactivan en vez de borrarse para no
   dejar gastos históricos sin categoría. Muestra cuántos egresos tiene cada una.

3. **Finanzas** (`/finanzas`) — la portada del módulo, con una tarjeta que enlaza a los
   egresos y un aviso de los reportes que llegan en las tareas siguientes.

---

## 4.x.9 Verificación (pruebas funcionales en navegador)

Además de las pruebas automáticas del dominio, el módulo se **verificó de punta a punta en
el navegador**, con la aplicación real corriendo contra SQL Server. No basta con que
compile: hay que verlo funcionar. Se registró un usuario de prueba, se inició sesión, y se
manejó la pantalla:

| Prueba funcional | Resultado |
|------------------|-----------|
| Arranque de la app | La siembra corrió: *"Categorías de egreso iniciales sembradas: 5"*. |
| Abrir la pantalla | El desplegable trae las 5 categorías sembradas; total RD$ 0.00. |
| Registrar Luz · RD$ 5,000 · "Luz de enero" | Aviso "Egreso registrado."; aparece en la lista con **"registrado por"** y el total pasa a RD$ 5,000.00. |
| Registrar **sin categoría** | Aviso rojo "Selecciona una categoría."; **no** se agrega nada. |
| Editar el monto a RD$ 6,000 | La fila y el total se actualizan a RD$ 6,000.00. |
| Eliminar el egreso | La lista queda vacía y el total vuelve a RD$ 0.00. |

La captura de la pantalla funcionando está en `egresos-verificacion.png`. El ciclo
completo —alta, validación, edición y borrado— quedó demostrado en ejecución.

---

## 4.x.10 Cómo alimenta los reportes siguientes

El egreso es la primera pieza del módulo de Finanzas; las demás lo consumen:

- **Estado de resultados** = Ingresos (subtotal de las facturas, sin ITBIS) − costo de lo
  vendido − **suma de los egresos del período**. El egreso entra aquí como gasto operativo.
- **Flujo de caja** = ventas de contado + cobros − **egresos** − pagos a proveedor. Como el
  egreso está pagado al registrarse, entra en el flujo con su misma fecha.
- Ambos pueden **agrupar por categoría** de egreso para el desglose, gracias a que la
  categoría es una entidad y no un texto libre.

---

## 4.x.11 Alcance y limitaciones

Lo que el módulo de egresos **no** hace, por decisión y por alcance:

- **No desglosa ITBIS** — el crédito fiscal está fuera del alcance del proyecto.
- **No lleva cuentas por pagar de gastos** — el egreso se registra ya pagado.
- **No toca el inventario ni los balances** — no es una compra ni un pago a proveedor.

Estas no son carencias, son fronteras trazadas a propósito para que el módulo responda
exactamente lo que el problema pide, sin cargar complejidad que el comercio no usa.

---

## Anexo — Contexto de la construcción

Durante la misma jornada ocurrieron dos hechos de coordinación que enmarcan este módulo y
conviene dejar registrados:

1. **Cambio de frontera entre desarrolladores (2026-07-17).** La repartición del trabajo se
   corrigió de "por concepto" a "por carpeta/módulo completo". Con ello, Samuel quedó como
   dueño de **`Finanzas/` únicamente** (una capa analítica que lee los demás módulos sin
   modificarlos), y el módulo de **Compras pasó a Dionis**, que ya trabajaba dentro de él
   con los pagos. El módulo de egresos se construyó ya bajo esta frontera nueva.

2. **Devolución a proveedor.** Antes del cambio de frontera, Samuel construyó la
   funcionalidad de devolución a proveedor completa (dominio y aplicación, con 13 pruebas).
   Al mover Compras a Dionis, esa funcionalidad quedó en su módulo y se le entregó como
   adelanto. Su diseño y su lógica están documentados en
   `Docs/superpowers/specs/2026-07-17-devolucion-proveedor-design.md`.

---

*Registro de commits del módulo (rama `samuel/finanzas`):*
`Egreso: entidades y Registrar` · `Egreso: validaciones de Registrar` ·
`Egreso: capa de Aplicación` · `Egreso: infraestructura, siembra y migración` ·
`Egreso: pantallas Web`.
