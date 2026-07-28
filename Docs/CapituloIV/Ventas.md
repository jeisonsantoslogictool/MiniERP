# Capítulo IV — Módulo de Ventas (Punto de venta y comprobantes fiscales)

Trabajo Final de Grado · Grupo 6 · Universidad Dominicana O&M
Autor del módulo: **Jeison Luis Santos** · rama `jeison/pos` · construido en las fases F2
(punto de venta) y F3 (comprobantes electrónicos).

> Esta sección documenta el módulo de **Ventas**: el punto de venta, la emisión de la
> factura con su comprobante fiscal, la impresión, la anulación, la administración de las
> secuencias de NCF y la generación **simulada** del comprobante fiscal electrónico (e-CF).
> Es la cadena crítica del sistema y el módulo con las decisiones técnicas más delicadas,
> porque un error aquí no es un fallo de programa: es una infracción fiscal.

---

## 4.x.1 Qué resuelve y qué problema atacaba

El punto de venta es donde el comercio **cobra**, y donde el sistema demuestra que los
módulos están de verdad integrados: al vender, la misma operación descuenta el inventario,
congela el costo de lo vendido, asigna un comprobante fiscal válido y —si es a crédito—
mueve el balance del cliente.

En el minimarket piloto, antes del sistema, la venta se anotaba a mano y el comprobante
fiscal se llevaba en un talonario. De ahí salían tres problemas: **no se sabía cuánto
quedaba** de mercancía (el inventario solo se corregía contando), **no se sabía cuánto se
ganaba** en cada venta (el costo no se registraba en el momento), y el **control del NCF**
dependía de que nadie se saltara un número del talonario.

El módulo resuelve los tres: la venta descuenta el stock en la misma transacción, guarda el
costo congelado que hace calculable el margen, y asigna el NCF de forma **atómica** desde el
rango autorizado, sin posibilidad de duplicados.

---

## 4.x.2 Decisiones de diseño y su justificación

### Decisión 1 — El NCF se asigna con un `UPDATE ... OUTPUT` atómico, no leyendo y sumando

Esta es **la decisión más importante del módulo**. Lo natural sería leer el contador de la
secuencia, sumarle uno y guardar. Eso deja una ventana de tiempo entre la lectura y la
escritura en la que **dos cajas pueden leer el mismo número** y emitir dos facturas con el
mismo NCF.

Dos comprobantes con el mismo número **es una infracción ante la DGII**, no un detalle
cosmético: el comercio responde por ella. Por eso la asignación se hace en **una sola
sentencia atómica** en la base de datos (`UPDATE … SET Actual = Actual + 1 OUTPUT
INSERTED.Actual`), que el motor serializa: dos cajas concurrentes obtienen números
distintos, siempre. Vive en `SecuenciaNcfRepositorio.AsignarSiguienteAsync` y se **verificó
con veinte procesos concurrentes**, comprobando que no hubiera un solo número repetido.

Como consecuencia de esta decisión, la clase de dominio `SecuenciaNcf` **no incrementa
nada**: solo sabe **formatear** el comprobante y **responder si el rango sirve**. El
incremento es responsabilidad de la capa de datos, porque la atomicidad es una garantía del
motor, no del lenguaje. Esa separación está escrita en el propio código como advertencia
para quien lo lea después.

### Decisión 2 — La emisión corre dentro de una transacción explícita

Emitir una factura hace varias cosas que deben ocurrir **todas o ninguna**: reservar el NCF,
descontar el inventario de cada línea, guardar la factura y sus movimientos. Si falla a
mitad —por ejemplo, porque la tercera línea no tiene existencia— quedaría un NCF consumido
sin factura y un inventario descontado a medias.

Por eso la emisión abre una **transacción explícita**: ante cualquier fallo, se revierte
todo y el NCF reservado **queda libre**. El precio de esta decisión es real y se asume a
conciencia: la fila de la secuencia queda bloqueada durante la venta, así que dos cajas se
**serializan un instante**. Para un local con una o dos cajas es perfectamente aceptable; en
una cadena con veinte cajas habría que replantearlo.

### Decisión 3 — El costo se congela en la línea de la factura

`LineaFactura.CostoUnitario` guarda el costo del producto **al momento exacto de vender**, y
`Emitir` lo copia *antes* de mover nada. Sin esto, una compra posterior —que cambia el costo
promedio del producto— alteraría **retroactivamente** el margen de ventas ya hechas, y los
resultados del Capítulo V se moverían solos cada vez que el comercio comprara mercancía.

De aquí sale la regla que gobierna todo el módulo de Finanzas: **los reportes de
rentabilidad usan `LineaFactura.CostoUnitario`, nunca `Producto.Costo`**. Hay una prueba
dedicada a fijarlo (`Una_compra_posterior_no_altera_el_margen_de_una_venta_ya_hecha`).

### Decisión 4 — Los documentos se congelan: nombre, documento y tasa de ITBIS

La factura guarda `ClienteNombre` y `ClienteDocumento` **copiados** al facturar, y cada línea
guarda su `Descripcion` y su `TasaItbis`. No se leen del cliente ni del producto al mostrar
la factura después.

La razón es que **una factura es un documento histórico**: si mañana el cliente corrige su
RNC, o el producto pasa de gravado a exento, la factura de ayer debe seguir mostrando
exactamente lo que se imprimió y se le entregó al cliente. Un documento fiscal que cambia
solo cuando cambian los catálogos no es un documento fiscal.

### Decisión 5 — El cliente de contado no se registra

`Factura.ClienteId` es **nulo** cuando la venta es al cliente de mostrador que no se
registra, y el nombre queda como "Cliente de contado". La justificación es la operación real
de un minimarket: la mayoría de las ventas son a personas que no dan sus datos, y obligar a
registrar un cliente por cada venta haría el punto de venta inusable. El registro se exige
solo cuando hace falta: **crédito** (hay que saber quién debe) o **crédito fiscal** (la DGII
exige el RNC del comprador).

### Decisión 6 — Anular no libera el NCF, y repone como devolución (no como ajuste)

Al anular una factura, el NCF **no vuelve al rango**. Ante la DGII queda como *comprobante
anulado* y la secuencia sigue avanzando; reutilizarlo produciría dos documentos con el mismo
número, que es justo lo que la decisión 1 evita.

Además, la mercancía se repone con un movimiento de tipo **`DevolucionCliente`**, no con un
ajuste positivo. Así el kardex del producto **dice por qué** volvió la mercancía, en lugar de
mostrar un ajuste anónimo. Y anular **exige un motivo escrito**, que queda guardado en la
factura.

### Decisión 7 — El redondeo es aritmético, no bancario

Los importes se redondean con `RetailConstants.RedondearImporte`, que usa `AwayFromZero`
(2.505 → 2.51). .NET redondea **a par** por defecto (2.505 → 2.50), que es correcto
estadísticamente pero **le hace perder la confianza al comerciante en su propia caja**:
cuando el cajero suma a mano y el sistema da un centavo menos, el problema deja de ser
técnico. Se eligió el redondeo que la gente espera.

### Decisión 8 — Los códigos del tipo de comprobante son los de la DGII

`TipoComprobante` lleva los códigos oficiales (**01** crédito fiscal, **02** consumo, **14**
régimen especial, **15** gubernamental) porque **forman parte del NCF** (`B02…`). No son un
enum interno cualquiera: si se renumeraran, los comprobantes emitidos serían inválidos. Hay
una prueba que los fija para que nadie los cambie por descuido.

### Decisión 9 — El e-CF se simula, con el límite escrito

Emitir un e-CF **real** exige registrar la empresa ante la DGII y obtener un certificado
digital de persona jurídica: un trámite externo, de la empresa piloto y no del grupo, que el
alcance excluye por escrito. La decisión fue **construir todo lo que se puede construir de
verdad y rotular con precisión lo que se simula** (ver 4.x.4).

### Resumen de decisiones

| # | Decisión | Elección | Razón corta |
|---|----------|----------|-------------|
| 1 | Asignación del NCF | `UPDATE … OUTPUT` atómico | Dos NCF iguales es una infracción fiscal |
| 2 | Emisión | Transacción explícita | Un fallo no deja NCF consumido ni stock a medias |
| 3 | Costo | Congelado en la línea | Una compra posterior no debe mover un margen pasado |
| 4 | Documento | Datos copiados al emitir | Una factura es un documento histórico |
| 5 | Cliente | Opcional (contado sin registro) | La mayoría de las ventas son de mostrador |
| 6 | Anulación | No libera el NCF; repone como devolución | Sin duplicados, y el kardex dice por qué |
| 7 | Redondeo | Aritmético (`AwayFromZero`) | Que la caja cuadre con lo que la gente espera |
| 8 | Tipos | Códigos de la DGII (01/02/14/15) | Forman parte del NCF |
| 9 | e-CF | Simulado y rotulado | La certificación es trámite externo, fuera de alcance |

---

## 4.x.3 Modelo de dominio

En `src/MiniERP.Domain/Ventas/`:

- **`Factura`** — la venta. Número, NCF, tipo de comprobante, cliente (opcional) con nombre y
  documento congelados, fecha, estado, condición (contado/crédito), subtotal, ITBIS, total,
  **costo total congelado**, monto recibido y cambio, cajero y motivo de anulación. Expone
  `Margen` (subtotal − costo) y `MargenPorcentaje`.
- **`LineaFactura`** — el renglón, con descripción y tasa congeladas, cantidad, precio y
  **costo unitario congelado**.
- **`SecuenciaNcf`** — el rango autorizado: tipo, prefijo, desde/hasta, actual, vencimiento y
  activa. Sabe si `PuedeEmitir(hoy)`, cuántos quedan (`Disponibles`), si está `PorAgotarse`
  (umbral de 50) y **cómo formatear** el comprobante. Distingue el largo del correlativo:
  **ocho dígitos en papel, diez en electrónico**.
- **`ComprobanteElectronico`** y **`CodigoSeguridadEcf`** — las reglas del e-CF (4.x.4).
- **`EstadoFactura`** — `Emitida` / `Anulada`.

### `Factura.Emitir(...)` — el corazón

```csharp
foreach (var linea in Lineas)
{
    // El costo se congela ANTES de mover nada: es el que se usará para el margen.
    linea.CostoUnitario = producto.Costo;

    var movimiento = new MovimientoInventario
    {
        Tipo = TipoMovimiento.Salida,
        Cantidad = linea.Cantidad,
        CostoUnitario = producto.Costo,
        Motivo = $"Factura {ncf}",
        ReferenciaTipo = "FACTURA",
        ReferenciaId = Id,
        ...
    };

    // Lanza si no hay existencia; la transacción de la capa de datos revierte lo anterior.
    producto.AplicarMovimiento(movimiento);
    movimientos.Add(movimiento);
}

Recalcular();
Ncf = ncf;
```

Nótese que **el NCF llega como parámetro ya reservado**: el dominio no lo genera, porque la
atomicidad es responsabilidad de la base de datos (decisión 1). Y que el descuento se hace
llamando a `Producto.AplicarMovimiento` —el guard del módulo de Inventario—, no tocando la
existencia directamente: **la frontera entre módulos se respeta incluso dentro del dominio.**

---

## 4.x.4 El e-CF: qué es real y qué se simula

El sistema genera el comprobante fiscal electrónico de la **Ley 32-23**, con un límite
explícito y rotulado. Esta tabla es la respuesta a la pregunta que el asesor va a hacer:

| Se construye de verdad | Se simula, y se rotula como tal |
|---|---|
| **El XML** según la estructura publicada del e-CF (formato 32, consumo). El esquema es público: no hace falta estar certificado para generarlo bien. | **La firma digital.** Exige el certificado de persona jurídica. Donde iría la firma, no hay firma. |
| **El QR** y su URL de consulta con los parámetros reales: RNC emisor, e-NCF, monto y fecha. | **El código de seguridad**, que en el original sale de la firma. Aquí es un **hash determinista** de seis caracteres. |
| **Los rangos de e-NCF** en la pantalla de secuencias (la clase ya soporta prefijo `E` y correlativo de diez). | **El envío.** Cero llamadas a la API de la DGII. El estado vive local. |

Dos detalles técnicos que valen la defensa:

**El prefijo electrónico no es el del papel.** Un consumo es `B02` impreso pero `E32`
electrónico; un crédito fiscal, `B01` y `E31`. Por eso el prefijo se **mapea** explícitamente
y no se deriva del valor del enum, que lleva el código tradicional.

**El e-NCF se deriva del NCF conservando el correlativo**, y ahí hay una trampa que el código
evita a propósito: extraer "los dígitos del NCF" sería un error, porque **el `02` de `B02`
también es dígito** y se colaría en el número. Se toma todo lo que sigue al prefijo de tres
caracteres. Así `B0200000123` de consumo da `E320000000123`.

La derivación es **determinista a propósito**: la misma factura produce siempre el mismo
e-CF, lo que lo hace verificable y probable. Y el código deja escrito el punto único de
cambio para cuando el comercio se certifique: el e-NCF pasaría a salir de una `SecuenciaNcf`
con prefijo `E` en vez de derivarse.

**Todo documento e-CF que el sistema genera lleva visible `DOCUMENTO SIMULADO — NO VÁLIDO
ANTE LA DGII`**, en pantalla y **dentro del XML**. El piloto es un comercio real facturando:
un documento que aparente ser un comprobante válido y no lo sea es un problema del
comerciante, no un detalle académico. Rotularlo convierte la limitación en una **decisión
defendible** en lugar de un hueco.

---

## 4.x.5 Pruebas de dominio (metodología TDD)

El módulo es el más probado del sistema: **sesenta y una pruebas** en cuatro archivos, todas
escritas antes del código que las hace pasar.

**`FacturaTests.cs` (19)** — la emisión, el margen, el cambio y la anulación:

| Prueba | Qué garantiza |
|--------|---------------|
| `Emitir_descuenta_del_inventario` · `El_movimiento_apunta_a_la_factura_que_lo_origino` | La venta mueve el stock y deja trazabilidad al documento. |
| `Emitir_asigna_el_comprobante_recibido` · `No_se_emite_sin_comprobante` | No hay factura sin NCF. |
| `No_se_puede_vender_mas_de_lo_que_hay` · `Una_factura_sin_lineas_no_se_emite` | Los guards de la emisión. |
| `El_costo_se_congela_al_emitir` · `Una_compra_posterior_no_altera_el_margen_de_una_venta_ya_hecha` | **La decisión 3, fijada por prueba.** |
| `El_margen_no_cuenta_el_itbis_porque_no_es_del_comercio` · `Una_venta_por_debajo_del_costo_da_margen_negativo` | El margen se calcula sobre el subtotal. |
| `El_cambio_sale_de_lo_que_el_cliente_entrego` · `Pagar_justo_no_deja_cambio` · `Una_venta_a_credito_no_tiene_cambio` | El cambio solo existe al contado. |
| `Anular_repone_el_inventario` · `La_anulacion_no_libera_el_ncf` · `La_reposicion_entra_como_devolucion_y_dice_por_que` · `Anular_exige_motivo` · `Una_factura_no_se_anula_dos_veces` | **La decisión 6, completa.** |
| `Se_puede_facturar_por_peso_con_decimales` | La venta a granel. |

**`SecuenciaNcfTests.cs` (12)** — el rango y su vigencia: que el NCF en papel lleve ocho
dígitos y el electrónico diez, que un rango **agotado**, **vencido** o **desactivado** no
emita, que el rango sirva **hasta el último día** de su vigencia (y que la hora no lo venza
antes), que los disponibles descuenten lo consumido, y que el aviso salte cerca del final.

**`EcfTests.cs` (12)** — las reglas del e-CF: que el prefijo electrónico **no** sea el del
papel, que el correlativo se conserve y se lleve a diez dígitos, que **los dígitos del
prefijo no se cuelen en el correlativo**, que un crédito fiscal no se convierta en consumo, y
que el código de seguridad tenga seis caracteres alfanuméricos, sea **estable** para el mismo
comprobante y **cambie si cambia cualquier campo firmado**.

**`EcfServiceTests.cs` (18)** — el documento generado: que los totales del XML **cuadren con
la factura**, que la tasa vaya en por ciento y no en fracción, que **lo gravado y lo exento no
se mezclen**, que cada línea sea un ítem numerado, que el emisor salga de la configuración,
que el documento **se declare simulado dentro del XML**, que **donde iría la firma no haya
firma**, que el mismo comprobante **se regenere idéntico**, que el QR del consumo omita al
comprador y el del crédito fiscal sí lo identifique, y que **una factura anulada, sin NCF o
inexistente no genere e-CF**.

---

## 4.x.6 Arquitectura y capas

```
MiniERP.Domain/Ventas          Factura, LineaFactura, SecuenciaNcf, ComprobanteElectronico,
      ^                         CodigoSeguridadEcf. Reglas puras: Emitir, Anular, formatear.
MiniERP.Application/Ventas      VentaService, SecuenciaNcfService, EcfService, contratos y DTOs.
      ^
MiniERP.Infrastructure         SecuenciaNcfRepositorio (el UPDATE atómico), VentaRepositorio
      ^                         (la transacción explícita), configuración EF y migraciones.
MiniERP.Web/…/Ventas           Las cinco pantallas.
```

En la **capa de aplicación**, `VentaService` orquesta la venta (armar la factura, pedir el
NCF, invocar `Emitir`, persistir en transacción, mover el balance del cliente si es a
crédito); `SecuenciaNcfService` administra los rangos; y `EcfService` genera el XML y el QR.
Los **datos fiscales del emisor** (RNC, nombre comercial) salen de **configuración**, no de la
base, y se leen desde el proveedor de servicios para no alterar la firma de `AddVentas` — un
detalle que evita tocar el `DependencyInjection` raíz, que es archivo compartido.

En **infraestructura**, la tabla `Facturas` lleva el NCF con **índice único filtrado** (único
cuando existe, admitiendo nulos), y las líneas se eliminan en cascada con su factura. Los
totales usan `decimal(18,2)`, los precios y costos `decimal(18,4)` y las cantidades
`decimal(18,3)`.

---

## 4.x.7 Interfaz de usuario

Cinco pantallas en `src/MiniERP.Web/Components/Pages/Ventas/`:

1. **Punto de venta** (`/ventas`) — la pantalla optimizada para rapidez: búsqueda por código,
   código de barras o nombre; armado del carrito validando la cantidad **según la unidad del
   producto**; selección de tipo de comprobante y cliente; y cobro al contado (con monto
   recibido y **cálculo de cambio**) o a crédito.
2. **Facturas emitidas** (`/ventas/facturas`) — el listado con filtros por fecha y estado.
3. **Detalle de la factura** (`/ventas/facturas/{id}`) — el documento con su NCF, el detalle
   de líneas, el **ITBIS desglosado** y los datos del cliente, con los botones de **imprimir**
   y **anular** (que pide motivo). El encabezado de impresión da forma de documento fiscal y
   una factura anulada se imprime rotulada como tal.
4. **e-CF** (`/ventas/facturas/{id}/ecf`) — el XML del comprobante electrónico y su **código
   QR**, con el rótulo de documento simulado. Solo se ofrece para facturas emitidas: **una
   factura anulada no genera e-CF**, porque ante la DGII eso se retira con una anulación de
   e-NCF, no volviendo a emitir el comprobante.
5. **Secuencias de NCF** (`/ventas/ncf`) — el administrador registra rangos, ve cuántos
   quedan y recibe el **aviso cuando están por agotarse**. La pantalla advierte que las
   secuencias de arranque **no son válidas ante la DGII** y que hay que desactivarlas al
   cargar las autorizadas.

Con el módulo de seguridad, cada una queda protegida por permiso: facturar exige
`ventas.facturar`; consultar facturas y el e-CF, `ventas.ver`; administrar secuencias,
`ventas.ncf`.

---

## 4.x.8 Verificación

**El gate de F2 quedó demostrado de punta a punta**, encadenando compra e inventario con la
venta —que es, literalmente, la integración que promete el anteproyecto:

> Se compraron **20 LB** y la existencia subió de 9.5 a **29.5**, con el costo promediado a
> **30.8729**. Luego se vendieron **3 LB** y bajó a **26.5**, con NCF **`B0200000001`**,
> total **114.00** y cambio **86.00**.

Y la asignación atómica del NCF se verificó con **veinte procesos concurrentes**,
comprobando que no se repitiera ni un solo número.

**Lo que falta por probar, dicho con franqueza:** no existe una prueba automatizada de la
**transacción de emisión** — nada demuestra automáticamente que un rollback libera el NCF
reservado. Es la pieza más delicada del módulo y está anotada como pendiente del plan de
pruebas de la sección 5.1. Las cuatro pantallas de facturas, impresión, anulación y
secuencias tampoco tienen prueba automatizada: se verifican a mano en el navegador.

*(Las capturas de las pantallas se incorporan como `ventas-*.png` en `Docs/CapituloIV/capturas/`.)*

---

## 4.x.9 Alcance y limitaciones

- **No hay certificación fiscal ante la DGII** — se genera el XML e-CF y el QR, y ahí se
  para. Excluido por escrito en el anteproyecto.
- **Las secuencias sembradas no son rangos autorizados** — son de arranque, para que el
  sistema funcione desde el primer día. Antes del piloto real hay que cargar los que la DGII
  le haya autorizado al comercio.
- **No hay devolución parcial de una venta** — la factura se anula completa; una devolución
  de parte de la mercancía no está modelada.
- **La emisión serializa las cajas un instante** — consecuencia aceptada de la transacción
  explícita (decisión 2), adecuada para uno o dos puntos de cobro.

---

*Registro de commits del módulo (rama `jeison/pos`):*
`F2: punto de venta, carrito y cobro` · `F2: asignación atómica del NCF y emisión
transaccional` · `F2c.4: pantalla de facturas emitidas` · `F2c.5: impresión de la factura` ·
`F2c.6: anulación desde la UI` · `F2c.7: administración de secuencias de NCF` ·
`F3: XML del e-CF y código QR, simulados`.
