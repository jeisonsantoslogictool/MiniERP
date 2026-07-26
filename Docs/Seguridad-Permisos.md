# Seguridad — Gestión de usuarios y permisos

Trabajo Final de Grado · Grupo 6 · Mini ERP

Addendum al [Plan Maestro](Plan-Maestro.md). Reparte en **tres ramas** una funcionalidad
que se nos pasó en la planificación: controlar **qué puede hacer cada usuario**, no solo
quién entra.

---

## El hueco que esto cierra

Hoy los cuatro roles de [Roles.cs](../src/MiniERP.Domain/Core/Roles.cs) —Administrador,
Cajero, Almacén, Supervisor— **están definidos pero no gobiernan nada**. Cada pantalla
lleva solo `[Authorize]` («con sesión iniciada»); no hay ni un `[Authorize(Roles=…)]` ni
una política en toda la app. Es decir: **cualquier usuario con sesión abierta llega a
Finanzas, a Compras y a todo.** Y no existe pantalla para que el dueño cree un empleado ni
le asigne acceso.

Esto **completa y amplía la sección 4.8** del informe («usuarios y roles con Identity»):
pasa de tener roles nombrados a tener autorización real, por usuario.

---

## Qué se construye

**Dos pantallas** (nuevas, dueño: Jeison / plataforma):

1. **Usuarios** — tabla con nombre, correo, estado y rol; **crear, editar, activar/
   desactivar, resetear contraseña**.
2. **Permisos del usuario** — seleccionas un usuario y ves una **matriz de permisos por
   módulo** con interruptores on/off. «Solo facturar» = dejas prendido únicamente
   `ventas.facturar`.

**El modelo, en cuatro piezas:**

- **Catálogo de permisos** — un `Permisos.cs` en `Domain/Core`, hermano de `Roles.cs`:

  | Módulo | Permisos (constante) |
  |--------|----------------------|
  | Ventas | `ventas.facturar` · `ventas.anular` · `ventas.ver` · `ventas.ncf` |
  | Inventario | `inventario.ver` · `inventario.editar` · `inventario.ajustar` |
  | Compras | `compras.ver` · `compras.recibir` · `compras.pagar` · `compras.devolver` |
  | Clientes | `clientes.ver` · `clientes.editar` · `clientes.cobrar` |
  | Finanzas | `finanzas.ver` · `finanzas.egreso` |
  | Sistema | `sistema.usuarios` |

- **Almacenamiento = claims por usuario.** Cada interruptor es un claim de tipo `permiso`
  en `AspNetUserClaims` (`UserManager.AddClaimAsync/RemoveClaimAsync`). **Sin tabla nueva.**
- **Enforcement = políticas.** Un `IAuthorizationPolicyProvider` a la medida convierte cada
  permiso en una política; un `AuthorizationHandler` la concede si el usuario tiene el claim
  **o es Administrador** (bypass total). Las pantallas usan
  `@attribute [Authorize(Policy = Permisos.VentasFacturar)]`; el menú, `<AuthorizeView Policy="…">`.
- **Roles = plantillas.** Al crear un empleado eliges un rol y eso *pre-marca* su juego de
  permisos; después lo afinas por usuario. Nadie marca 15 casillas a mano.

**La fontanería que ya existe** (por eso es abordable): `AspNetUserClaims` está creada desde
la migración inicial; `Cobro/Pago/Egreso/Compra` ya guardan `UsuarioId`; los roles ya se
siembran y el admin ya tiene el suyo.

---

## El reparto en tres ramas

| Rama | Quién | Dueño de este addendum | Depende de |
|------|-------|------------------------|------------|
| `jeison/seguridad` | **Jeison** | El núcleo (catálogo, políticas, siembra), las **dos pantallas**, el menú, y el enforcement de **Inventario** y **Ventas** | — (va primero) |
| `dionis/permisos` | **Dionis** | Enforcement de **Clientes** y **Compras** | Núcleo de Jeison en `main` |
| `samuel/permisos` | **Samuel** | Enforcement de **Finanzas** | Núcleo de Jeison en `main` |

**No es totalmente paralelo: hay dos fases.**

- **Fase A — Jeison, en solitario.** Construye el núcleo, las pantallas y siembra las
  plantillas de rol. Sin esto, las constantes de permiso y el policy provider no existen y
  los demás no pueden gatear nada. **Entra a `main` antes de que Dionis y Samuel empiecen.**
- **Fase B — Dionis y Samuel, en paralelo.** Cada uno trae `main`, ramifica, y gatea **sus
  propias** pantallas. Trabajo pequeño pero real: es en dos niveles (ruta + botón).

Esto respeta la frontera del proyecto: **cada quien gatea solo sus carpetas.** El núcleo y
lo compartido (el menú, la siembra) son de Jeison como dueño de la plataforma/Identity (F0).

---

## Rama 1 — Jeison · `jeison/seguridad`

Dueño del núcleo y de sus propios módulos. Va primero y en solitario.

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 1 | **Catálogo `Permisos.cs`** | Existe la lista de constantes de arriba, agrupada por módulo, con un `Todos[]` como `Roles.Todos`. |
| 2 | **Policy provider + handler** | `[Authorize(Policy = "ventas.facturar")]` bloquea a quien no tiene el claim; el Administrador pasa siempre (bypass). Registrado en `DependencyInjection`. |
| 3 | **`ApplicationUser.Nombre`** | Se agrega la propiedad `Nombre` y **su migración** (una columna). Es la **única migración** de todo el addendum. |
| 4 | **Servicio de gestión de usuarios** | En `Application`/`Infrastructure`, envuelve `UserManager`: listar, crear, editar, desactivar (bloqueo de Identity, no borrado), resetear clave, leer/escribir sus permisos-claim. |
| 5 | **Pantalla Usuarios** | Tabla con crear/editar/desactivar-reactivar/resetear. **Borrado físico solo si el usuario nunca registró nada**; con historial, se desactiva (si no, se pierde el rastro de `UsuarioId`). |
| 6 | **Pantalla Permisos del usuario** | Seleccionas un usuario → matriz por módulo con interruptores → guardar. Refleja y edita sus claims. |
| 7 | **Siembra de plantillas de rol** | `DatabaseInitializer` siembra el juego de permisos por defecto de cada rol (Cajero = ventas + consulta; Almacén = inventario + compras; Supervisor = solo `*.ver`). El admin, todo. |
| 8 | **Menú gateado (`NavMenu.razor`)** | Cada `<NavLink>` envuelto en `<AuthorizeView Policy="…">`. Lo hace Jeison **entero** para que Dionis y Samuel no toquen el archivo compartido. |
| 9 | **Enforcement de Inventario y Ventas** | Cada pantalla de `Inventario/` (Categorias, Inventario, ProductoEditor, Productos) y `Ventas/` (Ventas, Facturas, FacturaDetalle, FacturaEcf, Secuencias) con su `[Authorize(Policy=…)]`, y los botones de acción (facturar, anular, editar) escondidos por permiso. |
| 10 | **Guardrails** | No se puede desactivar/borrar al **último Administrador**, ni quitarte a ti mismo `sistema.usuarios`. Nadie se deja fuera. |
| 11 | **Sección 4.8 del Capítulo IV** | Escrita, con capturas de las dos pantallas y del rebote de un empleado sin permiso. |

### Gate de Jeison

Creas un empleado, le asignas «Cajero» y en su pantalla de permisos apagas todo menos
`ventas.facturar`. Inicias sesión como él: **solo ve Facturar en el menú**, y si teclea a
mano `/finanzas` o `/compras`, **rebota** a no autorizado. Intentas desactivar al único
Administrador y el sistema **no te deja**.

---

## Rama 2 — Dionis · `dionis/permisos`

Gatea **Clientes** y **Compras**. Arranca cuando el núcleo de Jeison está en `main`.
Trae `main`, ramifica, y trabaja solo dentro de tus carpetas.

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 1 | **Ruta — Clientes** | `Clientes`, `ClienteEditor`, `CuentasPorCobrar`, `RegistrarCobro` con `[Authorize(Policy=…)]`: `clientes.ver` para consultar, `clientes.editar` para el editor, `clientes.cobrar` para el cobro. |
| 2 | **Ruta — Compras** | `Compras`, `CompraEditor`, `Proveedores`, `ProveedorEditor`, `CuentasPorPagar`, `RegistrarPago` gateadas: `compras.ver`, `compras.recibir`, `compras.pagar`, `compras.devolver`. |
| 3 | **Botón — nivel acción** | Dentro de cada pantalla, los botones de acción (guardar, cobrar, pagar, devolver) escondidos con `<AuthorizeView Policy="…">`. Un usuario con `clientes.ver` pero sin `clientes.editar` ve la lista y **no** el botón de guardar. |
| 4 | **Sección del Capítulo IV** | La parte de Clientes/Compras del 4.8, con capturas. |

### Gate de Dionis

El Cajero de arriba (sin permisos de Clientes/Compras) **no ve** esos ítems en el menú, y
`/clientes`, `/clientes/cobro`, `/compras`, `/compras/pago` le rebotan. Un usuario con
`clientes.ver` pero sin `clientes.editar` ve la ficha del cliente pero **no puede guardarla**.

---

## Rama 3 — Samuel · `samuel/permisos`

Gatea **Finanzas**. Arranca cuando el núcleo de Jeison está en `main`. Trae `main`,
ramifica, y trabaja solo dentro de `Finanzas/`.

| # | Tarea | Terminado cuando |
|---|-------|------------------|
| 1 | **Ruta — reportes** | `Finanzas`, `Ingresos`, `Rentabilidad`, `EstadoDeResultados`, `FlujoDeCaja` con `[Authorize(Policy = "finanzas.ver")]`. |
| 2 | **Ruta — egresos** | `Egresos` y `CategoriasEgreso` con `finanzas.egreso` para registrar; `finanzas.ver` para consultar. |
| 3 | **Botón — nivel acción** | El botón de registrar egreso escondido con `<AuthorizeView Policy="finanzas.egreso">`. El Supervisor (solo lectura) ve los reportes y **no** puede registrar. |
| 4 | **Sección del Capítulo IV** | La parte de Finanzas del 4.8, con capturas. |

### Gate de Samuel

El Cajero **no ve** Finanzas en el menú y `/finanzas` y sus reportes le rebotan. Un
**Supervisor** entra a los reportes pero, al no tener `finanzas.egreso`, **no ve el botón**
de registrar egreso. Coincide con la intención del rol en `Roles.cs`: «consulta sin modificar».

---

## Cómo no chocar

Las mismas reglas del proyecto, aplicadas a este addendum:

1. **Jeison primero.** Su núcleo entra a `main` **antes** de que Dionis y Samuel ramifiquen.
   Avisa «núcleo de permisos en main, hagan pull».
2. **Una sola migración, la de Jeison** (`ApplicationUser.Nombre`). Dionis y Samuel **no
   crean ninguna** —solo agregan atributos `[Authorize]` en Razor—, así que el `ModelSnapshot`
   no choca.
3. **El menú lo gatea Jeison entero.** `NavMenu.razor` es archivo compartido: si los tres lo
   tocan, chocan. Jeison lo cierra en la Fase A; los demás solo tocan **sus** pantallas.
4. **Cada quien, solo sus carpetas.** Dionis no abre `Finanzas/`; Samuel no abre `Compras/`.
   El núcleo y lo compartido son de Jeison.
5. **Cada rama trae su sección del 4.8.** Un módulo gateado no está cerrado hasta que su
   parte del informe existe, con capturas.

---

## Alcance y lugar en el plan

Esto **no estaba en el anteproyecto** más allá de «usuarios y roles con Identity». Es una
adición real y defendible —un comercio con empleados necesita que el cajero no vea la
utilidad ni las compras—, pero **cuesta tiempo** en un calendario ya mapeado a septiembre.

- **Dónde encaja:** como sprint corto y transversal **antes de F4** (integración y piloto):
  el piloto factura con empleados reales, así que conviene que el control de acceso exista
  antes de instalar.
- **Decisión de grupo:** confirmar que entra ahora y no después, y que Jeison asume el
  núcleo como dueño de la plataforma. Registrar el cambio en el Plan Maestro para que quede
  trazado, no colado por el borde.
