# Informe de cierre — Capítulo V

**Para quien redacta el documento del Trabajo Final de Grado.**
Grupo 6 · Mini ERP · Universidad Dominicana O&M · Fecha de corte: **30 de julio de 2026**

---

## 1. Cómo usar este informe

Este documento resume **todo lo que se hizo, lo que se encontró y lo que falta** para el
Capítulo V. Está escrito para que alguien que no tocó el código pueda redactar con él.

Tres reglas que se siguieron y que conviene mantener al redactar:

1. **Ninguna cifra de este informe se inventó.** Cada número se obtuvo ejecutando algo:
   una consulta, una compilación, una suite de pruebas o un script de validación.
2. **Se distingue siempre entre tres cosas**, y no deben mezclarse al escribir:
   **defecto** (existe y funciona mal), **ausencia** (no existe y estaba previsto) y
   **limitación declarada** (no existe y el propio proyecto dice que queda fuera de alcance).
3. **Lo que no se midió, no se escribe.** Por eso hay secciones del capítulo que todavía no
   están: requieren ejecutar el piloto.

### Dónde está cada cosa

| Documento | Qué contiene |
|---|---|
| `E0-Matriz-de-Capacidades.md` | Las 55 capacidades auditadas, con evidencia por fila |
| `E1-Entorno-y-Linea-Base.md` | Versiones, pasos de instalación y línea base de pruebas |
| `E2-Plan-de-Pruebas.md` | Objetivo, alcance, calendario de 20 días, riesgos |
| `E3-Datos-Maestros.md` | El comercio ficticio y sus datos, con su validación |
| `datos/*.csv` | Los datos en sí: 64 productos, 40 clientes, 12 proveedores, 5 usuarios |
| `E6-Cuadres-de-Integridad.sql` | Las 17 verificaciones, ejecutables |
| `Capitulo-V-5.1-Plan-de-Pruebas.md` | **Texto listo para el documento** |
| `Correcciones-previas-al-piloto.md` | Los defectos repartidos por dueño, con su estado |

---

## 2. Estado del proyecto al 30 de julio

### 2.1 Lo que está integrado en `main`

El 28 de julio Jeison integró el control de acceso completo. **`main` hoy tiene:**

- El núcleo de permisos: catálogo de 17 permisos, proveedor de políticas y manejador.
- Las tres pantallas de administración de usuarios y permisos.
- **29 pantallas protegidas por permiso**, de las 30 que tienen ruta.
- Las secciones de Inventario y de Ventas del Capítulo IV.

Antes de esa fecha, `main` **no tenía ningún control de acceso**: cualquier sesión llegaba a
las 27 pantallas del sistema. Ese dato importa para el informe, porque marca un antes y un
después.

### 2.2 Lo que está en cola

| Rama | Autor | Estado |
|---|---|---|
| `dionis/permisos` | Dionis | **PR #4 abierto**, listo para revisar. 182 pruebas en verde, sin conflictos |
| `jeison/seguridad` | Jeison | Sin cambios desde el 28. Cuatro defectos pendientes |
| `samuel/permisos` | Samuel | Cerrada. Su contenido ya está en `main` |

---

## 3. Qué se hizo, y con qué método

### 3.1 Auditoría de capacidades antes de probar nada

Se auditaron **55 capacidades** propias de un comercio minorista, una por una, exigiendo para
cada veredicto la cita del archivo y la clase o método, o de la pantalla y su ruta.

**Para evitar el sesgo de confirmación**, toda capacidad declarada ausente o incompleta pasó
por una segunda revisión independiente cuyo único encargo era **demostrar que la primera se
había equivocado**. Se buscó con sinónimos, en inglés y en español, en las cuatro capas,
incluidas migraciones y pruebas.

**De las 27 capacidades sometidas a esa segunda revisión, ninguna cambió de veredicto.**

### 3.2 Verificaciones de integridad probadas antes del piloto

Las 17 verificaciones no se escribieron y se guardaron: **se ejecutaron contra una base de
datos con operaciones reales**. Eso permitió depurarlas, y en el proceso apareció un defecto
que la auditoría de código no había visto (ver §5, defecto D-03).

---

## 4. Resultados medidos

### 4.1 Capacidades

| Estado | Capacidades | Proporción |
|---|---:|---:|
| Existe | **28** | 51 % |
| Existe parcialmente | **12** | 22 % |
| No existe | **15** | 27 % |
| **Total** | **55** | 100 % |

**La frase que resume el hallazgo, y que sirve para el documento:** lo que está construido es
el **ciclo documental completo** —comprar, recibir, vender, facturar, fiar, cobrar, pagar,
gastar y reportar—; lo que falta es la **operación del mostrador**: la caja con su apertura y
su arqueo, los medios de pago distintos del efectivo y el crédito, los descuentos y las
devoluciones al cliente.

*El sistema sabe llevar la contabilidad de un colmado, pero todavía no sabe atender su
mostrador.*

**Ausencias principales** (son hallazgo, se reportan): caja completa con apertura, cierre,
arqueo y corte Z; pagos con tarjeta y transferencia; pagos combinados; descuentos por línea y
por factura; devolución parcial de venta; reembolsos; devolución a proveedor; respaldo y
restauración.

**Limitaciones declaradas** (no son hallazgo, están excluidas por escrito en el anteproyecto):
operación multisucursal, transferencias entre almacenes y certificación fiscal ante la DGII.

### 4.2 Línea base del sistema

| Medición | Resultado |
|---|---|
| Compilación limpia | **12.6 segundos**, 0 errores, 0 advertencias |
| Pruebas automatizadas | **180 aprobadas de 180**, en 76 milisegundos |
| Pruebas que tocan la base de datos | **Ninguna** |

Ese último dato es relevante para el documento: las 180 pruebas existentes son **unitarias de
dominio**, y corren en 76 milisegundos precisamente porque no hacen entrada ni salida. Todo lo
que el piloto debe demostrar —que el comprobante fiscal no se duplica bajo concurrencia, que
una transacción fallida no deja inventario a medias, que los saldos cuadran tras 20 días—
**está fuera del alcance de esa suite por construcción**. No es un defecto de las pruebas
actuales: es que cubren otra cosa.

### 4.3 Entorno de validación

Windows 11 Pro · .NET 10.0.302 · SQL Server 2022 Developer · Intel Core i7-13650HX, 20 hilos,
23.6 GB de RAM.

**Advertencia que debe constar en el documento:** el equipo de medición es sensiblemente más
potente que el de un colmado. Los tiempos de respuesta deben leerse como **cota inferior
optimista**: en el equipo real serán iguales o peores.

### 4.4 Datos del comercio piloto

**Colmado La Esperanza**, Moca, provincia Espaillat. Vende al contado y a crédito, y por unidad
y por peso.

| Dato maestro | Exigido | Cargado | Composición |
|---|---:|---:|---|
| Productos | 60 | **64** | 20 por peso · 11 exentos de ITBIS · 5 sin inventario |
| Clientes | 40 | **40** | 29 con crédito · 6 con crédito fiscal y RNC · 6 de contado |
| Proveedores | 12 | **12** | 8 con RNC · 2 con cédula · 2 sin documento |
| Usuarios | 5 | **5** | Administrador, 2 cajeros distintos, almacén, supervisor |

Los datos se validaron por programa contra las reglas del propio sistema: **las 26 cédulas
cumplen el formato oficial, los 8 RNC tienen nueve dígitos, y todo cliente con comprobante de
crédito fiscal posee RNC.**

Cifras de apertura: **RD$ 341,519.00** de inventario al costo, con un **margen promedio del
catálogo de 25.2 %** y **RD$ 282,100.00** de crédito otorgado a clientes.

### 4.5 Calendario del piloto

Veinte jornadas, de lunes a sábado, del **8 al 30 de junio**. La ventana se eligió para que la
séptima jornada caiga en **quincena** —día de pago en el país— y la vigésima sea **fin de mes
real**. Ritmo desigual: lunes flojos, viernes fuertes, sábados como día de mayor venta.

**378 facturas** y **583 operaciones encadenadas**, por encima del mínimo de 500.

---

## 5. Defectos encontrados

Siete en total. Ninguno se buscó a partir de una lista previa: todos salieron de la auditoría
o de ejecutar las verificaciones.

| # | Defecto | Módulo | Dueño | Severidad | ¿Corregido? | ¿En `main`? |
|---|---|---|---|---|:---:|:---:|
| **D-01** | El usuario que registra cobros y pagos estaba escrito a mano | Clientes y Compras | Dionis | Alta | ✅ | ⏳ PR #4 |
| **R-02** | La fecha de cobros y pagos no se podía capturar | Clientes y Compras | Dionis | Baja | ✅ | ⏳ PR #4 |
| **H-06** | La devolución a proveedor no ajustaba la deuda | Compras | Dionis | Media | ✅ | ⏳ PR #4 |
| **D-03** | Los movimientos de una venta no guardan el Id de su factura | Ventas | Jeison | Alta | ❌ | ❌ |
| **D-02** | El registro de usuarios es público y no asigna rol | Plataforma | Jeison | Alta | ❌ | ❌ |
| **H-07** | Editar un producto pisa el costo promedio ponderado | Inventario | Jeison | Media | ❌ | ❌ |
| **H-08** | `CLAUDE.md` afirma que el e-CF no existe, y sí existe | Documentación | Jeison | Baja | ❌ | ❌ |

### 5.1 Los dos más graves, explicados para el documento

**D-01 — La firma de los cobros y pagos era falsa.**
Las pantallas de cobro a cliente y de pago a proveedor pasaban el nombre de un desarrollador
escrito como texto fijo, en lugar del usuario de la sesión. Todo cobro y todo pago del comercio
quedaba firmado con ese nombre, sin importar quién lo registrara. Son las dos únicas
operaciones donde se mueve efectivo contra un tercero.

*Su valor didáctico para el informe:* **un arnés de pruebas que invocara los servicios
correctamente jamás lo habría detectado**, porque el defecto vivía en la capa de presentación.
Es el argumento más fuerte a favor de exigir pruebas ejecutadas desde la interfaz.

**D-03 — Los movimientos de inventario de una venta no saben de qué factura vienen.**
Al emitir una factura, el sistema le asigna al movimiento de salida el identificador del
documento, pero lo hace **antes de guardarlo**, cuando ese identificador todavía vale cero.

La prueba es la asimetría, verificable en los datos:

| Origen del movimiento | Identificador guardado | |
|---|---|---|
| Recepción de compra | 1, 2 | correcto |
| Anulación de factura | 4 | correcto |
| **Emisión de factura** | **0, 0, 0, 0, 0** | **defecto** |

El mismo patrón funciona en los otros dos casos porque allí el documento ya existe. Solo falla
al emitir.

*Consecuencia para el comerciante:* no se puede responder con una consulta a la pregunta «¿por
qué bajó el arroz el martes?». Habría que leer el texto del campo de motivo.

*Cómo apareció:* no lo vio la auditoría de código. **Lo destapó ejecutar las verificaciones de
integridad**, que es exactamente para lo que existen.

### 5.2 Un hallazgo metodológico que conviene contar

Al ejecutar el script de verificaciones por primera vez, **una de las diecisiete daba un
aprobado falso**. La verificación nº 12 —«la anulación repone exactamente lo que descontó»— se
apoyaba en el identificador de la factura para emparejar salidas con reposiciones; como ese
identificador venía en cero por el defecto D-03, la consulta no evaluaba ninguna fila y
devolvía «cero descuadres».

Se reescribió para emparejar por el número de comprobante fiscal, y entonces sí evaluó
correctamente. **Una verificación que aprueba porque no mira nada es peor que una que falla**,
y merece constar en el informe como parte del método.

---

## 6. Lo que falta, y por qué

### 6.1 Estado del Capítulo V

| Sección | Estado | Qué necesita |
|---|---|---|
| **5.1 Plan de pruebas** | ✅ **Redactada** | Nada. Está lista en `Capitulo-V-5.1-Plan-de-Pruebas.md` |
| 5.2 Implementación del caso piloto | ⬜ | Ejecutar las 20 jornadas |
| 5.3 Resultados operativos | ⬜ | Ejecutar el piloto y las 17 verificaciones |
| 5.4 Resultados financieros | ⬜ | Ejecutar el piloto |
| 5.5 Evaluación de la optimización | ⬜ | Medir tiempos frente a la gestión manual |

**La sección 5.1 se puede pasar al documento tal cual.** Está escrita en prosa académica, con
el mismo estilo del Capítulo III, numerada de 5.1.1 a 5.1.10, con cinco tablas rotuladas.

Las secciones 5.2 a 5.5 **no se redactaron a propósito**: son resultados medidos, no
redactables. Escribirlas sin ejecutar el piloto sería inventar cifras.

### 6.2 Tres condiciones antes de ejecutar el piloto

1. **Que entre el PR #4** (corrección de Dionis). Sin él, los 52 cobros y 26 pagos del piloto
   quedan firmados con un nombre fijo y hay que repetirlo entero.
2. **Que Jeison corrija D-03.** Si no, la verificación nº 4 falla las veinte jornadas seguidas
   siempre por la misma causa, y ensucia el informe con un descuadre repetido que no aporta
   información nueva.
3. **Crear la base de datos desechable** del piloto y congelar la versión.

### 6.3 Trabajo técnico pendiente

Construir el arnés que cargue los datos y ejecute las veinte jornadas, más la automatización
de la interfaz. Es lo único que separa al proyecto de tener las secciones 5.2 a 5.5.

---

## 7. Advertencias para quien redacte

**No presentar la multisucursal ni la certificación fiscal como pendientes.** Están excluidas
por escrito en el anteproyecto: son decisiones de alcance y se defienden como tales. Mezclarlas
con las ausencias reales debilita el argumento.

**No presentar la devolución a proveedor como funcionalidad entregada.** Su regla de negocio
existe y ahora está corregida, pero **no tiene tabla ni pantalla**: no se puede ejecutar. El
propio autor lo dejó documentado en el código.

**Sí presentar la ausencia de defectos como algo que no se buscó.** El criterio de conclusión
del piloto no es que no haya defectos, sino que todos los que haya estén documentados y sean
reproducibles. Un piloto que no encuentra nada no ha probado nada, y conviene decirlo.

**Mencionar que el control de acceso pasó de cero a veintinueve pantallas** entre el 25 y el 28
de julio. Es una mejora real y medible del período, y da una historia de progreso que el
capítulo puede aprovechar.

**Corregir `CLAUDE.md` antes de entregar.** Afirma que el comprobante fiscal electrónico no
existe, y sí existe. Es una contradicción del propio equipo contra su propio código, y si el
asesor la detecta resta credibilidad al resto.
