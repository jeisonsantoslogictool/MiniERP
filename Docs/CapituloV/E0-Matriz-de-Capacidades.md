# E0 — Matriz de capacidades solicitadas frente a capacidades reales

Piloto de validación del Mini ERP · Capítulo V · Trabajo Final de Grado Grupo 6

**Versión auditada:** commit `b3dff81`, rama `samuel/permisos` — ver [E1](E1-Entorno-y-Linea-Base.md) §1.
**Alcance de este documento:** decidir qué se puede probar en el piloto y qué no. Nada más.

---

## 1. Resultado

| Estado | Capacidades | % |
|---|---:|---:|
| **Existe** | **28** | 51 % |
| **Existe parcialmente** | **12** | 22 % |
| **No existe** | **15** | 27 % |
| **Total auditado** | **55** | 100 % |

**Ninguna ausencia cambió de veredicto al intentar refutarla.** Las 27 capacidades marcadas
como ausentes o parciales se sometieron a una segunda revisión cuyo único objetivo era
encontrarlas —buscando sinónimos, en inglés y en español, en las cuatro capas, en migraciones,
DTO, enums y pruebas—. **Las 27 se mantuvieron.** Cero correcciones.

### 1.1 Lectura para el informe

Poco más de la mitad de lo que una tienda necesita está construido de punta a punta. Pero el
reparto no es aleatorio: **lo que existe es el ciclo del documento** (comprar, recibir, vender,
facturar, cobrar, pagar, reportar), y **lo que falta es la operación del mostrador**
(caja, medios de pago, descuentos, devoluciones al cliente).

Dicho de otro modo: el sistema sabe llevar la contabilidad de un colmado, pero todavía no sabe
atender su mostrador.

---

## 2. Método

1. Seis auditores independientes recorrieron el código por área funcional, con la obligación de
   citar archivo y clase/método, o pantalla y ruta.
2. Toda capacidad que un auditor marcó como ausente o parcial pasó a un segundo revisor cuyo
   encargo era **demostrar que el primero se equivocó**. Solo se dan por buenas las ausencias
   que sobrevivieron a esa segunda pasada, y se registran los términos de búsqueda empleados.
3. Criterio de clasificación aplicado:
   - **Existe** — funciona de punta a punta: dominio, persistencia, servicio y pantalla.
   - **Existe parcialmente** — falta al menos una capa, o cubre solo una parte del caso.
   - **No existe** — no hay nada en ninguna capa.

**Advertencia de método.** Esta fase audita el **código**, no el comportamiento en ejecución.
Que una capacidad figure como *Existe* significa que está construida, no que funcione bien.
Demostrar lo segundo es el objeto de las fases 3 a 8.

---

## 3. Verificación de las sospechas previas del encargo

El encargo señalaba once capacidades como probablemente ausentes y pedía confirmarlas o
refutarlas. **Las once se confirmaron**, ninguna se refutó:

| Sospecha del encargo | Veredicto | Precisión añadida |
|---|---|---|
| Caja: apertura, cierre, arqueo | ✅ Confirmada | No existe ni la entidad. El único rastro es un comentario que justifica un índice |
| Medios de pago distintos de contado y crédito | ✅ Confirmada | `CondicionPago` tiene exactamente dos valores |
| Pagos combinados o mixtos | ✅ Confirmada | Es limitación estructural: la factura tiene **un** `MontoRecibido` |
| Descuentos | ✅ Confirmada | **Cero coincidencias** de «descuento» en todo `src/` |
| Devolución parcial de venta | ✅ Confirmada | `Factura.Anular` recorre todas las líneas, sin parámetro de cantidad |
| Reembolsos | ✅ Confirmada | Cero coincidencias de «reembols» |
| Transferencias entre almacenes | ✅ Confirmada | La existencia es un campo escalar de `Producto` |
| Multi-sucursal | ✅ Confirmada | **Excluida por escrito** en el anteproyecto — es limitación declarada, no hueco |
| Corte Z / cierre diario | ✅ Confirmada | Cero coincidencias de «corte z» |
| Devolución a proveedor: dominio y aplicación sí, persistencia y pantalla no | ✅ Confirmada | Se identificaron **6 capas faltantes** y **un hueco funcional adicional** (§6.1) |
| Permisos solo protegen Finanzas | ✅ Confirmada | **7 pantallas gateadas, 20 con `[Authorize]` pelado** — verificado archivo por archivo |

---

## 4. La matriz

Leyenda: ✅ Existe · ⚠️ Existe parcialmente · ❌ No existe

### 4.1 Arranque, usuarios y seguridad

| Capacidad | Estado | Evidencia | Sustituto | Cómo se probará |
|---|:---:|---|---|---|
| Configuración inicial y siembra | ✅ | `DatabaseInitializer.InicializarAsync` (37-45) encadena migraciones, roles, admin, categorías, categorías de egreso y secuencias NCF; llamado desde `Program.cs:39` | — | Borrar la base, arrancar y verificar por SQL: 4 roles, 1 admin, 10 categorías, 5 categorías de egreso, 4 secuencias, 7 unidades. Rearrancar y comprobar idempotencia |
| Usuarios | ⚠️ | Andamiaje de Identity completo (30 páginas bajo `/Account`). **No hay pantalla de administración**: ninguna ruta `/usuarios`; no hay `IUsuarioService`; `ApplicationUser` es una clase vacía | `/Account/Register` (público) + SQL a mano sobre `AspNetUserRoles` | Se prueba el autoservicio (funciona) y se documenta el hueco: no hay forma de que el dueño dé de alta un cajero desde la interfaz |
| Roles | ⚠️ | `Roles.cs` define 4 y `SembrarRolesAsync` los crea. **Único `AddToRoleAsync` del repositorio: el del admin.** Ningún `[Authorize(Roles=…)]` en ninguna pantalla | Asignación por SQL | Verificar que los 4 existen en `AspNetRoles`, y demostrar que asignar «Cajero» **no cambia nada** |
| Permisos | ⚠️ | Motor completo (catálogo de 17, proveedor de políticas, manejador con bypass de Administrador). **Gateo real: solo las 7 pantallas de Finanzas.** 15 de los 17 permisos no se exigen en ningún sitio | Sesión iniciada, para las otras 20 pantallas | Matriz rol × pantalla de la Fase 8, ruta por ruta |
| Auditoría y trazabilidad | ⚠️ | `EntidadBase` aporta `CreadoPor`/`ModificadoPor`; hay `UsuarioId` en factura, cobro, pago, egreso y movimiento. **No hay tabla ni pantalla de auditoría** | Consulta SQL sobre las columnas | Verificar el cuadre nº 16. **Ver defecto D-01: la firma de cobros y pagos es falsa** |
| Respaldo y restauración | ❌ | Búsqueda sin resultados en todo el árbol: no hay servicio, endpoint, pantalla, script ni procedimiento documentado | `BACKUP DATABASE` a mano desde SSMS | No se puede probar. Se reporta como ausencia y se recomienda declararla en el alcance |

### 4.2 Caja y terminales

| Capacidad | Estado | Evidencia | Sustituto | Cómo se probará |
|---|:---:|---|---|---|
| Apertura de caja | ❌ | Sin entidad, sin DbSet, sin permiso. La única «apertura» es de inventario (`ReferenciaTipo="APERTURA"`) | Ninguno | No se puede probar |
| Cierre de caja | ❌ | Único rastro: un comentario en `VentasConfiguration.cs:44` que menciona el cierre para justificar un índice. **El índice existe; la funcionalidad no** | `/finanzas/flujo-caja` con Desde=Hasta=hoy | Se probará el sustituto y se declarará que no es un cierre: es recalculable, no inmutable |
| Arqueo de caja | ❌ | Cero coincidencias de «arqueo», «denominacion», «billete», «conteo», «fondo» | Ninguno | No se puede probar. No existe el concepto de efectivo contado frente a esperado |
| Entradas de efectivo | ⚠️ | Existen atadas a documento: venta de contado (`MontoRecibido`/`Cambio`) y cobro. **No existe entrada libre** (aporte del dueño, fondo inicial) | Factura de contado y cobro | Se prueban las dos que existen; se documenta que no acumulan saldo de gaveta |
| Salidas de efectivo | ⚠️ | Existen como gasto (`Egreso`) y como pago a proveedor. **No existe retiro de caja** | Egreso y Pago | Igual. **Aviso: usar `Egreso` para simular un retiro rompe la regla «un pago no es un egreso»** |
| Cierre diario / corte Z | ❌ | Cero coincidencias. No hay documento correlativo de cierre ni agrupación por día | Panel `/finanzas` y reportes con rango de un día | Se prueba el sustituto. **No sirve como evidencia de cierre: no es inmutable** |
| Multi-caja / multi-terminal | ❌ | No hay entidad Terminal ni columna de terminal en `Factura`; solo `UsuarioId`, que identifica a la persona | Varios usuarios facturando contra la misma instancia | Fase 6: es multi-usuario, no multi-caja |
| Multi-sucursal | ❌ | **Excluida por escrito**: el alcance del anteproyecto, `EcfDtos.cs:6-11`, `Producto.cs:8-12` y `CLAUDE.md` lo declaran | Ninguno | No aplica. **Limitación declarada, no ausencia** |

### 4.3 Inventario

| Capacidad | Estado | Evidencia | Sustituto | Cómo se probará |
|---|:---:|---|---|---|
| Creación y edición de productos | ✅ | `ProductoService.CrearAsync`/`ActualizarAsync` con validación de unicidad; pantallas `/inventario/productos/nuevo` y `/{Id}` | — | Alta, edición, duplicados y precios negativos (Fase 4) |
| Categorías de producto | ✅ | `CategoriaService` con alta, edición y borrado; `/inventario/categorias` | — | CRUD y borrado de categoría con productos asociados |
| Unidades de medida | ⚠️ | Dominio completo y probado (18 casos). **No hay pantalla ni servicio de escritura**: solo lectura para llenar el combo | El combo del formulario de producto | Se prueba la validación de decimales; el alta de unidades exige migración |
| Precios | ✅ | `PrecioVenta` con vista previa de margen y aviso de venta bajo costo | — | Cambio de precio y su efecto en ventas posteriores. **No hay historial de precios** |
| Impuestos (ITBIS) | ✅ | `TasaItbis` por producto (18 %, 16 %, exento), congelado por línea | — | Venta gravada y exenta; separación en el e-CF |
| Consulta de inventario | ✅ | Búsqueda, filtro por categoría, «solo por reabastecer» y paginación | — | Búsqueda, filtro y paginación. **No hay reporte de valoración del inventario** |
| Kardex | ⚠️ | Datos completos y auditables. **Falta la presentación**: no hay ruta propia, ni filtro por fechas, ni paginación; la ficha muestra 25 fijos | Bloque «Últimos movimientos» de la ficha | Se prueba el cuadre nº 1 por SQL; en pantalla solo se ven 25 |
| Ajustes de existencia | ✅ | `AjustarExistenciaAsync` con motivo obligatorio y bloqueo de negativos | — | Ajustes ±, motivo vacío, producto sin inventario |
| Mermas | ✅ | Tipo propio (`Merma = 5`) con motivo obligatorio | — | Registro con y sin motivo. **Ver hallazgo H-04: no impactan la utilidad** |
| Transferencias entre almacenes | ❌ | Sin entidad, sin DbSet, sin tipo de movimiento. La existencia es un escalar | Ninguno representable | No aplica. **Excluida por escrito** |
| Alertas de reabastecimiento | ✅ | `RequiereReabastecimiento` + pantalla `/inventario` con badges | — | Cuadre nº 17, exacto: ni uno más ni uno menos |
| Productos agotados | ⚠️ | La condición existe y se pinta como badge. **Falta el filtro**: `FiltroProductos` no tiene `SoloAgotados` | Filtro «solo por reabastecer», que los contiene | Se prueba el sustituto y se documenta el hueco |

### 4.4 Compras y proveedores

| Capacidad | Estado | Evidencia | Sustituto | Cómo se probará |
|---|:---:|---|---|---|
| Registro de proveedores | ✅ | `ProveedorService` completo; `/compras/proveedores` | — | Alta, edición, documento inválido, duplicados |
| Órdenes de compra | ✅ | `Compra` con estados Borrador/Recibida/Anulada; `/compras/nueva` | — | Alta, edición de borrador, anulación antes y después de recibir |
| Recepción de mercancía | ✅ | `Compra.Recibir` + `CalcularCostoPromedio`, genera entradas y congela el documento | — | **Cuadre nº 10: 15 recepciones con el promedio ponderado calculado a mano** |
| Cuentas por pagar | ✅ | `ObtenerCuentasPorPagarAsync` con días vencidos y monto vencido | — | Cuadre nº 7 y la antigüedad |
| Pagos a proveedores | ✅ | `Pago` con balance anterior y resultante; `Proveedor.AplicarPago` rechaza excesos | — | Cuadre nº 8. **Ver defecto D-01 y restricción R-02** |
| Devoluciones a proveedor | ⚠️ | Dominio y aplicación completos y probados (13 pruebas). **Faltan 6 capas** (§6.1) | Anular la compra si no se recibió; ajuste manual si ya se recibió | **No se puede probar: no hay pantalla ni tabla.** Se reporta como no entregada |

### 4.5 Ventas, medios de pago y comprobantes

| Capacidad | Estado | Evidencia | Sustituto | Cómo se probará |
|---|:---:|---|---|---|
| Venta en efectivo | ✅ | `CondicionPago.Contado`, `MontoRecibido`/`Cambio`, validación de efectivo insuficiente | — | Camino feliz, efectivo insuficiente, cambio exacto |
| Venta con tarjeta | ❌ | Sin campo de medio de pago. Cero coincidencias de negocio | Registrarla como contado, indistinguible del efectivo | **No se puede probar. Impacto: infla el efectivo del flujo de caja** |
| Venta por transferencia | ❌ | Ídem. La única coincidencia de «transferencia» es un comentario del módulo de Compras | Registrarla como contado, sin referencia bancaria | No se puede probar |
| Venta a crédito | ✅ | `ValidarCredito` exige cliente, crédito aprobado y cupo; sube el balance en la transacción | — | Camino feliz y los cinco casos límite de crédito (Fase 4) |
| Pagos combinados o mixtos | ❌ | La factura tiene **una** condición y **un** `MontoRecibido`. Sin colección de pagos | Facturar a crédito y abonar después: otro documento, otro momento | No se puede probar. Limitación estructural |
| Descuentos por línea | ❌ | **Cero coincidencias de «descuento» en todo `src/`.** La columna Precio del POS es de solo lectura | Ninguno usable desde el POS | No se puede probar. **Es el «te lo dejo en 100» del mostrador: se va a notar** |
| Descuentos por factura | ❌ | `Factura` solo tiene Subtotal, Itbis y Total | Ninguno | No se puede probar |
| Anulación de factura | ✅ | `Factura.Anular` exige motivo, repone con costo congelado y no libera el NCF | — | **Cuadre nº 12**, doble anulación, anulación sin motivo, anulación de crédito abonado |
| Devolución parcial de venta | ❌ | `Anular` recorre todas las líneas, sin parámetro de cantidad. No hay entidad de devolución de venta | Anular completa y refacturar: consume un segundo NCF | No se puede probar |
| Reembolsos | ❌ | Cero coincidencias. La anulación repone inventario pero **no registra salida de dinero** | La anulación actúa como reembolso implícito | **No se puede probar. Ver hallazgo H-05: rompe el cuadre de caja del día** |
| Impresión de factura | ✅ | `window.print` con encabezado y sello de anulada solo para papel | — | Impresión de factura emitida y anulada, y captura como evidencia |
| Comprobante fiscal electrónico | ⚠️ | XML, e-NCF derivado y QR funcionan de punta a punta. **Falta la persistencia**: no hay DbSet, el XML se regenera y **el estado Generado→Enviado→Aceptado no existe** | — | Se prueba generar, ver, imprimir y descargar. **Limitación declarada:** firma y envío simulados |
| Secuencias NCF | ✅ | Rango con validación de solape, asignación atómica `UPDATE … OUTPUT`, pantalla con avisos | — | **Cuadre nº 5**, agotamiento a mitad de venta, secuencia vencida, y Fase 6 |

### 4.6 Clientes, cobros y finanzas

| Capacidad | Estado | Evidencia | Sustituto | Cómo se probará |
|---|:---:|---|---|---|
| Registro de clientes | ✅ | `ClienteService` con validación de documento y sustento del comprobante | — | Alta, cédula de 10 dígitos, RNC de 8, crédito fiscal sin RNC |
| Cuentas por cobrar | ✅ | Cartera total, monto vencido y clientes en mora; filtro «solo vencidos» | — | Cuadre nº 6. **Un cliente inactivo con deuda desaparece del reporte** |
| Abonos y cobros | ✅ | `Cliente.AplicarCobro` rechaza monto ≤ 0 y mayor que la deuda | — | **Cuadre nº 8.** Cobro parcial, total, excesivo y negativo |
| Límite de crédito | ✅ | `CreditoDisponible`, `ExcedeLimite`, `PuedeAsumirCredito`, aplicados al facturar | — | Los cinco casos límite de crédito de la Fase 4 |
| Antigüedad de saldos | ⚠️ | El cálculo **sí es real** por fecha de factura, con imputación FIFO. **Falta el corte por tramos** (0-30/31-60/61-90/+90) | La columna «Antigüedad (Días)» con un solo número | Se prueba lo que hay. **Ver restricción R-03** |
| Reportes de ingresos | ✅ | Separa contado y crédito, descarta anuladas, aparta el ITBIS | — | Cuadre nº 13 y rangos límite |
| Reporte de rentabilidad | ✅ | Usa `l.Cantidad * l.CostoUnitario` — el costo **congelado**, no `Producto.Costo` | — | **Cuadre nº 11: el margen de una factura vieja no se mueve tras una compra posterior** |
| Estado de resultados | ✅ | Ingresos − costo vendido − egresos. **No usa cobros ni pagos** | — | **Cuadre nº 13, calculado por SQL independiente** |
| Flujo de caja | ✅ | Contado + cobros − egresos − pagos. Excluye las ventas a crédito | — | **Cuadre nº 14, calculado por SQL independiente** |
| Panel de indicadores | ✅ | Cinco cifras reusando los cuatro reportes | — | **Cuadre nº 15. Ver riesgo R-01: zona horaria** |

---

## 5. Hallazgos no previstos por el encargo

Estos no estaban en la lista de sospechas y salieron de la auditoría. Se anticipan aquí porque
**condicionan el diseño del piloto**; su reporte formal irá en E7 con pasos de reproducción.

### D-01 · El usuario que registra cobros y pagos está escrito a mano — *severidad alta, tipo integridad*

`RegistrarCobro.razor:93` invoca `RegistrarCobroAsync(form, "Dionis")` y `RegistrarPago.razor:93`
invoca `RegistrarPagoAsync(form, "Dionis")`: **el nombre del desarrollador está escrito como
literal en lugar del usuario de la sesión.** Ninguna de las dos pantallas inyecta siquiera
`AuthenticationStateProvider`.

Consecuencia: **todo cobro a cliente y todo pago a proveedor queda firmado como «Dionis»**, sea
quien sea quien lo registre. Son justamente las dos operaciones donde se mueve dinero en
efectivo. El cuadre nº 16 (toda operación tiene usuario responsable) pasaría formalmente y sería
falso.

### D-02 · El registro de usuarios es público y no asigna rol — *severidad alta, tipo seguridad*

`/Account/Register` no lleva `[Authorize]` y está enlazada en el menú para no autenticados.
`Register.razor:84` crea el usuario y **nunca llama a `AddToRoleAsync`**.

Consecuencia: cualquiera que alcance la URL se crea una cuenta y, al entrar, **llega a las 20
pantallas que solo exigen `[Authorize]`** — inventario, punto de venta, facturas, compras,
clientes y cuentas por cobrar y por pagar. Solo Finanzas lo rechaza.

### H-03 · Zona horaria: el día del panel no es el día del colmado — *riesgo para el piloto*

`PanelFinanzasService` calcula «hoy» y «el mes» con `DateTime.UtcNow.Date`, y `Factura.Fecha`
también se guarda en UTC. República Dominicana es **UTC−4**.

Consecuencia: **a partir de las 8:00 p. m. hora local, las ventas ya cuentan en el día
siguiente.** Un colmado vende hasta las 9 o 10 de la noche. Debe verificarse con una venta
nocturna **antes** de la demostración, y afecta al cierre de cada jornada simulada.

### H-04 · La merma no impacta la utilidad — *severidad media, tipo datos*

La merma se registra y baja la existencia, pero el estado de resultados se arma con
`LineaFactura.CostoTotal + Egreso`. **La mercancía perdida desaparece del inventario sin
restar de la utilidad.** No hay reporte de mermas; cuantificarlas exige consultar la tabla.

Es relevante porque el anteproyecto nombra las «mermas no detectadas» como uno de los problemas
que el sistema debe resolver: hoy se detectan, pero no se valoran.

### H-05 · La anulación reescribe el pasado en lugar de registrar el presente

Anular una factura de un día anterior la excluye de los reportes de aquel día: el efectivo
desaparece **como si nunca hubiera entrado**, y no queda registro de la salida física del dinero
ni de cuándo ocurrió. El cuadre de caja de un día ya cerrado deja de cerrar.

Un `Egreso` **no** es el sustituto correcto: contarlo como gasto restaría la utilidad dos veces.

### H-06 · La devolución a proveedor no ajustaría el balance ni completándola

Además de las 6 capas faltantes, `DevolucionCompraService.ConfirmarAsync` **nunca toca al
proveedor**, a diferencia de `CompraService.RecibirAsync`. Aunque se agregaran repositorio,
DbSet, migración y pantalla, **devolver mercancía comprada a crédito dejaría la deuda intacta**,
incumpliendo lo que exige `Docs/Asignaciones.md:132`.

### H-07 · El costo promedio se puede pisar a mano

`Costo` es editable en el formulario de producto y `ProductoService.cs:123` lo sobrescribe. Una
edición manual **borra el costo promedio ponderado** que calculó la recepción de compras, y con
él la base del margen de las ventas siguientes.

### H-08 · `CLAUDE.md` está desactualizado sobre el e-CF

La sección «Huecos conocidos» afirma que «No existe el e-CF». En el commit auditado **sí existe**
(XML, e-NCF derivado, QR y pantalla). Lo que falta es la persistencia y el estado. Conviene
corregirlo antes de la sustentación: es una afirmación del propio equipo contra su propio código.

---

## 6. Precisiones sobre dos capacidades clave

### 6.1 Devolución a proveedor: exactamente qué falta

| Capa | Estado |
|---|---|
| Dominio (`DevolucionCompra`, `LineaDevolucionCompra`, `EstadoDevolucion`) | ✅ Completo, con 13 pruebas |
| Aplicación (servicio, contrato, DTO) | ✅ Completo |
| Implementación del repositorio | ❌ No existe |
| `DbSet` en el contexto | ❌ No existe |
| Configuración de EF | ❌ No existe |
| Tabla / migración | ❌ No existe |
| Registro en inyección de dependencias | ❌ No existe — inyectarlo lanzaría excepción |
| Pantalla | ❌ No existe |

Hay especificación y plan escritos en `Docs/superpowers/`. El trabajo se detuvo antes de tocar
EF Core. **Recomendación: declararla como no entregada en el alcance del piloto**, no como
parcial, porque no hay forma de ejercitarla.

### 6.2 Permisos: el gateo real, pantalla por pantalla

**Gateadas con política (7)** — todas con `Permisos.FinanzasVer`:
`/finanzas`, `/finanzas/ingresos`, `/finanzas/rentabilidad`, `/finanzas/estado-resultados`,
`/finanzas/flujo-caja`, `/finanzas/egresos`, `/finanzas/egresos/categorias`.
Además, dos bloques de acción con `AuthorizeView Policy="finanzas.egreso"`.

**Con `[Authorize]` pelado (20)** — cualquier sesión entra:
`/`, `/ventas`, `/ventas/facturas`, `/ventas/facturas/{id}`, `/ventas/facturas/{id}/ecf`,
`/ventas/ncf`, `/inventario`, `/inventario/productos`, `/inventario/productos/{id}`,
`/inventario/categorias`, `/clientes`, `/clientes/{id}`, `/clientes/cuentas-por-cobrar`,
`/clientes/{id}/cobro`, `/compras`, `/compras/{id}`, `/compras/proveedores`,
`/compras/proveedores/{id}`, `/compras/cuentas-por-pagar`, `/proveedores/{id}/pago`.

**De los 17 permisos del catálogo, solo 2 se exigen en algún sitio.** Los otros 15 —incluido
`sistema.usuarios`— están declarados y no se usan.

**El menú no está gateado:** `NavMenu.razor` usa `AuthorizeView` sin política, así que el enlace
de Finanzas se le muestra a todo el mundo y el bloqueo aparece al hacer clic.

---

## 7. Restricciones que este análisis impone al diseño del piloto

Se listan aquí porque obligan a ajustar las fases 2 y 3 **antes** de ejecutarlas.

**R-01 · El reloj es UTC y el colmado no.** Toda jornada simulada debe fijar su ventana en UTC,
y las ventas «de la noche» caen en el día siguiente del panel. Ver H-03.

**R-02 · `Pago.Fecha` es siempre `DateTime.UtcNow`** y no se puede capturar. Los pagos a
proveedor **no admiten fecha pasada**, así que la simulación de 20 días debe desplazarlos por
SQL después de registrarlos, con la verificación de saldos que exige el encargo. Debe
comprobarse si `Cobro.Fecha` tiene la misma restricción.

**R-03 · La antigüedad usa los `DiasCredito` actuales del cliente**, no los pactados al
facturar. Cambiar los días de crédito de un cliente **reescribe la antigüedad de sus facturas
viejas**. El calendario del piloto no debe modificar días de crédito a mitad del ejercicio, o
debe hacerlo a propósito y documentarlo como caso de prueba.

**R-04 · Cobros y pagos no se aplican a un documento concreto.** No hay `FacturaId` en `Cobro`
ni `CompraId` en `Pago`: la imputación es FIFO calculada. El cuadre nº 8 debe verificarse sobre
la cadena de balances, no sobre la imputación documento a documento, que no existe.

**R-05 · Sin gestión de usuarios en pantalla**, los cinco usuarios de la Fase 8 hay que crearlos
por `/Account/Register` y asignarles rol y permisos **por SQL**. El procedimiento debe quedar
escrito en E3 para que sea reproducible.

---

## 8. Alcance resultante para el resto del piloto

| Fase | Estado | Ajuste |
|---|---|---|
| 1 · Entorno | ✅ Ejecutable | Hecho — ver E1 |
| 2 · Diseño del piloto | ✅ Ejecutable | Con las restricciones R-01 a R-05 |
| 3 · Ejecución por día | ✅ Ejecutable | **Sin caja, sin medios de pago, sin descuentos, sin devoluciones al cliente.** El día de «devolución a proveedor» del calendario debe sustituirse por ajuste manual, y declararse |
| 4 · Casos límite | ⚠️ Parcial | De los 40 intentos del encargo, **los que dependen de capacidades ausentes no se ejecutan**; se reportan como no aplicables con su motivo |
| 5 · Integridad (17 cuadres) | ✅ Ejecutable | Es el núcleo del trabajo. Todos los cuadres son verificables por SQL |
| 6 · Concurrencia | ✅ Ejecutable | Multi-usuario, no multi-caja. Declararlo |
| 7 · Rendimiento | ✅ Ejecutable | Con la advertencia de hardware de E1 §3.2 |
| 8 · Seguridad | ✅ Ejecutable | **Es la fase con más hallazgos esperables**: 20 pantallas sin gatear y registro público |

### 8.1 Las pruebas se ejecutan también desde la interfaz — requisito firme

No basta con invocar servicios y verificar por SQL. **Toda capacidad marcada como *Existe* debe
ejercitarse además desde la pantalla real**, con navegador automatizado (Playwright sobre
Chromium) contra la aplicación Blazor Server corriendo.

Motivo: buena parte de los defectos que este piloto debe encontrar **solo son visibles desde la
interfaz** y son invisibles para un arnés de servicios. Tres ya identificados lo demuestran:

- **D-01** (la firma «Dionis» de cobros y pagos) vive en el `.razor`, no en el servicio: un
  arnés que llame a `RegistrarCobroAsync` con el usuario correcto **nunca lo detectaría**.
- **D-02** (registro público sin rol) solo se ve navegando a la URL sin sesión.
- Los **20 pantallas sin gatear** de §6.2 se prueban tecleando la dirección en la barra, que es
  precisamente como un empleado se saltaría el menú.

Reparto de la cobertura por vía de ejecución:

| Vía | Qué cubre | Volumen |
|---|---|---|
| Interfaz (Playwright) | Punto de venta, editores, impresión, e-CF, todas las pantallas de cada módulo, la matriz de seguridad rol × pantalla y **toda la evidencia visual** | **Mínimo 40 operaciones completas**, repartidas entre los cinco módulos, más las 27 rutas de la Fase 8 |
| Arnés de integración (C#) | El volumen de las 500+ operaciones encadenadas de los 20 días | El resto |
| SQL de verificación | Los 17 cuadres de integridad | Por día y al cierre |

Los casos límite de la Fase 4 que produzcan mensajes de error **se ejecutan por interfaz de
forma obligatoria**, porque parte del veredicto es si el mensaje que ve el comerciante es
comprensible o es una excepción cruda: eso no se puede juzgar desde un servicio.

**Conclusión del alcance:** el piloto se ejecuta, pero **no puede simular una jornada de
mostrador completa**. Puede simular con rigor el ciclo documental de 20 días —comprar, recibir,
vender, facturar, fiar, cobrar, pagar, gastar y reportar— que es exactamente lo que el
anteproyecto se comprometió a demostrar. Lo que queda fuera se reporta como ausencia con su
evidencia, no se disimula.
