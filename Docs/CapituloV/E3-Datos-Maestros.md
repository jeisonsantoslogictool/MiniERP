# E3 — Datos maestros del comercio piloto

Piloto de validación del Mini ERP · Capítulo V · Trabajo Final de Grado Grupo 6

Depende de: [E2 — Plan de pruebas](E2-Plan-de-Pruebas.md)
Archivos de datos: [`datos/`](datos/)

---

## 1. El comercio

**Colmado La Esperanza** · Calle Duarte 78, Moca, provincia Espaillat · RNC de prueba
`131456789`. Un solo local, una caja, atendido por el dueño y dos cajeros, con un encargado de
almacén y un supervisor que consulta.

Vende **al contado y a crédito** (fiado), **por unidad y por peso**. Esas cuatro
características son las que el anteproyecto usa para justificar el modelo de datos, así que el
piloto las ejercita todas.

---

## 2. Los archivos

Formato **CSV con punto y coma**, para que Excel en español los abra sin pelear y el arnés de
integración los lea sin ambigüedad con los decimales.

| Archivo | Filas | Contenido |
|---|---:|---|
| [`datos/productos.csv`](datos/productos.csv) | 64 | Catálogo completo |
| [`datos/clientes.csv`](datos/clientes.csv) | 40 | Cartera |
| [`datos/proveedores.csv`](datos/proveedores.csv) | 12 | Suplidores |
| [`datos/usuarios.csv`](datos/usuarios.csv) | 5 | Usuarios con su rol y permisos |
| [`datos/secuencias-ncf.csv`](datos/secuencias-ncf.csv) | 6 | Rangos de comprobantes y qué hacer con cada uno |

---

## 3. Catálogo de productos — 64

Verificado por script contra los mínimos exigidos:

| Requisito | Exigido | Real | |
|---|---:|---:|:---:|
| Productos | 60 | **64** | ✅ |
| Vendidos por peso (LB o KG) | 12 | **20** | ✅ |
| Exentos de ITBIS | 5 | **11** | ✅ |
| Sin manejo de inventario | 5 | **5** | ✅ |
| Categorías cubiertas | 10 | **10** | ✅ |
| Códigos duplicados | 0 | **0** | ✅ |

**Reparto por categoría:** Víveres 12 · Bebidas 8 · Carnes y Embutidos 8 · Snacks 8 · Lácteos 7 ·
Limpieza 6 · Enlatados 5 · Higiene Personal 5 · Panadería 3 · Congelados 2.

**Unidades usadas:** UND, LB, GAL y PAQ — cubre entero y decimal, que es lo que permite probar
la validación de cantidades por unidad de medida.

### 3.1 Decisiones que condicionan las pruebas

**Los 11 exentos son la canasta básica real dominicana:** arroz, habichuela roja y negra,
azúcar, sal, aceite, harina, leche entera, huevos, pan de agua y pan sobao. Permiten probar que
el ITBIS se separa correctamente y que el e-CF distingue gravado de exento.

**Los 5 sin inventario son servicios de colmado:** recargas de Claro, Altice y Viva, fotocopias
y la comisión por envío de dinero. Sirven para probar que una venta de servicio **no descuenta
existencia** y que el sistema rechaza ajustarles el inventario.

**Las existencias mínimas son variadas y realistas** (de 5 a 80 unidades según rotación), no un
número uniforme: eso hace que la alerta de reabastecimiento del cuadre nº 17 tenga que
discriminar de verdad.

### 3.2 Cifras de apertura

| | |
|---|---:|
| Inventario al costo | **RD$ 341,519.00** |
| Inventario a precio de venta | **RD$ 441,578.00** |
| Margen potencial | **RD$ 100,059.00** |
| Margen promedio del catálogo | **25.2 %** (mín. 16.1 % · máx. 80.0 %) |

Un margen promedio del 25 % es lo que se espera de un colmado dominicano: alto en snacks y
servicios, muy bajo en la canasta básica. Que el mínimo sea 16 % y no 2 % evita que los
redondeos escondan errores de cálculo en el reporte de rentabilidad.

**Ninguno de los 59 productos inventariables se vende al costo o por debajo**, verificado por
script: así, cualquier margen negativo que aparezca en un reporte durante el piloto es un
defecto y no un dato de entrada.

---

## 4. Cartera de clientes — 40

| Requisito | Exigido | Real | |
|---|---:|---:|:---:|
| Clientes | 40 | **40** | ✅ |
| Con límite de crédito | 15 | **29** | ✅ |
| Con crédito fiscal y RNC | 5 | **6** | ✅ |
| De contado sin documento | varios | **6** | ✅ |

**Verificaciones de formato ejecutadas por script:**

- Las **26 cédulas** cumplen el formato `000-0000000-0`.
- Los **8 RNC** tienen 9 dígitos.
- **Todo cliente con comprobante de crédito fiscal tiene RNC** — es la regla
  `Cliente.ComprobanteEstaSustentado` del dominio, así que los datos no la violan de entrada.

**Crédito total otorgado: RD$ 282,100.00**, repartido entre 29 clientes con límites de
RD$ 1,500 a RD$ 50,000 y plazos de 15, 30, 45 y 60 días. Esa dispersión es la que hace que el
cálculo de antigüedad de saldos tenga algo real que calcular.

**La cartera mezcla los cuatro tipos de comprobante que el sistema soporta:** consumo (32),
crédito fiscal (6) y gubernamental (2) — un ayuntamiento y una escuela, que en el país compran
a crédito con plazos largos. No hay clientes de régimen especial, y por eso esa secuencia se
desactiva (ver §6).

---

## 5. Suplidores — 12

| Documento | Cantidad |
|---|---:|
| Con RNC (empresas) | 8 |
| Con cédula (personas) | 2 |
| Sin documento | 2 |

**10 de los 12 otorgan crédito**, de 7 a 45 días, lo que permite que al menos 8 de las 30
órdenes de compra sean a crédito, como exige el plan, y que las cuentas por pagar tengan
antigüedad real.

Los dos sin documento son el mayorista informal y el suplidor de frutas y vegetales — que en un
colmado real existen y cobran en efectivo. Sirven para probar que el sistema admite un
proveedor sin documento y que el índice único filtrado no se queja de dos nulos.

---

## 6. Secuencias NCF — y el agotamiento planificado

Aquí hay una decisión de diseño del piloto que conviene explicar, porque es deliberada.

El sistema siembra cuatro rangos de 5,000 comprobantes cada uno. **Con 378 ventas planificadas,
ninguno se agotaría jamás**, y el caso de prueba «agotar una secuencia a mitad de una venta»
quedaría sin ejecutar. Por eso:

| Orden | Rango | Acción | Para qué |
|---|---|---|---|
| 1 | B02 · 1 a 5000 | **Desactivar** el día 1 | Es el sembrado; se aparta |
| 2 | **B02 · 5001 a 5250** | **Registrar** el día 1 | **250 comprobantes: se agotan el día 14** |
| 3 | B02 · 5251 a 6000 | **Registrar** el día 14 | El relevo que registra el administrador cuando el corto se agota |
| 4 | B01 · 1 a 5000 | Dejar activo | Para los 6 clientes con crédito fiscal |
| 5 | B15 · 1 a 5000 | Dejar activo | Para los 2 clientes gubernamentales |
| 6 | B14 · 1 a 5000 | Desactivar | No hay clientes de régimen especial |

**El rango corto no se solapa con el sembrado** (empieza en 5001), así que la validación de
solape de `SecuenciaNcfService` lo acepta. Es la forma limpia de forzar el agotamiento sin
tocar la base a mano.

### 6.1 Por qué el agotamiento cae en el día 14

Ventas acumuladas según el calendario de E2:

| Al cierre del día | 12 | 13 | **14** |
|---|---:|---:|---:|
| Facturas acumuladas | 237 | 249 | **263** |

Con 250 comprobantes disponibles, el rango se agota **durante la jornada del día 14**, que es
exactamente donde el calendario lo tenía previsto. No es casualidad: el rango se dimensionó
contando el calendario hacia atrás.

---

## 7. Los cinco usuarios

| Correo | Rol | Para qué está en el piloto |
|---|---|---|
| `admin@laesperanza.do` | Administrador | El dueño. Pasa a todo por el bypass de rol |
| `cajero1@laesperanza.do` | Cajero | Turno de mañana. Factura **y cobra** fiado |
| `cajero2@laesperanza.do` | Cajero | Turno de tarde. Factura pero **no cobra** |
| `almacen@laesperanza.do` | Almacén | Recibe, ajusta y registra mermas. **No factura ni paga** |
| `supervisor@laesperanza.do` | Supervisor | Consulta sin modificar. **No debe poder registrar egresos** |

**Los dos cajeros no son iguales a propósito.** `cajero2` tiene `clientes.ver` pero no
`clientes.cobrar`: es el caso que prueba el gateo **a nivel de acción**, no solo de ruta. Si
`cajero2` ve el botón de cobrar, es un hallazgo.

Lo mismo con el supervisor: tiene `finanzas.ver` y **no** `finanzas.egreso`. Es exactamente el
escenario que el módulo de Finanzas documenta en su sección 4.8, y aquí se vuelve a validar de
forma independiente.

**Los cinco se crean desde la interfaz**, por la pantalla `/usuarios/nuevo` que Jeison integró
a `main` el 28 de julio. Eso valida esa pantalla de paso, y elimina la restricción R-05 de E0,
que obligaba a crearlos por SQL.

---

## 8. Cómo se cargan los datos

**Por los servicios de la capa de aplicación, nunca por INSERT directo.** El motivo es que
insertar a mano se saltaría las reglas que el piloto quiere validar:

- La existencia inicial de un producto **debe entrar como movimiento de apertura**, no como un
  valor escrito en la columna. Si se inserta directo, el cuadre nº 1 (existencia = suma de
  movimientos) fallaría desde el minuto cero por culpa de la carga, no del sistema.
- La validación de documento, la unicidad de código y el sustento del comprobante fiscal
  **tienen que ejercitarse**: son parte de lo que se está probando.

El arnés lee los CSV y llama a `ProductoService.CrearAsync`, `ClienteService.CrearAsync`,
`ProveedorService.CrearAsync` y `SecuenciaNcfService.RegistrarAsync`.

### 8.1 Verificación posterior a la carga

Antes de arrancar el día 1 se comprueba por SQL:

1. 64 productos, 40 clientes, 12 proveedores y 5 usuarios con su rol.
2. **64 movimientos de apertura**, uno por producto — salvo los 5 servicios, que no manejan
   inventario: **59 movimientos**.
3. Para cada producto: `Existencia` = suma de sus movimientos = existencia resultante del
   último. Es el cuadre nº 1, ejecutado ya sobre la carga.
4. Valor del inventario al costo = **RD$ 341,519.00**, contrastado con el CSV.
5. Todos los balances de clientes y proveedores en cero: **el piloto arranca sin deudas**.

Si cualquiera de las cinco falla, **no se empieza el día 1**: se corrige la carga primero. Un
piloto que arranca descuadrado no demuestra nada.

---

## 9. Reproducibilidad

Los CSV son la fuente de verdad y están versionados. Cualquiera puede repetir la carga
completa desde una base vacía, y las cifras de §3.2 y §8.1 deben salir idénticas. Los scripts
de validación que produjeron los conteos de este documento se entregan junto con el arnés.
