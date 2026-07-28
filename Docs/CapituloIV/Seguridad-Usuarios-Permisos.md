# Capítulo IV — Sección 4.8: Gestión de Usuarios y Permisos (Seguridad)

Trabajo Final de Grado · Grupo 6 · Universidad Dominicana O&M
Autor del módulo: **Jeison Luis Santos** · rama `jeison/seguridad` · construido el 2026-07-28.

> Esta sección documenta el módulo transversal de **seguridad**: la administración de
> usuarios y el **control de acceso por permisos**. Amplía lo que el arranque (F0) ya
> resolvía —login, usuarios y roles con ASP.NET Core Identity— con lo que faltaba: decidir
> **qué puede hacer cada usuario** una vez dentro. El plan por rama vive en
> `Docs/Seguridad-Permisos.md`.

---

## 4.8.1 Qué resuelve y qué problema atacaba

Un sistema con login responde una sola pregunta: **¿quién eres?** (autenticación). Pero un
comercio real con empleados necesita responder una segunda: **¿qué te dejo hacer aquí
dentro?** (autorización). El dueño del minimarket piloto no quiere que su cajero vea la
utilidad del negocio, ni las compras, ni pueda anular facturas — solo quiere que **facture**.

Antes de este módulo, el sistema tenía la primera pregunta resuelta y la segunda **no**.
Los cuatro roles (`Administrador`, `Cajero`, `Almacén`, `Supervisor`) estaban **definidos en
el código pero no gobernaban nada**: cada pantalla llevaba solo `[Authorize]`, que significa
"con sesión iniciada", sin distinguir rol ni permiso. En la práctica, **cualquier usuario
con sesión abierta llegaba a Finanzas, a Compras y a todo**. Y no existía ninguna pantalla
para que el dueño creara un empleado ni le asignara acceso.

El módulo cierra ese hueco con tres piezas: un **catálogo de permisos**, un **motor de
autorización** que los hace cumplir, y **dos pantallas** para administrar usuarios y prender
o apagar sus permisos uno por uno.

---

## 4.8.2 Decisiones de diseño y su justificación

### Decisión 1 — Permisos por usuario (no por rol fijo)

Se evaluaron tres modelos de control de acceso:

- **Roles fijos:** cada rol trae un juego de módulos cerrado y el usuario "es" su rol.
  Simple, pero rígido: no permite "este cajero además cobra, pero aquel no".
- **Permisos por rol (RBAC clásico):** los permisos cuelgan del rol; el usuario los hereda.
  Flexible por rol, pero no por persona.
- **Permisos por usuario, con el rol como plantilla:** **el elegido.** Cada usuario tiene su
  propio juego de permisos, editable uno por uno; el rol solo sirve para **pre-marcar** un
  conjunto al crearlo.

Se eligió el tercero porque es exactamente lo que el dueño pidió: *"una pantalla donde
seleccione a un usuario y pueda activar o desactivar permisos"*. El control es **por
persona**, no por categoría. El rol no desaparece —sigue siendo el atajo que evita marcar
quince casillas a mano por cada empleado— pero deja de ser una jaula.

### Decisión 2 — Los permisos se guardan como *claims* del usuario, sin tabla nueva

Un permiso concedido es un **claim** de tipo `permiso` sobre el usuario, guardado en la
tabla `AspNetUserClaims` que **Identity ya crea**. Prender un permiso es agregar un claim;
apagarlo es quitarlo. No hizo falta ninguna tabla ni entidad nueva de permisos.

La razón es doble: es el mecanismo **idiomático** de ASP.NET Core para permisos por usuario
(los claims viajan en la cookie de sesión y el framework sabe leerlos), y **la fontanería ya
existía** —la tabla estaba creada desde la migración inicial de Identity—. Inventar una
tabla `PermisosUsuario` propia habría sido reconstruir lo que el framework regala.

### Decisión 3 — Enforcement con un *policy provider* a la medida que delega en el default

Para que una pantalla se escriba `[Authorize(Policy = "ventas.facturar")]` sin tener que
**declarar a mano** una política por cada uno de los diecisiete permisos en el arranque, se
construyó un `PermisoPolicyProvider` que **crea la política al vuelo**: cuando alguien pide
una política cuyo nombre es un permiso del catálogo, la fabrica en el momento; para
cualquier otro nombre, **delega en el proveedor por defecto**.

Esa delegación es la decisión importante: significa que el `[Authorize]` simple (que solo
exige sesión) y cualquier política del framework **siguen funcionando exactamente igual**.
El módulo de seguridad se añadió **sin romper una sola línea** del código de autorización
que ya existía.

### Decisión 4 — El Administrador tiene acceso total por regla, no por claims

El `PermisoAuthorizationHandler` concede cualquier permiso si el usuario está en el rol
`Administrador` (bypass), **antes** de mirar sus claims. Así el administrador no necesita
tener los diecisiete permisos marcados: manda por su rol.

Se hizo por regla y no sembrándole todos los claims porque es más robusto: si mañana se
agrega un permiso nuevo al catálogo, el administrador lo tiene **automáticamente**, sin que
nadie tenga que acordarse de marcárselo. La matriz, para un administrador, queda
informativa: se muestra un aviso de que tiene todo por regla.

### Decisión 5 — "Desactivar" es un bloqueo, no un borrado

La pantalla de usuarios ofrece **desactivar**, no borrar físicamente. Desactivar usa el
**bloqueo de Identity** (`LockoutEnd` en el futuro lejano); el usuario deja de poder entrar
pero su fila permanece.

La razón es de integridad y auditoría: las entidades `Cobro`, `Pago`, `Egreso` y `Compra`
**guardan el `UsuarioId`** de quién las registró. Borrar físicamente a un empleado que ya
operó **perdería el rastro** de quién hizo cada cobro, cada pago, cada gasto — y la
auditoría es medio proyecto. Bloquear conserva ese rastro. El borrado físico solo tendría
sentido para un usuario que nunca registró nada, y ni siquiera se expuso, por prudencia.

### Decisión 6 — Guardrail: no se puede dejar el sistema sin administrador

El servicio impide **desactivar** o **quitarle el rol de administrador** al **único
administrador activo** que quede. Es un candado contra el auto-encierro: sin él, un
administrador podría, de un clic, dejar a todo el mundo fuera del sistema sin manera de
volver a entrar. La comprobación cuenta los administradores activos distintos del afectado;
si no queda ninguno, la operación se rechaza con un mensaje claro.

### Decisión 7 — Al cambiar permisos o estado, se refresca el *security stamp*

Cuando el administrador cambia los permisos de un usuario, o lo desactiva, el servicio llama
a `UpdateSecurityStampAsync`. La razón es sutil pero real: la sesión del usuario vive en una
cookie con sus claims **de cuando entró**; el revalidador de Identity la acepta mientras el
*security stamp* no cambie. Refrescarlo **invalida la cookie vieja** y fuerza que el cambio
surta efecto sin esperar a que el usuario cierre y vuelva a abrir sesión. Sin esto, quitarle
un permiso a alguien no tendría efecto hasta su próximo login — un agujero silencioso.

### Decisión 8 — El contrato en Aplicación, la implementación en Infraestructura

El servicio de gestión de usuarios envuelve el `UserManager` de Identity, que es una pieza
de **Infraestructura**. Para respetar las capas, la **interfaz** (`IGestionUsuariosService`)
vive en `Application` —para que la capa web dependa de la abstracción y no de Identity— y la
**implementación** vive en `Infrastructure`, junto a lo demás que habla con Identity y la
base. La pantalla inyecta la interfaz sin saber que detrás hay un `UserManager`.

### Resumen de decisiones

| # | Decisión | Elección | Razón corta |
|---|----------|----------|-------------|
| 1 | Modelo de acceso | Permisos por usuario, rol como plantilla | Control por persona, que es lo que se pidió |
| 2 | Almacenamiento | Claims en `AspNetUserClaims` | Idiomático y sin tabla nueva; la fontanería ya existía |
| 3 | Enforcement | Policy provider al vuelo que delega en el default | No declarar N políticas y no romper los `[Authorize]` |
| 4 | Administrador | Acceso total por regla (bypass) | Robusto: hereda permisos nuevos sin marcarlos |
| 5 | "Borrar" | Bloqueo, no borrado físico | Conserva el rastro de `UsuarioId` en los documentos |
| 6 | Último admin | Guardrail que lo protege | Impide el auto-encierro del sistema |
| 7 | Efecto inmediato | Refrescar el security stamp | El cambio surte efecto sin re-login |
| 8 | Capas | Contrato en Application, impl en Infrastructure | La web depende de la abstracción, no de Identity |

---

## 4.8.3 El catálogo de permisos

El corazón del módulo es `src/MiniERP.Domain/Core/Permisos.cs`, hermano de `Roles.cs`. Es
una clase estática, sin dependencias, que declara **diecisiete permisos** agrupados por
módulo. Cada permiso es una constante con un valor `modulo.accion`:

| Módulo | Permisos |
|--------|----------|
| Ventas | `ventas.facturar` · `ventas.ver` · `ventas.anular` · `ventas.ncf` |
| Inventario | `inventario.ver` · `inventario.editar` · `inventario.ajustar` |
| Compras | `compras.ver` · `compras.recibir` · `compras.pagar` · `compras.devolver` |
| Clientes | `clientes.ver` · `clientes.editar` · `clientes.cobrar` |
| Finanzas | `finanzas.ver` · `finanzas.egreso` |
| Sistema | `sistema.usuarios` |

`Permisos` es la **única fuente de verdad**: expone el `Catalogo` (los módulos con sus
permisos y etiquetas, que dibuja la matriz), la lista `Todos`, el `EsPermiso(...)` que el
policy provider usa para decidir si construye una política o delega, y el
`PorDefectoDeRol(...)` que define la **plantilla** de cada rol. Que un permiso sea una
constante y no un texto suelto evita el error clásico de escribir mal `"ventas.facturar"` en
una pantalla y que la política nunca calce.

Las plantillas por rol se definen así, siguiendo la intención escrita en `Roles.cs`:

| Rol | Permisos por defecto |
|-----|----------------------|
| Cajero | facturar · ver ventas · ver inventario · ver clientes |
| Almacén | ver/editar/ajustar inventario · ver/recibir/devolver compras |
| Supervisor | solo los `*.ver` (consulta sin modificar) |
| Administrador | todo (aunque manda por regla, no por claims) |

---

## 4.8.4 El motor de autorización

En `src/MiniERP.Infrastructure/Seguridad/` viven tres piezas pequeñas que hacen cumplir los
permisos, siguiendo el patrón documentado de *custom authorization policy providers* de
ASP.NET Core:

- **`PermisoRequirement`** — un requisito que lleva el permiso exigido (p. ej.
  `ventas.facturar`).
- **`PermisoPolicyProvider`** — implementa `IAuthorizationPolicyProvider`. Para un nombre que
  `Permisos.EsPermiso` reconoce, construye una política con el requisito; para el resto,
  delega en un `DefaultAuthorizationPolicyProvider` interno. Se registra como **singleton**,
  como exige el framework.
- **`PermisoAuthorizationHandler`** — resuelve el requisito: concede si el usuario es
  `Administrador` (bypass) **o** tiene el claim del permiso; en cualquier otro caso, la
  política falla.

Se registran en `DependencyInjection.Seguridad.cs` (método `AddSeguridad`), encadenado desde
el `AddInfrastructure` raíz. El registro llama a `AddAuthorization()` para asegurar los
servicios base y luego sustituye el policy provider por el propio. Con eso, cualquier
pantalla —de cualquier módulo— puede escribir `[Authorize(Policy = Permisos.X)]` o el menú
puede usar `<AuthorizeView Policy="…">`, y el motor decide.

---

## 4.8.5 El servicio de gestión de usuarios

`IGestionUsuariosService` (en `Application/Seguridad/`) define los casos de uso, y
`GestionUsuariosService` (en `Infrastructure/Seguridad/`) los implementa sobre `UserManager`:

- **Listar / obtener** usuarios, mapeando a DTOs (nombre, correo, rol y estado activo).
- **Crear** — crea el usuario, lo asigna al rol y **le siembra los permisos por defecto del
  rol** como claims; luego se afinan en la matriz.
- **Editar** — nombre y rol; al cambiar de rol reemplaza la pertenencia. Aplica el guardrail
  del último administrador.
- **Activar / desactivar** — bloqueo de Identity, con el guardrail y el refresco del stamp.
- **Resetear clave** — genera una clave aleatoria que cumple las reglas de Identity y la
  devuelve **una sola vez** para entregarla.
- **Obtener / guardar permisos** — lee los claims de tipo `permiso`, y al guardar calcula el
  diferencial (qué agregar, qué quitar) y refresca el stamp.

Las operaciones que pueden fallar devuelven `Resultado`/`Resultado<T>` —el mismo patrón que
el resto del sistema— para que la pantalla muestre el error como un mensaje, no como una
excepción.

---

## 4.8.6 Persistencia: el nombre del usuario y su migración

`ApplicationUser` estaba vacío (solo lo que aporta `IdentityUser`). Se le agregó un campo
**`Nombre`** (el nombre para mostrar del empleado, que administra el dueño), con largo máximo
de 120. Es el único cambio de esquema del módulo, y se materializó con la migración
`AgregaNombreUsuario`, que agrega una columna `nvarchar(120)` a `AspNetUsers` con valor por
defecto vacío para las filas existentes.

Como manda la disciplina del proyecto —un solo `MiniErpDbContext`, un solo `ModelSnapshot`,
migraciones en serie— fue la **única** migración de este addendum: las tres ramas del sprint
de seguridad se repartieron de modo que solo la de Jeison (el núcleo) crea migración; Dionis
y Samuel solo agregan atributos `[Authorize]` en sus pantallas, sin tocar el esquema. El
administrador sembrado recibe su `Nombre` en `DatabaseInitializer`.

---

## 4.8.7 Interfaz de usuario

Tres pantallas nuevas en `src/MiniERP.Web/Components/Pages/Seguridad/`, con el mismo estilo
Bootstrap del resto del sistema, todas protegidas por `[Authorize(Policy = "sistema.usuarios")]`:

1. **Usuarios** (`/usuarios`) — la lista: nombre, correo, rol y estado, con acciones de
   **crear, editar, permisos, resetear clave y activar/desactivar**. Al resetear, muestra la
   clave nueva una sola vez.
2. **Editor** (`/usuarios/nuevo` y `/usuarios/{id}`) — alta y edición. Al crear pide nombre,
   correo, rol y clave inicial, y aplica el preset del rol.
3. **Permisos** (`/usuarios/{id}/permisos`) — **la matriz**: por cada módulo, los permisos
   como interruptores, marcados según lo concedido. Botones para "cargar el preset del rol" y
   "quitar todos". Para un administrador, muestra el aviso de acceso total en lugar de la
   matriz.

Además, se agregó el enlace **Usuarios** al menú lateral, visible solo para quien tenga
`sistema.usuarios`.

---

## 4.8.8 Enforcement: dónde se hace cumplir

El control se aplica en **dos frentes**, ambos con las políticas del catálogo:

- **El menú** (`NavMenu.razor`): cada enlace se envuelve en `<AuthorizeView Policy="…">`, de
  modo que el usuario **solo ve los módulos que puede usar**.
- **Las rutas** de las pantallas de Inventario y Ventas (los módulos de Jeison), con
  `[Authorize(Policy = …)]`, de modo que escribir la URL a mano **rebota** a quien no tiene
  el permiso:

| Pantalla | Política exigida |
|----------|------------------|
| Inventario, Productos | `inventario.ver` |
| Categorías, ProductoEditor | `inventario.editar` |
| Punto de venta (POS) | `ventas.facturar` |
| Facturas, detalle, e-CF | `ventas.ver` |
| Secuencias NCF | `ventas.ncf` |

El menú lo cierra Jeison entero (es archivo compartido); el enforcement de rutas de
**Compras, Clientes y Finanzas** corresponde a **Dionis y Samuel** en sus ramas, porque son
sus carpetas — la frontera del proyecto se respeta también aquí.

---

## 4.8.9 Verificación

**Verificación automática (hecha):**

| Prueba | Resultado |
|--------|-----------|
| Compilación de la solución | **0 errores, 0 advertencias.** |
| Arranque en ejecución (headless) | La app levanta sin errores con los servicios nuevos. |
| Aplicación de la migración | Al arrancar: *"Aplicando 1 migración(es) pendiente(s): AgregaNombreUsuario"* → *"Base de datos actualizada"*. |

**Plan de verificación funcional en navegador (el "gate" del módulo):**

1. Entrar como administrador → crear un empleado con rol **Cajero**.
2. En su matriz de permisos, **apagar todo menos `ventas.facturar`** → guardar.
3. Iniciar sesión como el Cajero: el menú **solo muestra "Punto de venta"**; teclear
   `/finanzas` o `/compras` **rebota** a no autorizado.
4. Como administrador, intentar **desactivar al único administrador** → el sistema **no lo
   permite**.

Estas pruebas funcionales, con sus capturas, quedan como el paso de cierre a ejecutar sobre
la aplicación real corriendo contra SQL Server, siguiendo la misma disciplina del resto del
proyecto: no basta con que compile, hay que verlo funcionar.

---

## 4.8.10 Alcance y limitaciones

Lo que el módulo **sí** y **todavía no** hace, dicho con franqueza:

- **Enforcement a nivel de ruta:** hecho. Entrar a una pantalla sin permiso rebota.
- **Enforcement a nivel de botón:** pendiente como pulido. Dentro de una pantalla a la que sí
  se tiene acceso, esconder botones de acción concretos por permiso fino (p. ej. ver la lista
  pero no el botón de editar) es una mejora posterior; hoy la protección es por pantalla.
- **Un rol por usuario:** la interfaz asume un rol principal por usuario. Identity permitiría
  varios, pero el caso del comercio no lo necesita.
- **Auto-protección del no-administrador:** el guardrail cubre al último administrador; quitar
  `sistema.usuarios` a un no-administrador que lo tuviera es un borde menor no blindado.

Estas fronteras son deliberadas: se construyó primero el control que el problema exige —que
un empleado no vea lo que no debe— y se dejó el pulido fino como trabajo incremental.

---

## 4.8.11 Encaje en el proyecto

Este es un módulo **transversal**, no uno de los cinco módulos de negocio. Por eso se
repartió en tres ramas: Jeison construye el **núcleo** (catálogo, motor, pantallas, menú, y
el enforcement de sus módulos Inventario y Ventas) y va primero; Dionis y Samuel, después,
gatean **sus** pantallas contra las mismas políticas. Amplía la sección 4.8 del informe, que
el arranque (F0) había dejado en "login, usuarios y roles": ahora los roles, por fin,
**gobiernan**.

---

*Registro de commits del módulo (rama `jeison/seguridad`):*
`Seguridad: núcleo de permisos, migración y servicio de usuarios` (backend — dominio,
aplicación e infraestructura) · `Seguridad: pantallas de usuarios/permisos y enforcement`
(web — las tres pantallas, el menú gateado y los `[Authorize]` de Inventario/Ventas) ·
`Docs: sección 4.8 del Capítulo IV`.
