# CAPÍTULO V: PRUEBAS, CASO PILOTO Y RESULTADOS

## 5.1. Plan de pruebas

El presente capítulo somete el sistema construido a una validación deliberadamente exigente:
no busca demostrar que funciona, sino **averiguar si soporta el uso real de un colmado y
decirlo con evidencia**, incluidos los casos en que no lo soporte. Esa distinción gobierna
todo lo que sigue. Un informe que afirme que todo funciona sin haber intentado romper nada no
es una prueba: es una demostración comercial, y no sustenta un trabajo de grado.

### 5.1.1. Objetivo y preguntas de investigación

El objetivo del plan es determinar si el Mini ERP **resiste veinte días de operación continua
de un microcomercio minorista sin perder ni descuadrar un solo peso ni una sola unidad de
inventario**, y cuantificar la optimización que aporta frente a la gestión manual descrita en
el planteamiento del problema.

El plan se diseñó para responder siete preguntas concretas, que son las que el informe final
debe contestar sin ambigüedad:

1. ¿Soporta veinte días de operación sin descuadrar? Con el número exacto de verificaciones
   de integridad que fallaron.
2. ¿Cuántas de las capacidades que una tienda necesita están presentes, cuántas parcialmente
   y cuántas ausentes?
3. ¿Qué defectos impedirían instalar el sistema en un comercio mañana?
4. ¿El rendimiento es suficiente para un mostrador con cola de clientes?
5. ¿El control de acceso protege lo que debe proteger?
6. ¿Qué evidencia medible hay de optimización operativa y financiera?
7. ¿Cuál es el veredicto: apto para el piloto real, apto con reservas, o no apto todavía?

### 5.1.2. Estrategia general

La validación se estructuró en nueve fases, de las cuales la primera condiciona a todas las
demás.

**Fase de reconocimiento.** Antes de diseñar un solo caso de prueba se auditó el código para
levantar el inventario real de capacidades. La razón es metodológica: **no tiene sentido
diseñar pruebas para funciones que no existen**, y confundir una función ausente con una
función defectuosa invalidaría las conclusiones. Se auditaron 55 capacidades propias de un
comercio minorista, una por una, exigiendo para cada veredicto la cita del archivo y la clase
o método, o de la pantalla y su ruta.

Para evitar el sesgo de confirmación, toda capacidad que un revisor declaró ausente o
incompleta pasó por **una segunda revisión independiente cuyo único encargo era demostrar que
la primera se había equivocado**, buscando sinónimos, términos en inglés y en español, y
rastros en todas las capas incluidas las migraciones y las pruebas. Solo se dieron por buenas
las ausencias que sobrevivieron a esa segunda pasada, y de cada una se registraron los
términos de búsqueda empleados.

**Fases de ejecución.** El sistema se somete después a un piloto simulado de veinte días de
operación de un colmado, con casos límite deliberados, verificaciones de integridad de los
datos, pruebas de concurrencia, medición de rendimiento y una auditoría de control de acceso
pantalla por pantalla.

### 5.1.3. Resultado del reconocimiento y su efecto sobre el alcance

La auditoría arrojó el siguiente reparto:

**Tabla 5.1.** *Capacidades auditadas frente a capacidades reales*

| Estado | Capacidades | Proporción |
|---|---:|---:|
| Existe | 28 | 51 % |
| Existe parcialmente | 12 | 22 % |
| No existe | 15 | 27 % |
| **Total** | **55** | **100 %** |

*Fuente: elaboración propia.*

Ninguna de las 27 capacidades declaradas ausentes o incompletas cambió de veredicto al
someterse a la revisión adversarial.

La lectura de esta tabla no es que el sistema esté a mitad de camino, sino algo más preciso:
**lo que está construido es el ciclo documental completo** —comprar, recibir, vender,
facturar, fiar, cobrar, pagar, gastar y reportar—, **y lo que falta es la operación del
mostrador**: la caja con su apertura y su arqueo, los medios de pago distintos del efectivo y
el crédito, los descuentos y las devoluciones al cliente. El sistema sabe llevar la
contabilidad de un colmado, pero todavía no sabe atender su mostrador.

Esa distinción determina el alcance del piloto. Quedan fuera por ausencia, y se reportan como
hallazgo: la caja completa, los pagos con tarjeta y transferencia, los pagos combinados, los
descuentos, la devolución parcial de venta, los reembolsos, la devolución a proveedor y el
respaldo de la información. Quedan fuera por **limitación declarada del proyecto**, y por
tanto no constituyen hallazgo: la operación multisucursal y las transferencias entre
almacenes, excluidas por escrito en el anteproyecto, y la certificación fiscal ante la DGII,
cuyo comprobante electrónico se genera simulado y rotulado como tal.

### 5.1.4. Estrategia de ejecución en tres vías

Las pruebas se ejecutan por tres caminos complementarios, y ninguno sustituye a los otros.

**Tabla 5.2.** *Vías de ejecución de las pruebas*

| Vía | Qué cubre | Volumen |
|---|---|---|
| Interfaz de usuario automatizada | Punto de venta, editores, impresión, comprobante electrónico, todas las pantallas de cada módulo y la matriz de control de acceso | Mínimo 40 operaciones completas y 32 rutas |
| Arnés de integración | El volumen de las operaciones encadenadas de los veinte días, invocando los servicios de la capa de aplicación | El resto |
| Consultas de verificación | Las diecisiete comprobaciones de integridad | En cada cierre de jornada |

*Fuente: elaboración propia.*

**La ejecución por interfaz no es opcional, y la experiencia lo confirmó.** Los defectos más
graves detectados hasta el momento residen en la capa de presentación y **son invisibles para
un arnés que invoque correctamente los servicios**. Un ejemplo concreto: en las pantallas de
cobro a cliente y de pago a proveedor, el identificador del usuario responsable estaba escrito
como texto fijo en el componente, de modo que todas esas operaciones quedaban firmadas con el
mismo nombre. Un arnés que llamara al servicio pasándole el usuario correcto habría dado la
prueba por superada. Análogamente, la accesibilidad pública de la pantalla de registro de
usuarios solo se detecta navegando a la dirección sin sesión iniciada.

Por el mismo motivo, **los casos límite se ejecutan obligatoriamente por interfaz**: parte del
veredicto es determinar si el mensaje que recibe el comerciante es comprensible o es una
excepción sin tratar, y eso no puede juzgarse desde la capa de servicios.

Todo el código de pruebas reside en un proyecto separado. **No se modifica ninguna línea de
lógica de negocio para que una prueba pase**: cuando algo falla, se reporta.

### 5.1.5. El comercio piloto y sus datos

El piloto simula el **Colmado La Esperanza**, ubicado en Moca, provincia Espaillat, que vende
al contado y a crédito, y por unidad y por peso. Esas cuatro características son las que el
anteproyecto invoca para justificar el modelo de datos, de manera que el piloto las ejercita
todas.

**Tabla 5.3.** *Datos maestros del comercio piloto*

| Dato maestro | Exigido | Cargado | Composición relevante |
|---|---:|---:|---|
| Productos | 60 | 64 | 20 vendidos por peso · 11 exentos de ITBIS · 5 sin manejo de inventario |
| Clientes | 40 | 40 | 29 con límite de crédito · 6 con crédito fiscal y RNC · 6 de contado sin documento |
| Proveedores | 12 | 12 | 8 con RNC · 2 con cédula · 2 sin documento |
| Usuarios | 5 | 5 | Administrador, dos cajeros con permisos distintos, almacén y supervisor |

*Fuente: elaboración propia.*

Los datos no se generaron al azar sino que se curaron con precios y denominaciones reales del
mercado dominicano, y después **se validaron por programa contra las propias reglas del
dominio**: las veintiséis cédulas cumplen el formato oficial, los ocho RNC tienen nueve
dígitos y todo cliente con comprobante de crédito fiscal posee RNC, que es la regla que el
sistema exige. El inventario de apertura asciende a RD$ 341,519.00 al costo, con un margen
promedio del catálogo de 25.2 %, cifras coherentes con las de un colmado real. Se verificó
además que ningún producto se vende al costo o por debajo, de modo que cualquier margen
negativo que aparezca durante el piloto sea atribuible a un defecto y no a los datos de
entrada.

Los dos cajeros **no tienen los mismos permisos deliberadamente**: uno puede cobrar fiado y el
otro no. Es el caso que permite validar el control de acceso a nivel de acción y no solo de
pantalla. El supervisor, por su parte, puede consultar los reportes financieros pero no
registrar gastos, que es exactamente la definición del rol en el sistema.

### 5.1.6. Calendario del piloto

El piloto cubre veinte jornadas, de lunes a sábado sin domingos, entre el 8 y el 30 de junio.
La ventana se eligió de modo que **la séptima jornada coincida con la quincena** —fecha de
pago en el país, cuando repuntan los cobros de fiado— y que **la vigésima sea el último día del
mes**, para que el cierre del período sea un cierre mensual verdadero.

El ritmo es deliberadamente desigual, replicando el comportamiento de un colmado: lunes flojos,
viernes fuertes y **sábados como día de mayor venta**. El plan contempla 378 facturas y un
total de 583 operaciones encadenadas entre sí, por encima del mínimo de 500 establecido.

**Tabla 5.4.** *Jornadas singulares del calendario*

| Jornada | Fecha | Situación que se provoca |
|---:|---|---|
| 7 | 15 de junio | Quincena: pico de cobros de fiado y pagos a proveedores |
| 11 | 19 de junio | Interrupción de energía a mitad de una venta ya cobrada |
| 14 | 23 de junio | Agotamiento de la secuencia de comprobantes durante la jornada |
| 15 | 24 de junio | Inventario físico: ajustes y mermas con motivo |
| 20 | 30 de junio | Quincena y cierre de mes: los cinco reportes y las diecisiete verificaciones |

*Fuente: elaboración propia.*

El diseño original contemplaba además una jornada de devolución de mercancía al proveedor.
**Esa jornada no puede ejecutarse** porque la funcionalidad, aunque tiene su regla de negocio
escrita, carece de persistencia y de pantalla. Se sustituye por una salida manual de
inventario y **la sustitución se declara expresamente**, en lugar de presentarla como
operación equivalente.

Merece explicarse una decisión de diseño del calendario. El sistema siembra rangos de cinco
mil comprobantes; con 378 ventas ninguno se agotaría jamás, y el caso «agotar la secuencia a
mitad de una venta» quedaría sin probar. Por ello se registra deliberadamente un rango corto
de 250 comprobantes: contando el calendario, al cierre de la decimotercera jornada se han
emitido 249 facturas, de modo que **el agotamiento ocurre necesariamente durante la
decimocuarta**. El rango se dimensionó a partir del calendario, no al revés.

### 5.1.7. Técnica de simulación temporal y sus límites

El sistema estampa las fechas con el reloj del servidor en horario universal, de manera que
simular veinte días exige una técnica explícita, que se declara con sus limitaciones porque
**esto es una simulación y no un piloto en producción**.

Para las entidades cuya fecha permite fijar el propio dominio —compras, recepciones, gastos,
cobros y pagos— se emplea directamente ese campo. Para las que fija el sistema —facturas y
movimientos de inventario— se aplica un desplazamiento controlado por sentencia SQL
inmediatamente después de cerrar cada jornada, dejando registrado el script exacto. **El
desplazamiento afecta únicamente a la columna de fecha, nunca a un saldo ni a un correlativo**,
y se comprueba ejecutando las ocho primeras verificaciones de integridad antes y después de
cada desplazamiento y comparando los resultados.

Adicionalmente, **dos jornadas se ejecutan en tiempo real sin desplazar ninguna fecha**, para
poder comparar el comportamiento con el reloj natural frente al simulado.

Se identificó un riesgo asociado que se prueba a propósito: el sistema almacena las fechas en
horario universal mientras que el país opera cuatro horas por detrás, de modo que **una venta
realizada después de las ocho de la noche se contabiliza en el día siguiente** en el panel de
indicadores. Un colmado vende hasta pasadas las nueve, así que el fenómeno es real y no
teórico.

### 5.1.8. Verificaciones de integridad

El núcleo del plan son diecisiete verificaciones que deben cuadrar **al centavo y a la
unidad** al cierre de cada jornada y al final del período. Cualquier diferencia, por mínima
que sea, se clasifica como defecto crítico. Entre ellas: que la existencia de cada producto
sea igual a la suma algebraica de sus movimientos; que ninguna existencia haya sido negativa
en ningún momento; que no exista un solo comprobante fiscal duplicado; que el balance de cada
cliente y de cada proveedor se corresponda con sus documentos; que la cadena de saldos de
cobros y pagos sea continua; que el estado de resultados y el flujo de caja, calculados de
forma independiente mediante consultas directas, coincidan con los que muestra el sistema.

Las diecisiete se entregan como **script ejecutable**, de modo que el jurado pueda repetirlas.
El script se validó ejecutándolo contra una base de datos con operaciones reales, lo que
permitió depurarlo antes del piloto y, en el proceso, **detectar un defecto que la auditoría
de código no había revelado**: los movimientos de inventario originados por una venta no
conservan el identificador de la factura que los produjo, porque este se asigna antes de que
el documento se persista. La consecuencia práctica es que no se puede rastrear, mediante una
consulta, qué factura provocó una salida de mercancía.

### 5.1.9. Criterios de entrada y de salida

**Tabla 5.5.** *Criterios de aceptación del plan*

| Para iniciar el piloto | Para darlo por concluido |
|---|---|
| Auditoría de capacidades completa | Las veinte jornadas ejecutadas, con guion previo y acta posterior |
| Entorno documentado y reproducible | Más de 500 operaciones encadenadas registradas |
| Línea base sin errores de compilación ni pruebas fallidas | Las diecisiete verificaciones ejecutadas en cada cierre |
| Base de datos desechable creada, separada de la de desarrollo | Al menos 40 operaciones completas ejecutadas por interfaz |
| Datos maestros cargados y verificados | Casos límite intentados o declarados no aplicables con su motivo |
| Versión congelada, con el identificador de la revisión anotado | Matriz de control de acceso completa, celda por celda |
| | Cada defecto reportado con pasos de reproducción desde cero |
| | Veredicto emitido y justificado |

*Fuente: elaboración propia.*

Conviene subrayar que **la ausencia de defectos no figura entre los criterios de conclusión**.
El criterio es que todos los defectos encontrados estén documentados y sean reproducibles. Un
piloto que no encuentra nada no ha probado nada.

### 5.1.10. Riesgos identificados

Se registraron siete riesgos con su mitigación. Los tres de mayor impacto son: que el
desplazamiento de fechas altere algún saldo, mitigado ejecutando las verificaciones antes y
después de cada desplazamiento; que el desfase horario mueva operaciones de una jornada a
otra, mitigado fijando las ventanas en horario universal y probando el fenómeno
deliberadamente; y que el arnés de integración enmascare defectos de la capa de presentación,
mitigado mediante la obligación de ejecutar un mínimo de operaciones por interfaz.

Se documenta asimismo que **el equipo de medición es sensiblemente más potente que el de un
colmado**, por lo que los tiempos de respuesta deben leerse como cota inferior optimista: en
el equipo real serán iguales o peores.

---

> **Nota sobre el estado de este capítulo.** La sección 5.1 está completa y se sustenta en
> trabajo ya ejecutado y verificable: la auditoría de las 55 capacidades, la línea base del
> entorno, los datos maestros validados por programa y el script de verificaciones probado
> contra una base real. **Las secciones 5.2 a 5.5 requieren la ejecución del piloto** y no se
> redactarán hasta disponer de sus resultados medidos, conforme al principio de que ninguna
> cifra del informe puede escribirse sin haberse medido.
