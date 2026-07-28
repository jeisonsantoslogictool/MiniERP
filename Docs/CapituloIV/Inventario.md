# Capítulo IV — Módulo de Inventario

Trabajo Final de Grado · Grupo 6 · Universidad Dominicana O&M
Autor del módulo: **Jeison Luis Santos** · rama `jeison/pos` · construido en la fase F1
(catálogos).

> Esta sección documenta el módulo de **Inventario**: el registro de productos y el control
> de existencias por movimientos auditables. Es la base de la cadena crítica del sistema —la
> mercancía que entra por una compra y sale por una venta es la misma que se controla aquí—,
> y se escribe con el detalle y la justificación de cada decisión para poder defenderla en la
> sustentación.

---

## 4.x.1 Qué resuelve y qué problema atacaba

El **inventario** es el registro de la mercancía del comercio: qué productos hay, cuántos
quedan, cuánto cuestan y cuánto se venden. En el minimarket piloto —el de Jeison— el
control de existencias vivía en la cabeza del dueño y en el estante: no había forma de saber
qué está por agotarse, cuánto se pierde por vencimiento o daño, ni cuál es el costo real de
lo que se vende.

El planteamiento del problema nombra dos carencias concretas que este módulo ataca de
frente: las **"mermas no detectadas"** (mercancía que se pierde sin que nadie lo note) y el
no conocer **"los costos reales de adquisición"** (sin los cuales no hay margen verdadero).
El módulo las resuelve con dos ideas centrales: **ningún saldo se edita a mano** —toda
existencia cambia por un movimiento que deja rastro— y **la merma es un hecho de primera
clase**, no un ajuste anónimo.

Sobre esa base, el módulo administra los productos y sus categorías, distingue la venta por
unidad de la venta por peso, avisa cuando un producto toca su mínimo, y expone la existencia
que los demás módulos —compras al recibir, ventas al facturar— mueven sin poder tocarla
directamente.

---

## 4.x.2 Decisiones de diseño y su justificación

### Decisión 1 — La existencia vive en el producto, no en una tabla de almacenes

La cantidad disponible (`Existencia`) es un campo del propio `Producto`, no una tabla de
existencias por ubicación. La razón es el alcance real del negocio: el comercio **opera en
una sola ubicación**, y el anteproyecto excluye por escrito los múltiples almacenes.
Modelar existencia por ubicación sería maquinaria para un problema que el piloto no tiene.
La consecuencia es un modelo más simple y directo, sin dejar de ser auditable (decisión 2).

### Decisión 2 — Ningún saldo se edita: la existencia solo cambia por un movimiento

Este es el corazón del módulo. La existencia **nunca se escribe a mano**; solo cambia a
través de `Producto.AplicarMovimiento(...)`, que registra un `MovimientoInventario` con la
**existencia anterior y la resultante**. Incluso la existencia inicial de un producto entra
como un movimiento de **apertura**, no como un valor tecleado.

La justificación es directa del problema: guardar el saldo antes y después de cada cambio
hace que **cualquier existencia sea reconstruible hacia atrás**, y que una discrepancia se
pueda rastrear hasta el movimiento exacto, el usuario y la hora. Es la respuesta a la falta
de rastro que describe el planteamiento. Es también un requerimiento no funcional escrito
(RNF-03: *"ningún saldo se edita"*).

### Decisión 3 — La merma es un tipo de movimiento propio, no un ajuste negativo

Cuando se pierde mercancía por vencimiento, daño o robo, se registra como **`Merma`**, un
tipo distinto del `AjusteNegativo`. Podrían haberse mezclado —ambos bajan la existencia—,
pero se separaron **a propósito**: el planteamiento del problema nombra las *"mermas no
detectadas"* como uno de los males a resolver, y meterlas dentro de los ajustes las
volvería invisibles otra vez. Al ser un tipo propio, las pérdidas se pueden contar,
totalizar y explicar aparte. La merma, además, **exige un motivo escrito**.

### Decisión 4 — La existencia no puede quedar en negativo

`AplicarMovimiento` calcula la existencia resultante y, si diera negativa, **lanza una
excepción** en lugar de guardar. No se puede vender ni ajustar más de lo que hay. La razón
es de integridad: una existencia negativa no significa nada en un estante físico, y
permitirla escondería un error de captura detrás de un número imposible. Es preferible
rechazar la operación y que el usuario cuadre lo que de verdad tiene.

### Decisión 5 — La unidad de medida distingue la venta por peso de la venta por unidad

Un `Producto` tiene una `UnidadMedida` que declara si **admite decimales** y con cuántos.
Una lata se vende por unidad (cantidades enteras); el arroz a granel se vende por libra
(hasta tres decimales, los que una balanza de mostrador puede pesar). Esta distinción es una
de las cuatro características del negocio que el anteproyecto identifica —*"vende por unidad
y por peso"*— y por eso la cantidad de un movimiento y de una línea de venta se **valida
contra la unidad del producto**: no se puede vender media lata, ni cobrar exactamente "3
libras" si la balanza dio 3.247.

### Decisión 6 — El costo se lleva por promedio ponderado, no por último costo

El `Costo` del producto se actualiza al **recibir** mercancía, mediante **promedio
ponderado** (el cálculo vive en el módulo de Compras, `Compra.Recibir`, porque es la
recepción quien lo dispara). Comprar 10 a 30 y luego 10 a 40 deja el costo en 35, no en 40.
Con último costo, el costo quedaría inflado y el margen real, escondido. El anteproyecto
habla justamente de no conocer *"los costos reales de adquisición"*: el promedio ponderado
es la respuesta a esa pregunta, y hace que la rentabilidad que reporta Finanzas sea creíble.

### Decisión 7 — Las alertas son propiedades derivadas, no un campo que se guarda

"Requiere reabastecimiento" y "agotado" **no se almacenan**: se calculan al vuelo
(`Existencia <= ExistenciaMinima`, `Existencia <= 0`) sobre el estado actual del producto.
Guardarlas sería redundante y correría el riesgo de quedar desincronizado con la existencia
real. Al derivarlas, la alerta es **siempre correcta** por construcción. Un producto
inactivo no genera alerta, para no ensuciar la lista de reposición con lo que ya no se vende.

### Decisión 8 — Los productos que no manejan inventario no descuentan stock

Un `Producto` lleva la bandera `ManejaInventario`. Para un artículo normal es verdadera;
para un servicio (algo que se cobra pero no se descuenta de un estante) es falsa, y entonces
`AplicarMovimiento` **no hace nada**. Esto permite que el mismo catálogo maneje mercancía y
servicios sin inventar una segunda entidad, y evita que algo sin existencia física bloquee
una venta por "existencia negativa".

### Resumen de decisiones

| # | Decisión | Elección | Razón corta |
|---|----------|----------|-------------|
| 1 | Ubicación | Existencia en `Producto` | Un solo local; múltiples almacenes fuera de alcance |
| 2 | Saldo | Solo cambia por movimiento con anterior/resultante | Auditable y reconstruible (RNF-03) |
| 3 | Merma | Tipo de movimiento propio | Que las mermas dejen de ser invisibles |
| 4 | Negativo | Se rechaza con excepción | Una existencia negativa no significa nada real |
| 5 | Unidad | Distingue por peso (decimal) de por unidad (entero) | El negocio vende de las dos formas |
| 6 | Costo | Promedio ponderado al recibir | Margen real; responde a "costos reales de adquisición" |
| 7 | Alertas | Propiedades derivadas, no guardadas | Siempre correctas por construcción |
| 8 | Servicios | `ManejaInventario` = falso no descuenta | Un catálogo para mercancía y servicios |

---

## 4.x.3 Modelo de dominio

El dominio vive en `src/MiniERP.Domain/Inventario/` y está formado por dos catálogos, la
entidad principal, el registro de movimiento y un enum de tipos.

- **`Producto`** — el artículo. Guarda código interno (único), código de barras (opcional,
  único cuando existe: a granel no lo hay), descripción, categoría, unidad de medida, costo,
  precio sin ITBIS, tasa de ITBIS, existencia, existencia mínima y las banderas
  `ManejaInventario` y `Activo`. Expone `PrecioConItbis` (el que ve el cliente),
  `MargenUnitario`/`MargenPorcentaje`, y las alertas `RequiereReabastecimiento` y `Agotado`.
- **`Categoria`** y **`UnidadMedida`** — los catálogos. La unidad declara `PermiteDecimales`
  y `CantidadDecimales` (la clave de la venta por peso). Se siembran siete unidades: UND, LB,
  KG, GAL, LT, CAJ, PAQ.
- **`MovimientoInventario`** — el registro **inmutable** de cada cambio: producto, fecha,
  tipo, cantidad (siempre positiva; el signo lo da el tipo), costo unitario, existencia
  anterior y resultante, motivo, referencia al documento origen (`FACTURA`, `COMPRA`…) y
  usuario responsable.
- **`TipoMovimiento`** — `Entrada`, `Salida`, `AjustePositivo`, `AjusteNegativo`, `Merma`,
  `DevolucionCliente`, `DevolucionProveedor`. El movimiento sabe cuáles **suman**
  (`EsEntrada`) y cuáles **exigen motivo** (`RequiereMotivo`: merma y ajustes).

### `Producto.AplicarMovimiento(...)` — el guard del dominio

```csharp
public void AplicarMovimiento(MovimientoInventario movimiento)
{
    ArgumentNullException.ThrowIfNull(movimiento);

    if (!ManejaInventario)
        return;

    var delta = movimiento.EsEntrada ? movimiento.Cantidad : -movimiento.Cantidad;
    var resultante = Existencia + delta;

    if (resultante < 0)
        throw new InvalidOperationException(
            $"El movimiento deja el producto '{Codigo}' en existencia negativa: ...");

    movimiento.ExistenciaAnterior = Existencia;
    movimiento.ExistenciaResultante = resultante;
    movimiento.ProductoId = Id;

    Existencia = resultante;
}
```

Es el **único camino** por el que la existencia cambia. Calcula el signo según el tipo,
rechaza el negativo, estampa el saldo antes y después en el propio movimiento, y recién
entonces actualiza la existencia. Con esto, la regla de que ningún saldo se edita queda
garantizada en el dominio, no confiada a la disciplina de la pantalla.

---

## 4.x.4 Pruebas de dominio (metodología TDD)

El proyecto sigue **desarrollo guiado por pruebas (TDD)**: se escribe primero la prueba, se
la ve fallar y solo entonces se escribe el código que la hace pasar. El dominio de
inventario se cubrió con **dieciocho pruebas** que fijan una a una las decisiones de arriba:

**`ProductoTests.cs`** — la mecánica de la existencia:

| Prueba | Qué garantiza |
|--------|---------------|
| `Entrada_suma_a_la_existencia` | Una entrada aumenta la existencia. |
| `Salida_resta_de_la_existencia` | Una salida la disminuye. |
| `Merma_resta_igual_que_una_salida` | La merma baja el stock como una salida. |
| `Movimiento_guarda_el_saldo_antes_y_despues_para_poder_auditarlo` | Cada movimiento deja anterior y resultante (decisión 2). |
| `Existencia_soporta_decimales_porque_se_vende_por_peso` | El stock admite fracciones (decisión 5). |
| `No_se_puede_vender_mas_de_lo_que_hay` | El guard de negativo rechaza la operación (decisión 4). |
| `Un_servicio_no_mueve_inventario` | Con `ManejaInventario` falso no se descuenta (decisión 8). |
| `La_alerta_de_reabastecimiento_salta_al_tocar_el_minimo` | La alerta se dispara en el umbral (decisión 7). |
| `Un_producto_inactivo_no_genera_alerta` | Lo inactivo no ensucia la reposición. |
| `El_precio_al_publico_incluye_el_itbis` | `PrecioConItbis` calcula bien. |
| `Un_producto_exento_se_vende_sin_recargo` | Tasa 0 no agrega ITBIS. |
| `El_margen_se_calcula_sobre_el_precio_de_venta` | El margen usa el precio, no el costo. |
| `Un_producto_sin_precio_no_divide_entre_cero` | El margen porcentual no rompe con precio 0. |

**`UnidadMedidaTests.cs`** — la venta por peso frente a la venta por unidad:

| Prueba | Qué garantiza |
|--------|---------------|
| `Unidad_acepta_cantidades_enteras` | Una unidad entera se acepta. |
| `Unidad_rechaza_decimales` | No se vende media lata. |
| `Libra_acepta_hasta_tres_decimales` | El peso admite la precisión de la balanza. |
| `Libra_rechaza_mas_precision_de_la_que_pesa_una_balanza` | No más decimales de los reales. |
| `Ninguna_unidad_acepta_cantidades_no_positivas` | Cero o negativo se rechaza. |

Cada prueba se vio fallar antes de existir el código, garantizando que prueba algo real.

---

## 4.x.5 Arquitectura en capas

El módulo respeta la arquitectura modular en capas, con una carpeta `Inventario/` en cada
una:

```
MiniERP.Domain/Inventario          Producto, Categoria, UnidadMedida, MovimientoInventario,
      ^                             TipoMovimiento y AplicarMovimiento. Reglas puras.
MiniERP.Application/Inventario      ProductoService, CategoriaService, contratos de
      ^                             repositorio y DTOs. Los casos de uso.
MiniERP.Infrastructure             Configuración EF, repositorios y migración.
      ^
MiniERP.Web/…/Inventario           Las pantallas. Solo presentación.
```

El patrón es el del resto del sistema: **el dominio decide, el servicio orquesta y
persiste.** La regla de que la existencia no queda negativa vive en el dominio
(`AplicarMovimiento`); el servicio la invoca y traduce la excepción en un mensaje amable; el
repositorio solo lee y escribe.

---

## 4.x.6 Capa de aplicación

En `src/MiniERP.Application/Inventario/`:

- **`ProductoService`** — busca productos por texto, código o categoría (con paginación y
  filtros de "solo activos", "requieren reposición", "agotados"); crea y edita productos
  validando el **código único** y el código de barras único; y siembra la **existencia
  inicial como un movimiento de apertura**, nunca como un valor directo. Expone también la
  consulta de productos que requieren reabastecimiento, que alimenta la alerta.
- **`CategoriaService`** — administra el catálogo de categorías (alta, edición,
  activar/desactivar), con nombre único.
- Los **contratos de repositorio** (`IProductoRepositorio`, `ICategoriaRepositorio`) y los
  **DTOs** (`ProductoListaDto`, `ProductoFormDto`, `FiltroProductos`…) separan la pantalla de
  Entity Framework y de las entidades del dominio.

---

## 4.x.7 Infraestructura y persistencia

En `src/MiniERP.Infrastructure/` está la configuración de las tablas `Productos`,
`Categorias`, `UnidadesMedida` y `MovimientosInventario`:

- Los importes usan `decimal(18,4)` para costo y precio, y las cantidades `decimal(18,3)`
  para admitir el peso. La tasa de ITBIS usa `decimal(5,4)`.
- `Productos.Codigo` lleva índice **único**; `CodigoBarras`, índice **único filtrado** (para
  que muchos productos a granel puedan compartir el valor nulo sin chocar).
- Las llaves foráneas a `Categorias` y `UnidadesMedida` usan **restricción** (no se borra un
  catálogo referenciado).
- Las **unidades de medida** se siembran con `HasData` porque son un catálogo **inmutable**;
  las **categorías** típicas, en cambio, van en el sembrador de arranque porque el
  comerciante las editará. La tabla se crea con la migración de la fase de catálogos.

---

## 4.x.8 Interfaz de usuario

Las pantallas viven en `src/MiniERP.Web/Components/Pages/Inventario/`, en Blazor Server con
el estilo Bootstrap del sistema:

1. **Inventario** (`/inventario`) — la portada del módulo, con el acceso a productos y
   categorías y un resumen de las alertas.
2. **Productos** (`/inventario/productos`) — el listado con búsqueda y los filtros de activos,
   reposición y agotados, mostrando existencia, costo, precio y el estado de cada producto.
3. **Editor de producto** (`/inventario/productos/nuevo` y `/{id}`) — el formulario de alta y
   edición, con la unidad de medida y la existencia inicial (que entra como apertura).
4. **Categorías** (`/inventario/categorias`) — administra el catálogo.

Con el módulo de seguridad, estas pantallas quedan protegidas por permiso: ver el inventario
exige `inventario.ver`; editar productos y categorías, `inventario.editar`.

---

## 4.x.9 Verificación (pruebas funcionales en navegador)

Además de las dieciocho pruebas de dominio, el módulo se verificó **en ejecución** contra
SQL Server. El *gate* de la fase de catálogos (F1) quedó demostrado:

| Prueba funcional | Resultado |
|------------------|-----------|
| Registrar un producto con su unidad y existencia inicial | El producto aparece; la existencia entró como movimiento de apertura. |
| Ajustar la existencia por debajo del mínimo | **Salta la alerta de reabastecimiento** en la lista. |
| Intentar una salida mayor que la existencia | Se rechaza: no se puede vender más de lo que hay. |

Y en el *gate* integrado de F2, el inventario se movió de punta a punta con el resto de la
cadena: **se compraron 20 LB y la existencia subió de 9.5 a 29.5**, con el costo promediado
a 30.8729; luego **se vendieron 3 LB y bajó a 26.5**. Ese encadenamiento —la misma mercancía
que entra por la compra y sale por la venta— es, literalmente, la integración que promete el
anteproyecto.

*(Las capturas de estas pruebas se incorporan como `inventario-*.png` en `Docs/CapituloIV/capturas/`.)*

---

## 4.x.10 Alcance y limitaciones

Lo que el módulo **no** hace, por decisión y por alcance:

- **No maneja múltiples almacenes** — la existencia vive en el producto porque el comercio
  opera en una sola ubicación (excluido por escrito en el anteproyecto).
- **No hace conteo cíclico ni valuación PEPS/UEPS** — la valuación es a costo promedio
  ponderado, que es lo que el margen real del comercio necesita.
- **No lleva lotes ni fechas de vencimiento por partida** — la merma cubre la pérdida por
  vencimiento como un hecho, sin rastrear el lote.

Son fronteras deliberadas: el módulo responde lo que el problema pide —control auditable de
existencias, mermas visibles y costo real— sin cargar complejidad que el piloto no usa.

---

*Registro de commits del módulo (rama `jeison/pos`, fase F1):*
`Inventario: entidades, unidades y movimientos` · `Inventario: AplicarMovimiento y alertas` ·
`Inventario: capa de aplicación y repositorios` · `Inventario: infraestructura y siembra` ·
`Inventario: pantallas Web`.
