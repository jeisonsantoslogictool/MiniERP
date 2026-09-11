# E2 — Plan de pruebas del piloto simulado

Piloto de validación del Mini ERP · Capítulo V · Trabajo Final de Grado Grupo 6
**Materia prima de la sección 5.1 del informe.**

Depende de: [E0 — Matriz de capacidades](E0-Matriz-de-Capacidades.md) ·
[E1 — Entorno y línea base](E1-Entorno-y-Linea-Base.md)

---

## 1. Objetivo

Determinar si el Mini ERP **aguanta veinte días de operación real de un colmado dominicano sin
perder ni descuadrar un solo peso ni una sola unidad de inventario**, y decirlo con evidencia
reproducible, incluidos los casos en que no aguante.

No es una demostración. El criterio de éxito de este trabajo **no es que todo pase**: es que
todo lo que se afirme se pueda repetir ejecutando algo.

### 1.1 Las siete preguntas que el informe final debe responder

1. ¿Soporta 20 días sin descuadrar? Sí o no, y cuántos de los 17 cuadres fallaron.
2. ¿Cuántas capacidades están presentes, cuántas parcialmente y cuántas ausentes?
3. ¿Qué defectos impedirían instalarlo en un comercio mañana?
4. ¿El rendimiento aguanta un mostrador con cola?
5. ¿El control de acceso protege lo que debe?
6. ¿Qué evidencia hay de optimización frente a la gestión manual?
7. **Veredicto:** apto · apto con reservas · no apto todavía.

---

## 2. Alcance

### 2.1 Versión bajo prueba

`origin/main` a partir del commit `af4e7c3` (28/07/2026), que ya integra el núcleo de permisos,
las pantallas de usuarios, y el gateo de los cinco módulos.

**Se prueba `main` y no una rama** porque es lo que el grupo va a sustentar. Cualquier
corrección que se integre durante el piloto obliga a anotar el commit exacto en el acta del día
y a repetir los cuadres afectados.

### 2.2 Qué queda fuera, y por qué

De las 55 capacidades auditadas en E0: **28 existen, 12 existen parcialmente y 15 no existen.**
El piloto **no diseña pruebas para lo que no existe**, pero lo reporta con su evidencia.

Quedan fuera por **ausencia** (se reportan como hallazgo):

- Caja completa: apertura, cierre, arqueo, corte Z, entradas y salidas libres de efectivo.
- Medios de pago distintos de contado y crédito: tarjeta, transferencia, pagos mixtos.
- Descuentos, por línea y por factura.
- Devolución parcial de venta y reembolsos.
- Devolución a proveedor: **la regla existe pero no hay tabla ni pantalla**.
- Respaldo y restauración.
- Multi-caja y multi-terminal.

Quedan fuera por **limitación declarada del proyecto** (no son hallazgos):

- Multi-sucursal y transferencias entre almacenes — excluidas por escrito en el anteproyecto.
- Certificación fiscal ante la DGII: el e-CF se genera simulado y rotulado como tal.

### 2.3 Consecuencia sobre el calendario

El día de **devolución de mercancía al proveedor** que pedía el diseño original **no se puede
ejecutar**. Se sustituye por una salida manual de inventario, y **se declara en el acta del día
como sustitución forzada**, no como operación equivalente.

---

## 3. Estrategia: tres vías, y la interfaz es obligatoria

| Vía | Qué cubre | Volumen |
|---|---|---|
| **Interfaz (Playwright/Chromium)** | Punto de venta, editores, impresión, e-CF, todas las pantallas de cada módulo, la matriz rol × pantalla y **toda la evidencia visual** | **Mínimo 40 operaciones completas** + las 32 rutas de la Fase 8 |
| **Arnés de integración (C#)** | El volumen de las 500+ operaciones encadenadas de los 20 días, invocando los servicios de la capa de aplicación | El resto |
| **SQL de verificación** | Los 17 cuadres de integridad | Cada día y al cierre |

### 3.1 Por qué la interfaz no es opcional

**Los defectos más graves encontrados hasta ahora viven en la pantalla, no en el servicio.**
Un arnés que invoque los servicios correctamente **jamás los habría detectado**:

- **D-01** — el usuario escrito a mano en cobros y pagos estaba en el `.razor`. Un arnés que
  llama a `RegistrarCobroAsync` pasando el usuario correcto no ve nada raro.
- **D-02** — el registro público solo se detecta navegando sin sesión.
- Las pantallas sin política se prueban **tecleando la dirección en la barra**, que es
  exactamente como un empleado se saltaría el menú.

Además, **los casos límite de la Fase 4 se ejecutan obligatoriamente por interfaz**: parte del
veredicto es si el mensaje que recibe el comerciante es comprensible o es un volcado de pila, y
eso no se puede juzgar desde un servicio.

### 3.2 Dónde vive el código de pruebas

Proyecto nuevo bajo `tests/`. **Nunca dentro de `src/`.** No se modifica una sola línea de
lógica de negocio para que una prueba pase: si algo falla, se reporta.

---

## 4. El comercio piloto

**Colmado La Esperanza**, Moca, provincia Espaillat. Vende al contado y a crédito (fiado), por
unidad y por peso. Un solo local, una caja, atendido por el dueño y dos cajeros.

| Dato maestro | Cantidad | Requisitos que debe cumplir |
|---|---:|---|
| Usuarios | 5 | 1 administrador, 2 cajeros, 1 de almacén, 1 supervisor |
| Categorías de producto | 10 | Las sembradas por el sistema |
| Productos | 60 | ≥12 por peso · ≥5 exentos de ITBIS · ≥5 sin manejo de inventario · existencias mínimas variadas |
| Proveedores | 12 | Mezcla de RNC, cédula y sin documento |
| Clientes | 40 | ≥15 con límite de crédito · ≥5 con crédito fiscal y RNC · varios de contado sin documento |
| Secuencias NCF | 2 rangos | Uno normal y **uno corto a propósito, para forzar el agotamiento** |

El detalle completo va en [E3](E3-Datos-Maestros.md).

**Nota sobre los usuarios:** al no existir pantalla de alta en `main` cuando se diseñó esto, y
existir ahora `/usuarios` con permisos, **los cinco usuarios se crean por la interfaz**, lo que
de paso valida esa pantalla. Ver restricción R-05 de E0.

---

## 5. Calendario de los 20 días

**Ventana simulada: lunes 8 al martes 30 de junio de 2026**, de lunes a sábado, sin domingos.
Elegida para que **el día 7 caiga en quincena (15 de junio)** y **el día 20 sea fin de mes
real (30 de junio)**, de modo que el cierre del período sea un cierre de mes de verdad.

Ritmo: lunes flojo, viernes fuerte, **sábado el más fuerte**, y repunte de cobros en las dos
quincenas, que es como se comporta un colmado dominicano.

| Día | Fecha | Ventas | Carácter de la jornada |
|---:|---|---:|---|
| 1 | lun 08/06 | 13 | Apertura del piloto. Alta de maestros, 3 órdenes de compra |
| 2 | mar 09/06 | 15 | Recepción de las compras del día 1. Primeros fiados |
| 3 | mié 10/06 | 16 | Jornada normal. Alta de 4 clientes con crédito |
| 4 | jue 11/06 | 17 | Compras a crédito. Primeros egresos operativos |
| 5 | vie 12/06 | 23 | **Día fuerte.** Sube el fiado antes del fin de semana |
| 6 | sáb 13/06 | 30 | **El día más fuerte.** Pico de mediodía y de atardecer |
| 7 | lun 15/06 | 20 | **QUINCENA.** Pico de cobros de fiado. Pagos a proveedores |
| 8 | mar 16/06 | 16 | Resaca de quincena. Más cobros |
| 9 | mié 17/06 | 15 | Jornada normal. Cambios de precio |
| 10 | jue 18/06 | 17 | **Sustitución declarada:** el día de devolución a proveedor pasa a ser salida manual de inventario, porque la capacidad no existe |
| 11 | vie 19/06 | 24 | **CORTE DE ENERGÍA** simulado a mitad de una venta ya cobrada |
| 12 | sáb 20/06 | 31 | Día fuerte. Se acerca el agotamiento de la secuencia NCF corta |
| 13 | lun 22/06 | 12 | Día flojo. Anulaciones de facturas de la semana anterior |
| 14 | mar 23/06 | 14 | **Agotamiento de la secuencia NCF corta** a mitad de jornada |
| 15 | mié 24/06 | 15 | **INVENTARIO FÍSICO.** Ajustes ±, mermas con motivo |
| 16 | jue 25/06 | 16 | Reabastecimiento tras el conteo. Compras nuevas |
| 17 | vie 26/06 | 22 | Día fuerte |
| 18 | sáb 27/06 | 29 | Día fuerte |
| 19 | lun 29/06 | 14 | Preparación del cierre. Cobros de morosos |
| 20 | mar 30/06 | 19 | **QUINCENA Y CIERRE DE MES.** Los cinco reportes, todos los cuadres |
| | **Total** | **378** | |

**Dos jornadas se ejecutan en tiempo real, sin desplazar ninguna fecha** (los días 6 y 15), para
comparar el comportamiento con el reloj natural frente al simulado. Se declara en el informe.

---

## 6. Volumen de operaciones

Mínimo exigido: 500 operaciones **encadenadas entre sí**. Planificado: 583.

| Operación | Mínimo | Planificado | Condiciones |
|---|---:|---:|---|
| Facturas de venta | 360 | **378** | 1 a 12 líneas; **≥25 % a crédito** (≈95) |
| Órdenes de compra creadas y recibidas | 30 | **30** | ≥8 a crédito; **recepción en día distinto al de emisión** |
| Cobros a clientes | 50 | **52** | Parciales y totales; ≥3 que salden por completo |
| Pagos a proveedores | 25 | **26** | Parciales y totales |
| Egresos operativos | 40 | **42** | Repartidos entre las 5 categorías |
| Ajustes y mermas | 25 | **25** | Positivos, negativos y mermas con motivo |
| Anulaciones de factura | 15 | **15** | **≥5 de facturas a crédito ya abonadas parcialmente** |
| Altas y cambios de maestros | 40 | **15** | Ya cubiertos por los 117 maestros de E3 |

### 6.1 Regla de encadenamiento

**Un conjunto de datos desconectados no valida nada.** Por tanto:

- Toda mercancía vendida **entró antes por una compra recibida**.
- Todo fiado cobrado **procede de una factura a crédito concreta**.
- Toda merma afecta a **un producto con existencia real** en ese momento.
- Toda anulación recae sobre **una factura emitida en un día anterior del piloto**.

---

## 7. Cómo se simulan los veinte días

El sistema estampa las fechas con el reloj del servidor en horario universal. La técnica es
mixta y se declara como tal: **esto es una simulación, no un piloto en producción.**

### 7.1 Fechas que el dominio deja fijar

Se usan directamente:

- `Compra.Fecha` y `Compra.FechaRecepcion`
- `Egreso.Fecha`
- **`Cobro.Fecha` y `Pago.Fecha`** — capturables desde la corrección `3c240c0` de Dionis.
  **Requiere que esa corrección esté en `main`**; si no lo está al arrancar el piloto, vuelven
  al grupo siguiente y se anota en el acta.

### 7.2 Fechas que fija el sistema

`Factura.Fecha` y `MovimientoInventario.Fecha` los pone el servidor. Para esas:

1. Se ejecuta la jornada completa.
2. **Inmediatamente después de cerrar cada día simulado** se aplica un desplazamiento
   controlado por sentencia SQL, dejando registrado el script exacto.
3. **Se verifica que el desplazamiento no alteró ningún saldo ni ninguna secuencia**, ejecutando
   los cuadres 1 a 8 antes y después y comparando.

### 7.3 Limitaciones declaradas de la técnica

- El desplazamiento toca la columna de fecha, **nunca un saldo ni un correlativo**.
- Los tiempos de la Fase 7 se miden **sin desplazar**, sobre la base ya cargada.
- **Riesgo de zona horaria (H-03):** el sistema guarda en UTC y el comercio opera en UTC−4. Las
  ventanas de cada día se fijan **en UTC** y se documenta la conversión. Una venta de las 9 de
  la noche cae en el día siguiente del panel: eso **se prueba a propósito** el día 6.

---

## 8. Criterios de entrada y de salida

### 8.1 Para empezar (todos obligatorios)

| # | Criterio | Estado |
|---|---|---|
| E-1 | E0 completa, con el alcance real decidido | ✅ |
| E-2 | Entorno documentado y reproducible | ✅ |
| E-3 | Línea base verde: compila sin errores y las pruebas pasan | ✅ 180/180 |
| E-4 | Base `MiniERP_Piloto` creada, separada de la de desarrollo | ⬜ |
| E-5 | Datos maestros cargados y verificados (E3) | ⬜ |
| E-6 | Versión congelada: commit anotado | ⬜ |

### 8.2 Para dar el piloto por terminado

| # | Criterio |
|---|---|
| S-1 | Los 20 días ejecutados, con sus 20 guiones y sus 20 actas |
| S-2 | ≥500 operaciones encadenadas registradas |
| S-3 | Los **17 cuadres** ejecutados al cierre de cada día y al final |
| S-4 | ≥40 operaciones completas ejecutadas **por interfaz** |
| S-5 | Todos los casos límite de la Fase 4 intentados o declarados no aplicables con su motivo |
| S-6 | Matriz rol × pantalla completa, celda por celda |
| S-7 | Mediciones de rendimiento con su metodología |
| S-8 | Cada defecto reportado con pasos de reproducción desde cero |
| S-9 | Veredicto emitido y justificado |

**No es criterio de salida que no haya defectos.** Es criterio de salida que todos los que haya
estén documentados y sean reproducibles.

---

## 9. Riesgos del piloto

| # | Riesgo | Impacto | Mitigación |
|---|---|---|---|
| RG-1 | El desplazamiento de fechas altera un saldo | Invalida los cuadres | Ejecutar cuadres 1-8 antes y después de cada desplazamiento y comparar |
| RG-2 | La zona horaria mueve operaciones de día | Descuadra los cierres diarios | Fijar ventanas en UTC; probarlo a propósito el día 6 |
| RG-3 | Se integra una corrección a mitad del piloto | Los días previos quedan sobre otra versión | Anotar el commit en cada acta y repetir los cuadres afectados |
| RG-4 | El hardware no es el del comercio | Los tiempos son optimistas | Declararlo; leer la Fase 7 como cota inferior |
| RG-5 | Falta la corrección de cobros y pagos en `main` | Los cuadres 8 y 16 pasan siendo falsos | **Verificar antes de empezar**; si no está, la fecha vuelve a ser la del registro |
| RG-6 | La secuencia NCF corta se agota antes de lo previsto | Se pierde el caso de prueba | Dimensionar el rango con el volumen del calendario y verificarlo el día 12 |
| RG-7 | El arnés enmascara defectos de pantalla | Se reportan menos defectos de los reales | Obligación de las 40 operaciones por interfaz (§3.1) |

---

## 10. Trazabilidad de los entregables

| Fase | Produce | Alimenta |
|---|---|---|
| 0 · Reconocimiento | E0 | 5.1, 5.5 |
| 1 · Entorno | E1 | 5.1, 5.2 |
| 2 · Diseño | **E2 (este)**, E3 | **5.1** |
| 3 · Ejecución | E4, E5, E10 | **5.2, 5.3** |
| 4 · Casos límite | E5, E7 | 5.3 |
| 5 · Integridad | E6 | **5.3, 5.4** |
| 6 · Concurrencia | E5, E7 | 5.3 |
| 7 · Rendimiento | E8 | 5.3, 5.5 |
| 8 · Seguridad | E9, E7 | 5.3 |
| — · Cierre | E11 | **5.3, 5.4, 5.5** |
