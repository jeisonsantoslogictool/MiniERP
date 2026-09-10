# AUDITORÍA TÉCNICA — MiniERP

**Fecha de ejecución:** 2026-09-10, 19:10–19:12 (UTC−04:00)
**Máquina:** DEVELOP-SRSANTO (Windows 11 Pro 10.0.22631)
**Repositorio auditado:** `D:\Desktop\Proyectos\MiniERP`
**Commit auditado:** `4f3a1f416d6f0dd5f9f29d52ea1ac67a5bd3c236` (rama `main`, árbol de trabajo limpio, igual a `origin/main`)

Criterio: todo lo que sigue se obtuvo ejecutando comandos o leyendo archivos del repositorio en el commit indicado. Donde un dato no pudo obtenerse del código ni de los artefactos disponibles se marca **no verificable**. No se modificó ningún archivo fuente; el único archivo añadido es este.

---

## 1. Entorno y compilación

### 1.1 `global.json`

```json
{
  "sdk": {
    "rollForward": "latestFeature",
    "version": "10.0.204"
  }
}
```

### 1.2 SDKs y runtimes instalados

| Comando | Resultado |
|---|---|
| `dotnet --version` | `10.0.301` |
| `dotnet --list-sdks` | `9.0.301`, `10.0.301` |
| Runtimes `Microsoft.AspNetCore.App` | 7.0.20, 9.0.6, 9.0.17, **10.0.9** |

**¿Coincide la versión declarada con la instalada?** No exactamente. `global.json` pide `10.0.204`; el SDK instalado es `10.0.301`. La política `rollForward: latestFeature` permite usar 10.0.301 (misma banda mayor.menor, feature superior), por lo que **la resolución es válida y no bloquea**, pero la versión declarada no es la que está instalada.

### 1.3 Compilación

Se ejecutó dos veces: `dotnet build MiniERP.slnx` (incremental) y `dotnet build MiniERP.slnx --no-incremental` (recompilación completa) para que el conteo de advertencias fuera fiable.

| Métrica | Valor |
|---|---|
| Resultado | **Compilación correcta** |
| Errores | **0** |
| Advertencias | **0** |
| Tiempo (recompilación completa) | 14.34 s |
| Proyectos compilados | 5: `MiniERP.Domain`, `MiniERP.Application`, `MiniERP.Infrastructure`, `MiniERP.Web`, `MiniERP.Tests` |
| Target framework | `net10.0` en los 5 proyectos |

Se verificó que ningún `.csproj` contiene `<NoWarn>`, `<TreatWarningsAsErrors>` ni `<WarningLevel>` que pudiera ocultar advertencias. El "0 advertencias" es real.

Versiones de paquetes centralizadas en `Directory.Packages.props`: EF Core / Identity 10.0.10, xunit 2.9.3, coverlet.collector 6.0.4, QRCoder 1.7.0.

---

## 2. Pruebas unitarias

### 2.1 Ejecución

Comando: `dotnet test MiniERP.slnx --collect:"XPlat Code Coverage" --logger trx`

| Métrica | Valor |
|---|---|
| Fecha/hora inicio (TRX `start`) | 2026-09-10 19:11:25.20 −04:00 |
| Fecha/hora fin (TRX `finish`) | 2026-09-10 19:11:36.55 −04:00 |
| **Total de pruebas** | **191** |
| Aprobadas | **191** |
| Fallidas | **0** |
| Omitidas | **0** |
| Duración reportada por el runner (solo ejecución) | 218 ms |
| Duración total incl. descubrimiento y cobertura | ≈ 11.3 s |
| Ensamblado | `tests/MiniERP.Tests/bin/Debug/net10.0/MiniERP.Tests.dll` |
| Requiere SQL Server | No (todas son en memoria) |

### 2.2 ¿Cuántas pruebas hay realmente vs. las 180 del documento?

**Hay 191 casos de prueba en `main`**, no 180. Conteo estático coincidente con el runner: 154 `[Fact]` + 37 `[InlineData]` (de 11 `[Theory]`) = 191.

| Archivo | `[Fact]` | `[Theory]` | `[InlineData]` | Casos |
|---|---|---|---|---|
| `tests/MiniERP.Tests/Clientes/ClienteTests.cs` | 10 | 2 | 9 | 19 |
| `tests/MiniERP.Tests/Clientes/CobroTests.cs` | 4 | 0 | 0 | 4 |
| `tests/MiniERP.Tests/Compras/CompraTests.cs` | 18 | 1 | 3 | 21 |
| `tests/MiniERP.Tests/Compras/DevolucionCompraTests.cs` | 13 | 0 | 0 | 13 |
| `tests/MiniERP.Tests/Compras/PagoTests.cs` | 4 | 0 | 0 | 4 |
| `tests/MiniERP.Tests/Finanzas/EgresoTests.cs` | 5 | 0 | 0 | 5 |
| `tests/MiniERP.Tests/Finanzas/EstadoResultadosTests.cs` | 3 | 0 | 0 | 3 |
| `tests/MiniERP.Tests/Finanzas/FlujoCajaTests.cs` | 3 | 0 | 0 | 3 |
| `tests/MiniERP.Tests/Finanzas/ResumenIngresosTests.cs` | 6 | 0 | 0 | 6 |
| `tests/MiniERP.Tests/Finanzas/ResumenRentabilidadTests.cs` | 8 | 0 | 0 | 8 |
| `tests/MiniERP.Tests/Inventario/ProductoServiceTests.cs` | 5 | 0 | 0 | 5 |
| `tests/MiniERP.Tests/Inventario/ProductoTests.cs` | 12 | 1 | 4 | 16 |
| `tests/MiniERP.Tests/Inventario/UnidadMedidaTests.cs` | 1 | 4 | 9 | 10 |
| `tests/MiniERP.Tests/Ventas/EcfServiceTests.cs` | 18 | 0 | 0 | 18 |
| `tests/MiniERP.Tests/Ventas/EcfTests.cs` | 10 | 2 | 8 | 18 |
| `tests/MiniERP.Tests/Ventas/FacturaTests.cs` | 19 | 0 | 0 | 19 |
| `tests/MiniERP.Tests/Ventas/SecuenciaNcfTests.cs` | 11 | 1 | 4 | 15 |
| `tests/MiniERP.Tests/Ventas/VentaServiceTests.cs` | 4 | 0 | 0 | 4 |
| **Total** | **154** | **11** | **37** | **191** |

Cifras que el propio repositorio afirma en distintos momentos (todas desactualizadas respecto a `main`): 118 (`Docs/CapituloIV/Egresos.md:254`), 144 (`Docs/CapituloIV/Reportes-Finanzas.md:37`), **180** (`Docs/CapituloIV/Entorno-Herramientas.md:55`, `Docs/CapituloIV/Permisos-Finanzas.md:187`), 185 (`CLAUDE.md:168`).

**Dato adicional:** la rama remota `origin/samuel/pruebas-finanzas` (9 commits del 2026-09-07/08, **no integrada en `main`**) añade 9 archivos de prueba (servicios de Finanzas + una prueba de integración de punta a punta). Conteo estático en esa rama: 232 `[Fact]` + 37 `[InlineData]` ≈ 269 casos; el mensaje del commit `113e5ef` afirma 267. No se ejecutó esa rama; la cifra es estática.

### 2.3 Cobertura de líneas

Archivo: `coverage.cobertura.xml` generado por coverlet.

| Ámbito | Líneas válidas | Líneas cubiertas | **Cobertura** |
|---|---|---|---|
| **Total (raíz del reporte)** | 14 974 | 893 | **5.96 %** |
| `MiniERP.Domain` | 658 | 525 | 79.79 % |
| `MiniERP.Application` | 1 725 | 368 | 21.33 % |
| `MiniERP.Infrastructure` | 12 591 | 0 | 0.00 % |
| ↳ de las cuales `Persistence/Migrations/*` (código generado) | ≈ 10 873 | 0 | 0.00 % |
| **Total excluyendo migraciones generadas** | ≈ 4 101 | 893 | **≈ 21.8 %** |
| `MiniERP.Web` | — | — | **No medido** (el proyecto de pruebas no lo referencia) |

Cobertura de ramas total: 22.8 % (203 / 890).

Lectura honesta: el dominio está bien cubierto (≈80 %); la capa de aplicación en un quinto; infraestructura (repositorios EF, Identity, DI, `DatabaseInitializer`) y la capa web tienen **cero** pruebas automatizadas. Servicios de aplicación con 0 % de cobertura en `main`: `ClienteService`, `CobrosService`, `CompraService`, `DevolucionCompraService`, `PagosService`, `ProveedorService`, `CategoriaEgresoService`, `EgresoService`, `EstadoResultadosService`, `FlujoCajaService`, `PanelFinanzasService`, `ReporteIngresosService`, `ReporteRentabilidadService`, `CategoriaService`, `SecuenciaNcfService`.

---

## 3. Control de acceso

### 3.1 Método

Se listaron todos los `.razor` bajo `src/MiniERP.Web` que contienen una directiva `@page` (patrón tolerante a BOM; 3 archivos del scaffold de Identity tienen BOM y un patrón estricto los omitía). Se revisó además `_Imports.razor` de cada carpeta (un `@attribute [Authorize]` ahí aplica a toda la carpeta), `Routes.razor`, `Program.cs`, `DependencyInjection*.cs` y los endpoints Minimal API.

Hechos estructurales:
- `Routes.razor` usa `AuthorizeRouteView` con `<NotAuthorized><RedirectToLogin/>`: una página con `[Authorize]` no autorizada redirige al login.
- **No existe `FallbackPolicy`** (ni en `Program.cs` ni en `AddSeguridad`): una página sin atributo es pública.
- Único endpoint no-Razor: los del scaffold de Identity en `Components/Account/IdentityComponentsEndpointRouteBuilderExtensions.cs` (`/Account/Logout`, passkeys, `/Account/Manage/*` con `RequireAuthorization()`). **No hay controladores ni Minimal API de negocio.**
- No existe `Register.razor`: el autorregistro está cerrado (commit `827e1fe`).

### 3.2 Inventario completo (60 archivos con `@page`, 65 rutas)

**Páginas de negocio — 29 archivos, 29 con `[Authorize(Policy=…)]`, 0 sin protección**

| Ruta(s) | Archivo (`src/MiniERP.Web/Components/Pages/`) | Atributo | Tipo |
|---|---|---|---|
| `/clientes` | `Clientes/Clientes.razor` | `[Authorize(Policy = "clientes.ver")]` | Policy |
| `/clientes/nuevo`, `/clientes/{Id:int}` | `Clientes/ClienteEditor.razor` | `[Authorize(Policy = "clientes.editar")]` | Policy |
| `/clientes/cuentas-por-cobrar` | `Clientes/CuentasPorCobrar.razor` | `[Authorize(Policy = "clientes.ver")]` | Policy |
| `/clientes/{ClienteId:int}/cobro` | `Clientes/RegistrarCobro.razor` | `[Authorize(Policy = "clientes.cobrar")]` | Policy |
| `/compras` | `Compras/Compras.razor` | `[Authorize(Policy = "compras.ver")]` | Policy |
| `/compras/nueva`, `/compras/{Id:int}` | `Compras/CompraEditor.razor` | `[Authorize(Policy = "compras.recibir")]` | Policy |
| `/compras/cuentas-por-pagar` | `Compras/CuentasPorPagar.razor` | `[Authorize(Policy = "compras.ver")]` | Policy |
| `/compras/proveedores` | `Compras/Proveedores.razor` | `[Authorize(Policy = "compras.ver")]` | Policy |
| `/compras/proveedores/nuevo`, `/compras/proveedores/{Id:int}` | `Compras/ProveedorEditor.razor` | `[Authorize(Policy = "compras.recibir")]` | Policy |
| `/proveedores/{ProveedorId:int}/pago` | `Compras/RegistrarPago.razor` | `[Authorize(Policy = "compras.pagar")]` | Policy |
| `/finanzas` | `Finanzas/Finanzas.razor` | `[Authorize(Policy = Permisos.FinanzasVer)]` | Policy |
| `/finanzas/egresos` | `Finanzas/Egresos.razor` | `[Authorize(Policy = Permisos.FinanzasVer)]` (+ `AuthorizeView Policy=FinanzasEgreso` en el formulario) | Policy |
| `/finanzas/egresos/categorias` | `Finanzas/CategoriasEgreso.razor` | `[Authorize(Policy = Permisos.FinanzasVer)]` (+ `AuthorizeView Policy=FinanzasEgreso`) | Policy |
| `/finanzas/estado-resultados` | `Finanzas/EstadoDeResultados.razor` | `[Authorize(Policy = Permisos.FinanzasVer)]` | Policy |
| `/finanzas/flujo-caja` | `Finanzas/FlujoDeCaja.razor` | `[Authorize(Policy = Permisos.FinanzasVer)]` | Policy |
| `/finanzas/ingresos` | `Finanzas/Ingresos.razor` | `[Authorize(Policy = Permisos.FinanzasVer)]` | Policy |
| `/finanzas/rentabilidad` | `Finanzas/Rentabilidad.razor` | `[Authorize(Policy = Permisos.FinanzasVer)]` | Policy |
| `/inventario` | `Inventario/Inventario.razor` | `[Authorize(Policy = Permisos.InventarioVer)]` | Policy |
| `/inventario/productos` | `Inventario/Productos.razor` | `[Authorize(Policy = Permisos.InventarioVer)]` | Policy |
| `/inventario/productos/nuevo`, `/inventario/productos/{Id:int}` | `Inventario/ProductoEditor.razor` | `[Authorize(Policy = Permisos.InventarioEditar)]` | Policy |
| `/inventario/categorias` | `Inventario/Categorias.razor` | `[Authorize(Policy = Permisos.InventarioEditar)]` | Policy |
| `/usuarios` | `Seguridad/Usuarios.razor` | `[Authorize(Policy = Permisos.SistemaUsuarios)]` | Policy |
| `/usuarios/nuevo`, `/usuarios/{Id}` | `Seguridad/UsuarioEditor.razor` | `[Authorize(Policy = Permisos.SistemaUsuarios)]` | Policy |
| `/usuarios/{Id}/permisos` | `Seguridad/PermisosUsuario.razor` | `[Authorize(Policy = Permisos.SistemaUsuarios)]` | Policy |
| `/ventas` | `Ventas/Ventas.razor` | `[Authorize(Policy = Permisos.VentasFacturar)]` | Policy |
| `/ventas/facturas` | `Ventas/Facturas.razor` | `[Authorize(Policy = Permisos.VentasVer)]` | Policy |
| `/ventas/facturas/{Id:int}` | `Ventas/FacturaDetalle.razor` | `[Authorize(Policy = Permisos.VentasVer)]` | Policy |
| `/ventas/facturas/{Id:int}/ecf` | `Ventas/FacturaEcf.razor` | `[Authorize(Policy = Permisos.VentasVer)]` | Policy |
| `/ventas/ncf` | `Ventas/Secuencias.razor` | `[Authorize(Policy = Permisos.VentasNcf)]` | Policy |

**Página de inicio — 1 archivo**

| Ruta | Archivo | Atributo | Tipo |
|---|---|---|---|
| `/` | `Home.razor` | `[Authorize]` | Sin parámetros (solo exige sesión) |

**Páginas de cuenta del scaffold de Identity, con sesión — 14 archivos** (`src/MiniERP.Web/Components/Account/Pages/Manage/`): `ChangePassword`, `DeletePersonalData`, `Disable2fa`, `Email`, `EnableAuthenticator`, `ExternalLogins`, `GenerateRecoveryCodes`, `Index` (`/Account/Manage`), `Passkeys`, `PersonalData`, `RenamePasskey/{Id}`, `ResetAuthenticator`, `SetPassword`, `TwoFactorAuthentication`. Ninguna lleva atributo propio; **todas heredan `@attribute [Authorize]` (sin parámetros) de `Manage/_Imports.razor`**.

**Páginas de cuenta del scaffold de Identity, anónimas por diseño — 14 archivos** (`Components/Account/Pages/`): `AccessDenied`, `ConfirmEmail`, `ConfirmEmailChange`, `ForgotPassword`, `ForgotPasswordConfirmation`, `InvalidPasswordReset`, `InvalidUser`, `Lockout`, `Login`, `LoginWith2fa`, `LoginWithRecoveryCode`, `ResendEmailConfirmation`, `ResetPassword`, `ResetPasswordConfirmation`. Sin atributo. Son los flujos de entrada/recuperación; deben ser públicos.

**Páginas utilitarias — 2 archivos**: `Pages/Error.razor` (`/Error`), `Pages/NotFound.razor` (`/not-found`). Sin atributo. No exponen datos.

### 3.3 Conteo final

| Categoría | Archivos |
|---|---|
| **Páginas navegables (archivos con `@page`)** | **60** (65 rutas, 5 archivos tienen dos `@page`) |
| Protegidas con `[Authorize(Policy=…)]` | **29** (todas las de negocio) |
| Protegidas con `[Authorize]` sin parámetros | **15** (`Home` + 14 de `Account/Manage` vía `_Imports`) |
| **Total protegidas** | **44** |
| Sin protección | **16** (14 flujos anónimos de Identity + `Error` + `NotFound`) |
| **Páginas de negocio sin protección** | **0** |
| Páginas con `[Authorize(Roles=…)]` | 0 (el modelo usa políticas por permiso, no roles) |

### 3.4 Sobre la afirmación del jurado

> "Solo el módulo financiero aplica permisos y cualquier usuario autenticado entra a ventas, inventario, compras y clientes escribiendo la URL."

**Desmentido para el commit auditado (`4f3a1f4`).** Las 29 páginas de negocio de los seis módulos (Ventas, Inventario, Compras, Clientes, Finanzas, Seguridad) llevan `[Authorize(Policy = …)]` con un permiso concreto; `AuthorizeRouteView` redirige al login/AccessDenied al que no lo tenga. Un usuario autenticado sin el claim `ventas.facturar` que escriba `/ventas` no entra.

**La afirmación sí era cierta en un momento anterior del proyecto.** El documento `Docs/Seguridad-Permisos.md` (líneas 12-18) lo describe literalmente: "Cada pantalla lleva solo `[Authorize]` … cualquier usuario con sesión abierta llega a Finanzas, a Compras y a todo." Git muestra que el gateo por permiso entró en tres commits:

| Commit | Fecha | Autor | Alcance |
|---|---|---|---|
| `cdbf131` | 2026-07-27 | castrogomezdionisjose-wq | Clientes y Compras |
| `36dc703` | 2026-07-27 | Starsamuel25 | Finanzas |
| `a4e5a15` | 2026-07-28 | Jeison1723 | Núcleo (provider/handler), pantallas de usuarios/permisos, Ventas e Inventario |

Si el jurado revisó una versión anterior al 2026-07-28, su observación era correcta para esa versión.

### 3.5 Brechas reales encontradas en el control de acceso actual (no las señaló el jurado, pero existen)

1. **`ventas.anular` está definido pero no se aplica en ningún sitio.** `FacturaDetalle.razor` (`/ventas/facturas/{Id}`) solo exige `ventas.ver` y contiene el botón y la lógica de anulación (`FacturaDetalle.razor:324-351` → `ServicioVentas.AnularAsync`). No hay `AuthorizeView Policy=VentasAnular`. Consecuencia: **un Cajero o un Supervisor (ambos reciben `ventas.ver` por plantilla, `Permisos.cs:114-116`) puede anular facturas.**
2. **`inventario.ajustar` está definido pero no se aplica.** El ajuste/merma se ejecuta desde `ProductoEditor.razor:360` (`AjustarExistenciaAsync`), gateado solo por `inventario.editar`.
3. **`compras.devolver` está definido pero no hay funcionalidad que lo use** (ver §6).
4. La autorización es exclusivamente de capa de presentación. Los servicios de aplicación (`VentaService.AnularAsync`, `ProductoService.AjustarExistenciaAsync`, etc.) no verifican permisos. En Blazor Server con render interactivo esto es aceptable, pero cualquier futuro endpoint de API tendría que gatear por su cuenta.

---

## 4. Roles y permisos

### 4.1 Clases

| Clase | Existe | Ubicación | Registrada en DI |
|---|---|---|---|
| `PermisoPolicyProvider` | **Sí** | `src/MiniERP.Infrastructure/Seguridad/PermisoPolicyProvider.cs` (42 líneas) | **Sí** — `DependencyInjection.Seguridad.cs:23` `AddSingleton<IAuthorizationPolicyProvider, PermisoPolicyProvider>()` |
| `PermisoAuthorizationHandler` | **Sí** | `src/MiniERP.Infrastructure/Seguridad/PermisoAuthorizationHandler.cs` (25 líneas) | **Sí** — `DependencyInjection.Seguridad.cs:24` `AddSingleton<IAuthorizationHandler, PermisoAuthorizationHandler>()` |
| `PermisoRequirement` | Sí | `src/MiniERP.Infrastructure/Seguridad/PermisoRequirement.cs` | (no requiere registro) |
| `GestionUsuariosService` | Sí | `src/MiniERP.Infrastructure/Seguridad/GestionUsuariosService.cs` (256 líneas) | Sí — `DependencyInjection.Seguridad.cs:27` |

`AddSeguridad()` se invoca desde `AddInfrastructure()` (`DependencyInjection.cs:55`), que a su vez se invoca desde `Program.cs:25`. Cadena de registro verificada.

Funcionamiento (`PermisoPolicyProvider.cs:28-41`): para cualquier nombre de política que sea un permiso del catálogo (`Permisos.EsPermiso`) construye al vuelo `RequireAuthenticatedUser() + PermisoRequirement`; cualquier otro nombre lo delega al proveedor por defecto. El handler (`PermisoAuthorizationHandler.cs:17-21`) concede si `User.IsInRole("Administrador")` **o** si tiene el claim `permiso = <valor>`.

### 4.2 Constantes de permiso (`src/MiniERP.Domain/Core/Permisos.cs`)

17 constantes, tipo de claim `"permiso"` (`Permisos.ClaimType`, línea 13):

| Módulo | Constante | Valor | ¿Aplicada en alguna pantalla? |
|---|---|---|---|
| Ventas | `VentasFacturar` | `ventas.facturar` | Sí |
| Ventas | `VentasAnular` | `ventas.anular` | **No** |
| Ventas | `VentasVer` | `ventas.ver` | Sí |
| Ventas | `VentasNcf` | `ventas.ncf` | Sí |
| Inventario | `InventarioVer` | `inventario.ver` | Sí |
| Inventario | `InventarioEditar` | `inventario.editar` | Sí |
| Inventario | `InventarioAjustar` | `inventario.ajustar` | **No** |
| Compras | `ComprasVer` | `compras.ver` | Sí |
| Compras | `ComprasRecibir` | `compras.recibir` | Sí |
| Compras | `ComprasPagar` | `compras.pagar` | Sí |
| Compras | `ComprasDevolver` | `compras.devolver` | **No** (no hay pantalla) |
| Clientes | `ClientesVer` | `clientes.ver` | Sí |
| Clientes | `ClientesEditar` | `clientes.editar` | Sí |
| Clientes | `ClientesCobrar` | `clientes.cobrar` | Sí |
| Finanzas | `FinanzasVer` | `finanzas.ver` | Sí |
| Finanzas | `FinanzasEgreso` | `finanzas.egreso` | Sí (a nivel de `AuthorizeView` en `Egresos.razor:30,131` y `CategoriasEgreso.razor:21,90`) |
| Sistema | `SistemaUsuarios` | `sistema.usuarios` | Sí |

Total: 14 aplicados, 3 definidos sin aplicación.

### 4.3 Roles (`src/MiniERP.Domain/Core/Roles.cs`)

Definidos exactamente los cuatro: `Administrador`, `Cajero`, `Almacen`, `Supervisor` (líneas 10-19). Se siembran al arrancar en `DatabaseInitializer.SembrarRolesAsync` (`DatabaseInitializer.cs:124-142`). El administrador inicial se siembra en `SembrarAdministradorAsync` (líneas 149-197) con el email de `SeedSettings.AdminEmail` (por defecto `admin@minierp.local`).

Los roles funcionan como **plantillas de permisos** (`Permisos.PorDefectoDeRol`, `Permisos.cs:111-118`):

| Rol | Permisos por defecto |
|---|---|
| Administrador | Todos (además el handler le da bypass por rol) |
| Cajero | `ventas.facturar`, `ventas.ver`, `inventario.ver`, `clientes.ver` |
| Almacen | `inventario.ver`, `inventario.editar`, `inventario.ajustar`, `compras.ver`, `compras.recibir`, `compras.devolver` |
| Supervisor | `ventas.ver`, `inventario.ver`, `compras.ver`, `clientes.ver`, `finanzas.ver` |

### 4.4 ¿Existe pantalla de administración?

**Sí, existe y está cableada.** Tres páginas en `src/MiniERP.Web/Components/Pages/Seguridad/`, todas bajo `sistema.usuarios`:

| Página | Ruta | Operaciones verificadas (llamadas al servicio) |
|---|---|---|
| `Usuarios.razor` | `/usuarios` | `ListarAsync` (l. 92), `CambiarEstadoAsync` (l. 99), `ResetearClaveAsync` (l. 115) |
| `UsuarioEditor.razor` | `/usuarios/nuevo`, `/usuarios/{Id}` | `CrearAsync` / `ActualizarAsync` (l. 127-128) |
| `PermisosUsuario.razor` | `/usuarios/{Id}/permisos` | `ObtenerPermisosAsync` (l. 92), `GuardarPermisosAsync` (l. 125) — matriz de interruptores por módulo |

Enlace en el menú: `NavMenu.razor:77-80` (`<AuthorizeView Policy="@Permisos.SistemaUsuarios">`).

**¿Los permisos solo se asignan insertando claims en la BD?** **No.** `GestionUsuariosService.GuardarPermisosAsync` (líneas 189-212) sincroniza los claims con `UserManager.AddClaimAsync/RemoveClaimAsync` y llama a `UpdateSecurityStampAsync` para que la sesión abierta se reevalúe. `CrearAsync` (líneas 56-91) asigna el rol y siembra sus permisos de plantilla como claims. Se persisten en `AspNetUserClaims`, sin tabla propia.

---

## 5. Defecto D-01 — ventas de productos configurados como servicio

### 5.1 Diagnóstico

El defecto **existe y se reproduce por lectura del código**. La causa **no** es una validación de "existencia insuficiente": es una **violación de clave foránea en la base de datos** que se produce porque, para un producto que no maneja inventario, se persiste un `MovimientoInventario` con `ProductoId = 0`.

Cadena exacta:

1. `src/MiniERP.Domain/Inventario/Producto.cs:80-100` — `AplicarMovimiento`:
   ```csharp
   84:        if (!ManejaInventario)
   85:            return;
   ...
   95:        movimiento.ExistenciaAnterior = Existencia;
   96:        movimiento.ExistenciaResultante = resultante;
   97:        movimiento.ProductoId = Id;          // <-- nunca se alcanza para un servicio
   ```
   Para un servicio, el `return` de la línea 85 sale **antes** de asignar `ProductoId` (línea 97). El movimiento queda con `ProductoId = 0`.

2. `src/MiniERP.Domain/Ventas/Factura.cs:96-116` — `Emitir` crea el movimiento (líneas 96-111), llama a `producto.AplicarMovimiento(movimiento)` (línea 115) y **lo añade a la lista igualmente** (línea 116: `movimientos.Add(movimiento)`), sin distinguir si el producto maneja inventario.

3. `src/MiniERP.Application/Ventas/Services/VentaService.cs:158-159` — `EmitirAsync` persiste esa lista: `ventas.AgregarMovimientos(movimientos); await ventas.GuardarAsync(ct);`.

4. La columna es obligatoria y tiene FK: `MovimientoInventario.ProductoId` es `int` no anulable (`MovimientoInventario.cs:16`); configuración `MovimientoInventarioConfiguration.cs:29-32` (`HasOne(...).WithMany(...).HasForeignKey(m => m.ProductoId)`); constraint en la migración `20260716183645_InventarioYClientes.cs:150` (`FK_MovimientosInventario_Productos_ProductoId`, `nullable: false`, `onDelete: Restrict`). No existe un producto con `Id = 0`, así que SQL Server rechaza el INSERT.

5. La excepción que llega es `DbUpdateException` (envuelve `SqlException` 547). El `catch` de `VentaService.cs:134` solo captura `InvalidOperationException`, así que no la maneja. `EnTransaccionAsync` (`VentaRepositorio.cs:195-217`) revierte la transacción (se libera el NCF, correcto), pero la excepción sigue subiendo.

6. `Ventas.razor:375-401` (`Cobrar()`) tiene `try/finally` **sin `catch`**: la excepción llega al circuito de Blazor y el cajero ve el error genérico de la aplicación. La venta no se guarda.

### 5.2 Por qué las 191 pruebas no lo detectan

`ProductoTests.Un_servicio_no_mueve_inventario` (`ProductoTests.cs:86-95`) solo afirma que `Existencia` sigue en 0; no afirma nada sobre `movimiento.ProductoId`. Ninguna prueba de `FacturaTests`/`VentaServiceTests` emite una factura con `ManejaInventario = false`. El fallo ocurre en la capa de persistencia, que tiene 0 % de cobertura.

### 5.3 El mismo patrón está en otros dos puntos (no reportados en D-01)

- `src/MiniERP.Domain/Compras/Compra.cs:97-109` — `Recibir`: una compra que contenga un servicio fallaría igual al recibirse.
- `src/MiniERP.Domain/Ventas/Factura.cs:174-187` — `Anular`: si se corrigiera solo `Emitir`, anular una factura con un servicio fallaría por la misma razón.
- `DevolucionCompra.Confirmar` (`DevolucionCompra.cs:95-108`) tiene el patrón pero no está persistida (§6).

### 5.4 Qué archivo y qué línea tocar (sin aplicar)

Punto mínimo y correcto: **`src/MiniERP.Domain/Ventas/Factura.cs`, líneas 96-116** — no construir ni añadir el `MovimientoInventario` cuando `!producto.ManejaInventario` (congelar `linea.CostoUnitario` en la línea 94 sigue siendo necesario para el margen). Aplicar el mismo criterio en `Factura.cs:174-187` (`Anular`) y en `Compra.cs:97-109` (`Recibir`).

Alternativa que **no** se recomienda: mover `movimiento.ProductoId = Id` en `Producto.cs` antes del `return` de la línea 85. Compilaría y persistiría, pero dejaría un movimiento de kardex con existencia anterior/resultante 0 para un producto que por definición no tiene kardex, y `AjustarExistenciaAsync` ya rechaza explícitamente los servicios (`ProductoService.cs:148-149`), lo que indica que la intención de diseño es que un servicio no genere movimientos.

Añadir una prueba en `FacturaTests` que emita con un producto `ManejaInventario = false` y afirme que la lista devuelta no contiene un movimiento para ese producto.

### 5.5 Actualización posterior a la auditoría (2026-09-10)

Corrección aplicada según §5.4 y commiteada como **`512dc38`** (ver sección final "Cambios aplicados el 2026-09-10"): `Factura.Emitir` y `Factura.Anular` (`Factura.cs`) y `Compra.Recibir` (`Compra.cs`) omiten con `continue` la creación del movimiento cuando `!producto.ManejaInventario`; el costo de la línea sigue congelándose. Se añadieron 5 pruebas (3 en `FacturaTests.cs`, 2 en `CompraTests.cs`) que **fallan contra el código del commit `4f3a1f4` y pasan con la corrección** (verificado con `git stash` sobre `src/`). Suite tras el cambio: **196/196**. Los hechos de §1-§4 y §6-§11 describen el commit `4f3a1f4`.

---

## 6. Devolución a proveedor

| Pieza | ¿Existe? | Evidencia |
|---|---|---|
| Entidad de dominio | **Sí** | `src/MiniERP.Domain/Compras/DevolucionCompra.cs` (115 líneas), `LineaDevolucionCompra.cs`, enum `EstadoDevolucion`. Método `Confirmar` (l. 55-114) genera movimientos `DevolucionProveedor`. |
| Contrato de repositorio | Sí (solo interfaz) | `src/MiniERP.Application/Compras/Contracts/IDevolucionCompraRepositorio.cs` |
| Servicio de aplicación | **Sí** | `src/MiniERP.Application/Compras/Services/DevolucionCompraService.cs` (212 líneas): `NuevaDesdeCompraAsync`, `GuardarAsync`, `ConfirmarAsync`. |
| DTOs | Sí | `src/MiniERP.Application/Compras/Dtos/DevolucionesDtos.cs` |
| Implementación EF del repositorio | **No** | `grep -ri devolucion src/MiniERP.Infrastructure` → única coincidencia es `ProductoRepositorio.cs:131` (`DevolucionCliente`, otra cosa). No hay `DevolucionCompraRepositorio.cs`. |
| `DbSet` en el contexto | **No** | `MiniErpDbContext.cs:20-42` tiene 14 DbSets; ninguno de devolución. |
| Configuración EF | **No** | No hay `DevolucionCompraConfiguration`. |
| Migración / tabla | **No** | Las 8 migraciones aplicadas no crean `DevolucionesCompra` ni `LineasDevolucionCompra`. |
| Registro en DI | **No** | `DependencyInjection.Compras.cs:13-18` registra Proveedor, Compra y Pagos; nada de devolución. |
| Pantalla | **No** | `grep -ri devolucion src/MiniERP.Web` → solo la etiqueta del enum en `Formato.cs:91-92`. No hay `.razor`. |
| Permiso | Definido, no usado | `compras.devolver` (§4.2). |
| Pruebas | Sí, de dominio | `DevolucionCompraTests.cs`: 13 `[Fact]`, todos sobre la entidad en memoria. |
| **¿Se persiste?** | **No.** | Sin repositorio, DbSet ni tabla, `GuardarAsync` no tiene dónde escribir. Un intento de resolver `IDevolucionCompraService` desde DI fallaría por dependencia no registrada. |
| **¿Afecta el balance del proveedor?** | **Solo en código no ejecutable.** | `DevolucionCompraService.ConfirmarAsync:170-175` contiene `proveedor.BalanceActual = Math.Max(0, proveedor.BalanceActual - devolucion.Total)` solo si la compra fue a crédito; pero como el servicio no es alcanzable, en runtime **no afecta nada**. |

El propio servicio lo declara en sus `<remarks>` (`DevolucionCompraService.cs:25-29`): "este caso de uso aun no se registra en DI ni cuenta con repositorio EF, DbSet, migracion o pantallas. No debe exponerse hasta completar esas piezas".

**Veredicto:** la afirmación del documento es exacta. Estado: **Parcial** (dominio + aplicación + pruebas de dominio; sin persistencia, sin DI, sin UI, sin efecto real sobre la deuda).

---

## 7. Multiempresa

Búsqueda en `src/` (excluyendo `bin/`, `obj/`) de `TenantId`, `ComercioId`, `EmpresaId`, `Tenant`, `MultiTenant`, `HasQueryFilter`, `IgnoreQueryFilters`, `SucursalId`, `LocalId`: **0 coincidencias** en `.cs`, `.razor` y `.json`.

Evidencia adicional:
- `EntidadBase` (`src/MiniERP.Domain/Shared/EntidadBase.cs`) tiene `Id`, `FechaCreacion`, `CreadoPor`, `FechaModificacion`, `ModificadoPor`. Ningún discriminador de comercio.
- `MiniErpDbContext.OnModelCreating` (`MiniErpDbContext.cs:44-48`) solo aplica las configuraciones del ensamblado; ningún filtro global.
- Los datos del emisor fiscal viven en **un único** bloque de configuración `Comercio` en `src/MiniERP.Web/appsettings.json` (`Rnc`, `RazonSocial`, `NombreComercial`, `Direccion`, `Telefono`), leído una vez en `DependencyInjection.Ventas.cs:26` hacia `DatosEmisor`. Un RNC por instalación.
- `Producto.cs:9-10` (remarks): "La existencia vive aqui y no en una tabla aparte porque el comercio opera en una sola ubicacion."
- `DatabaseInitializer.cs:24-26`: "hay una sola instancia atendiendo un solo local."

**Veredicto: el sistema es mono-comercio.** No hay ninguna separación de datos por empresa, comercio, sucursal ni inquilino. Estado: **No existe** (multiempresa).

---

## 8. Concurrencia y comprobantes fiscales (NCF)

### 8.1 Código que asigna el NCF

`src/MiniERP.Infrastructure/Persistence/Repositories/SecuenciaNcfRepositorio.cs:37-73`, método `AsignarSiguienteAsync`:

```csharp
57:        var asignados = await contexto.Database
58:            .SqlQuery<long>($@"
59:                UPDATE SecuenciasNcf
60:                SET Actual = Actual + 1
61:                OUTPUT INSERTED.Actual AS Value
62:                WHERE Id = {secuencia.Id}
63:                  AND Activa = 1
64:                  AND Actual < Hasta
65:                  AND FechaVencimiento >= {hoy}")
66:            .ToListAsync(ct);
67:
68:        // Cero filas afectadas: entre la lectura y el UPDATE, otra caja consumio el
69:        // ultimo numero del rango. No hay comprobante que entregar.
70:        return asignados.Count == 0
71:            ? null
72:            : secuencia.Formatear(asignados[0]);
```

**Respuesta: usa una sentencia atómica `UPDATE … OUTPUT INSERTED.Actual` de SQL Server.** El incremento y la lectura del nuevo valor ocurren en una sola sentencia; el `WHERE` rechaza en la misma operación la secuencia agotada o vencida. La lectura previa (líneas 45-52) solo sirve para conocer el `Id` y el `Prefijo`, que no cambian. El formateo a `B02 + 8 dígitos` (o 10 para e-CF) es `SecuenciaNcf.Formatear` (`SecuenciaNcf.cs:75-76`). Los parámetros van interpolados vía `SqlQuery<T>($"…")`, que EF Core convierte en parámetros SQL (no concatenación de texto).

La llamada ocurre dentro de `ventas.EnTransaccionAsync` (`VentaService.cs:85-87`); si el resto de la venta falla, `EnTransaccionAsync` (`VentaRepositorio.cs:195-217`) hace rollback y el número reservado se libera. Efecto colateral documentado en el propio código (`VentaRepositorio.cs:191-193`): la fila de la secuencia queda bloqueada durante la venta, serializando las cajas en ese instante.

### 8.2 Protección contra duplicados a nivel de base de datos

**Sí, existe.**

| Índice | Definición | Migración |
|---|---|---|
| `IX_Facturas_Ncf` | `HasIndex(f => f.Ncf).IsUnique().HasFilter("[Ncf] IS NOT NULL")` — `VentasConfiguration.cs:40-42` | `20260716201411_Facturas.cs:98-103` (`unique: true, filter: "[Ncf] IS NOT NULL"`) |
| `IX_Facturas_Numero` | `HasIndex(f => f.Numero).IsUnique()` — `VentasConfiguration.cs:36` | `20260716201411_Facturas.cs:105-109` |

Ambos están aplicados en la BD local (migración `20260716201411_Facturas` figura en `__EFMigrationsHistory`).

### 8.3 Matices que conviene declarar en la tesis

1. El **número interno** de factura (`FAC-000001`) **sí** se calcula en memoria: `VentaRepositorio.SugerirNumeroAsync` (`VentaRepositorio.cs:151-163`) lee el último y le suma 1 en C#. Bajo dos cajas simultáneas, ambas podrían proponer el mismo `Numero`; el índice único `IX_Facturas_Numero` haría fallar la segunda con `DbUpdateException` (no capturada en `Ventas.razor`), la transacción revertiría y el NCF se liberaría. No hay duplicado, pero sí un fallo visible al cajero. Solo afecta al correlativo interno, no al NCF.
2. No hay índice único ni restricción sobre `SecuenciasNcf` que impida registrar dos secuencias activas del mismo tipo con rangos solapados; `AsignarSiguienteAsync` toma la de menor `Id`. Es una validación de datos maestros, no de concurrencia.

---

## 9. Seguridad adicional

| Control | Estado | Evidencia |
|---|---|---|
| **Política de contraseñas** (longitud mínima, complejidad) | **Parcial** | No hay configuración explícita: `grep -rn "options.Password"` en `src/` → 0 coincidencias. `AddIdentityCore` (`DependencyInjection.cs:34-43`) solo fija `SignIn.RequireConfirmedAccount` y `Stores.SchemaVersion`. Rigen los **valores por defecto de ASP.NET Core Identity**: mínimo 6 caracteres, requiere dígito, minúscula, mayúscula y no alfanumérico. Se aplica (Identity lo valida en `CreateAsync`/`ResetPasswordAsync`), pero **el proyecto no la definió** y no aparece documentada como decisión. Las claves generadas por el sistema tienen 14 caracteres (`GestionUsuariosService.cs:235-255`) y 16 (`DatabaseInitializer.cs:209-233`). |
| **Bloqueo por intentos fallidos (lockout)** | **No existe** | `Login.razor:114`: `SignInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false)`. Con `false`, los intentos fallidos **no incrementan `AccessFailedCount` ni bloquean**. No hay `options.Lockout.*` configurado. Lo único que existe es el **bloqueo administrativo** (desactivar usuario = `LockoutEnd = MaxValue`, `GestionUsuariosService.cs:141-145`), que es otra cosa. En la BD local ambos usuarios tienen `AccessFailedCount = 0`. |
| **Expiración de sesión** | **Parcial** | No hay `ConfigureApplicationCookie`, `ExpireTimeSpan` ni `SlidingExpiration` en el código. Rige el **valor por defecto** de la cookie de Identity: 14 días con expiración deslizante. Sí existe revalidación del circuito Blazor cada 30 minutos: `IdentityRevalidatingAuthenticationStateProvider.cs:18` (`RevalidationInterval => TimeSpan.FromMinutes(30)`), que comprueba el security stamp y expulsa al usuario desactivado o con permisos cambiados. No hay cierre por inactividad definido por el proyecto. |
| **Auditoría de accesos** | **No existe** | No hay tabla, entidad ni servicio de auditoría de inicio/cierre de sesión. Lo único: `Logger.LogInformation("User logged in.")` en `Login.razor:119` (texto del scaffold, sin identificar al usuario, solo a consola/log del proceso). Sí existe **trazabilidad de operaciones de negocio** (`CreadoPor`, `UsuarioId` en movimientos, cobros, pagos, egresos, facturas), que es distinto de auditar accesos. |
| **Respaldo y restauración de BD** | **No existe** | `grep -ri "backup|respaldo|restaur|restore"` en código, docs, README y scripts → 0 coincidencias relevantes (las únicas son `IsBackupEligible` de passkeys en las migraciones, sin relación). No hay scripts `.sql`, `.ps1`, `.bat` ni `.sh` en el repositorio. No hay pantalla ni job. |

Observación adicional (no pedida, pero relevante para seguridad): cuando no se configura `Seed:AdminPassword`, `DatabaseInitializer.cs:179-189` **escribe la contraseña generada del administrador en el log** con `LogWarning`. Es intencional y documentado ("única vez que esta clave es visible"), pero significa que la clave inicial queda en cualquier archivo/consola donde se capture el log.

---

## 10. Conciliación financiera

### 10.1 Búsqueda del arnés / generador del escenario

| Búsqueda | Resultado |
|---|---|
| `"Esperanza"` en árbol de trabajo (todo el repo) | 0 coincidencias |
| `"Esperanza"` / `"Colmado"` en historial git (`git log --all -S`) | Solo `"Colmado La Esquina"` como nombre de cliente en pruebas (`ClienteTests.cs:10`, y `MinimarketEndToEndTests.cs:53` en la rama no integrada). No es el escenario. |
| Montos del documento (`320278.82`, `712329.10`, `643442.62`, `36960.25`, `1032607.92`, `352205.05`, con y sin separadores) | **0 coincidencias** en árbol de trabajo y en historial. |
| Archivos `.sql`, `.ps1`, `.bat`, `.sh` en el repositorio | Ninguno. |
| Rama `origin/sim/reloj-virtual` (commit `b4c6f49`, 2026-09-07, Starsamuel25, no integrada) | Añade `src/MiniERP.Domain/Shared/RelojSimulado.cs` (61 líneas) y sustituye `DateTime.UtcNow` en 35 archivos por un reloj leído de un archivo (`MINIERP_RELOJ`). **No contiene generador de datos.** Su mensaje dice: "es lo que permitio correr los 21 dias de operacion del caso piloto contra la base, del 13 de agosto al 2 de septiembre, comprimiendo tres semanas en una sesion." Es decir, el escenario se **operó** contra una base de datos con reloj comprimido, mediante un orquestador externo que **no está en el repositorio**. |

**Conclusión: el arnés/generador del escenario "Colmado La Esperanza" no existe en el repositorio ni en ninguna de sus ramas** (se recorrieron las 17 locales y las 14 remotas; 19 nombres distintos). Los datos del escenario son datos de una base SQL Server, no código.

### 10.2 Los cinco (seis) valores pedidos

| Valor | Resultado | Motivo |
|---|---|---|
| a) Efectivo inicial de caja | **No verificable** | Además de no estar los datos, **el modelo no tiene el concepto**: `grep -ri "EfectivoInicial|CajaInicial|AperturaCaja|CierreCaja|SaldoInicial"` en `src/` → 0. `FlujoCaja` (`FlujoCaja.cs:7-13`) es `VentasContado + Cobros − Egresos − Pagos`, sin saldo de apertura. |
| b) Capital aportado por el propietario | **No verificable** | Mismo caso: `grep -ri "Aporte|Capital"` en `src/` → 0. No existe entidad ni asiento de aporte. |
| c) Inventario inicial sembrado, a costo | **No verificable para el escenario de la tesis** (ver §10.4 para cómo obtenerlo) | Los datos no están en el repo ni en esta máquina. El **mecanismo** sí está identificado (§10.3). |
| d) Total de compras del período, a costo | **No verificable** | Ídem. |
| e) De esas compras, cuánto fue a crédito | **No verificable** | Ídem. |
| f) Mermas y ajustes, a costo | **No verificable** | Ídem. |

### 10.3 Hipótesis del autor: "el inventario inicial se sembró sin pasar por una compra"

**Confirmada en el código, y hay un segundo mecanismo que también explica parte de la brecha.**

**Mecanismo 1 — existencia inicial de apertura (sin compra).** `src/MiniERP.Application/Inventario/Services/ProductoService.cs:82-100` (`CrearAsync`):

```csharp
84:        if (form.ExistenciaInicial > 0 && producto.ManejaInventario)
85:        {
86:            var apertura = new MovimientoInventario
87:            {
88:                Tipo = TipoMovimiento.Entrada,
89:                Cantidad = form.ExistenciaInicial,
90:                CostoUnitario = form.Costo,
91:                Motivo = "Existencia inicial",
92:                ReferenciaTipo = "APERTURA",
```

Al crear un producto con existencia inicial, la mercancía entra al kardex valorada a `form.Costo` con `ReferenciaTipo = "APERTURA"`. **No crea `Compra`, no toca `Proveedor.BalanceActual`, no crea `Pago`.** Esa mercancía después se vende (entra en "costo de lo vendido") o queda (entra en "inventario final"), pero nunca aparece en "pagos a proveedores" ni en "cuentas por pagar".

**Mecanismo 2 — compras al contado (no entran en pagos ni en CxP).** `src/MiniERP.Application/Compras/Services/CompraService.cs:133-141` (`RecibirAsync`):

```csharp
133:        if (compra.Condicion == CondicionPago.Credito)
134:        {
135:            var proveedor = await proveedores.ObtenerPorIdAsync(compra.ProveedorId, ct);
...
140:            proveedor.BalanceActual += compra.Total;
141:        }
```

Una compra **al contado** recibida sube el inventario pero **no** incrementa el balance del proveedor **ni crea un registro en `Pagos`**. El reporte de flujo de caja suma solo la tabla `Pagos` (`FinanzasRepositorios.cs:183-191`, `SumarPagosAsync`) y las CxP son `Proveedor.BalanceActual`. Por tanto, **toda compra de contado queda fuera de ambos lados**: ni pago ni deuda. Es un dinero que salió de caja y que el flujo de caja no ve.

**Mecanismo 3 — mezcla de bases (con y sin ITBIS).** Inventario y costo de lo vendido se valoran a `CostoUnitario` **sin ITBIS** (`LineaCompra.CostoUnitario`, `MovimientoInventario.CostoUnitario`). Los pagos y las CxP se registran contra `Compra.Total`, que **incluye ITBIS** (`Compra.Recalcular`, `Compra.cs:49-54`). La comparación "mercancía a costo vs. pagos + CxP" no está en la misma base.

**Fórmula de la brecha** (lo que el autor observa como RD$ 352 205.05):

```
Brecha = (CostoVendido + InventarioFinal) − (Pagos + CxP)
       ≈ APERTURA_a_costo
       + Compras_contado_a_costo
       + AjustesPositivos − Mermas − AjustesNegativos
       + revalorización por costo promedio (pequeña)
       − ITBIS de las compras a crédito
```

Con los datos reales del escenario (§10.4) cada término se obtiene con una consulta y la identidad debe cerrar.

### 10.4 Verificación empírica con la BD de esta máquina (NO es el escenario de la tesis)

En esta máquina hay una BD `MiniERP` (SQL Server local, migración `20260728190250` aplicada) con un dataset distinto: 1 240 facturas del 2026-06-01 al 2026-07-30, 28 productos, 10 compras, 9 pagos. Los datos fueron insertados por un script externo (1 240 facturas creadas en 12 s el 2026-07-30 19:39, con `Fecha` retroactiva, algo que la aplicación no permite porque `Factura.Fecha = DateTime.UtcNow`). **Sus cifras no coinciden con las del documento**, pero sirven para demostrar que los mecanismos producen exactamente esta brecha. Consultas de solo lectura:

| Concepto | Valor en BD local (RD$) |
|---|---|
| Costo de lo vendido (facturas emitidas, `SUM(CostoTotal)`) | 469 331.87 |
| Inventario final a costo (`SUM(Existencia*Costo)`) | 596 877.97 |
| **Mercancía que entró (suma)** | **1 066 209.84** |
| Pagos a proveedores (`SUM(Pagos.Monto)`) | 393 255.20 |
| Cuentas por pagar (`SUM(Proveedores.BalanceActual)`) | 104 943.97 |
| **Pagos + CxP** | **498 199.17** |
| **Brecha** | **568 010.67** |
| APERTURA a costo (`ReferenciaTipo='APERTURA'`) — 28 movimientos, 9 505 unidades | **427 345.00** |
| Compras al contado recibidas, a costo (`Subtotal`) / con ITBIS (`Total`) | 193 844.00 / 206 159.60 |
| Compras a crédito recibidas, con ITBIS (`Total`) | 498 199.17 |
| Mermas a costo (Tipo 5) | 900.88 |
| Ajuste positivo / negativo a costo (Tipo 3 / 4) | 89.70 / 86.32 |
| Devoluciones de cliente por anulación (Tipo 6) | 529.00 |

Comprobaciones:
- `Pagos + CxP = 498 199.17 = Compras a crédito (Total con ITBIS)`, al centavo. **Las compras de contado (206 159.60) no aparecen en ningún lado del flujo.**
- La brecha de **568 010.67** se descompone exactamente:

| Término | RD$ |
|---|---|
| APERTURA a costo (mecanismo 1) | + 427 345.00 |
| Compras al contado, a costo (mecanismo 2) | + 193 844.00 |
| ITBIS de las compras a crédito (mecanismo 3: pagos/CxP lo incluyen, el costo no) | − 52 289.57 |
| Ajuste positivo | + 89.70 |
| Devoluciones de cliente por anulación | + 529.00 |
| Ajuste negativo | − 86.32 |
| Mermas | − 900.88 |
| Costo de facturas anuladas (está en el kardex, no en "costo vendido") | − 528.86 |
| Revalorización por costo promedio ponderado (inventario a costo actual vs. movimientos a costo histórico) | + 8.60 |
| **Suma** | **568 010.67** |

Esto **confirma la hipótesis del autor** para el mecanismo y muestra que, en un dataset real generado con este sistema, el inventario de apertura sin compra (75 % de la brecha en este caso) y las compras de contado (34 %) explican la totalidad, una vez descontado el ITBIS que los pagos incluyen y el inventario no.

### 10.5 Consultas para obtener los valores exactos del escenario de la tesis

Ejecutar contra la base donde se corrió la simulación del 13-ago al 2-sep (la máquina de quien ejecutó `sim/reloj-virtual`). Son `SELECT`, no modifican nada.

```sql
-- c) Inventario inicial sembrado, a costo
SELECT SUM(Cantidad * CostoUnitario) AS apertura_costo, COUNT(*) AS productos
FROM MovimientosInventario WHERE ReferenciaTipo = 'APERTURA';

-- d) Compras del período a costo (sin ITBIS) y e) cuánto fue a crédito
SELECT Condicion,                       -- 1 = Contado, 2 = Crédito
       SUM(Subtotal) AS costo_sin_itbis, SUM(Itbis) AS itbis, SUM(Total) AS total
FROM Compras WHERE Estado = 2           -- 2 = Recibida
GROUP BY Condicion;

-- f) Mermas y ajustes a costo
SELECT Tipo,                            -- 3 AjustePositivo, 4 AjusteNegativo, 5 Merma
       SUM(Cantidad * CostoUnitario) AS valor_costo, SUM(Cantidad) AS unidades
FROM MovimientosInventario WHERE Tipo IN (3, 4, 5) GROUP BY Tipo;

-- Contraste con lo que reporta el documento
SELECT (SELECT SUM(CostoTotal) FROM Facturas WHERE Estado = 1)        AS costo_vendido,
       (SELECT SUM(Existencia * Costo) FROM Productos WHERE ManejaInventario = 1) AS inventario_final,
       (SELECT SUM(Monto) FROM Pagos)                                  AS pagos_proveedores,
       (SELECT SUM(BalanceActual) FROM Proveedores)                    AS cuentas_por_pagar;
```

a) y b) no pueden obtenerse de la base porque el sistema no los modela; tendrían que estar en las notas de la simulación.

---

## 11. Repositorio

| Comando | Resultado |
|---|---|
| `git remote -v` | `origin  https://github.com/jeisonsantoslogictool/MiniERP.git (fetch)` / `(push)` |
| `git branch --show-current` | `main` |
| `git rev-parse HEAD` | `4f3a1f416d6f0dd5f9f29d52ea1ac67a5bd3c236` |
| `git rev-parse origin/main` | `4f3a1f416d6f0dd5f9f29d52ea1ac67a5bd3c236` (local y remoto sincronizados) |
| `git log -1` | commit `4f3a1f416d6f0dd5f9f29d52ea1ac67a5bd3c236` — Author: Jeison1723 <jeison.santos@logictool.com.do> — Date: Tue Sep 1 15:24:27 2026 −0400 — "AJuste de formato impreso" |
| `git status` | Árbol limpio (antes de crear este archivo) |
| Ramas remotas con trabajo **no integrado** en `main` | `origin/samuel/pruebas-finanzas` (9 commits, 2026-09-07/08) y `origin/sim/reloj-virtual` (1 commit, 2026-09-07) |

Para citar en la tesis: **jeisonsantoslogictool/MiniERP, rama `main`, commit `4f3a1f4` (2026-09-01).** Todo lo afirmado en esta auditoría corresponde a ese commit; lo que esté en las ramas no integradas (más pruebas, reloj simulado) no forma parte de él.

---

## Tabla resumen

Estado **al commit auditado `4f3a1f4`**. Tres filas cambiaron después ese mismo día (D-01, `ventas.anular`, bloqueo por intentos); el estado posterior está en la sección final.

| Ítem | Estado | Evidencia |
|---|---|---|
| SDK declarado (`10.0.204`) igual al instalado | Parcial | Instalado `10.0.301`; resuelve por `rollForward: latestFeature`. `global.json`, `dotnet --list-sdks`. |
| Compilación limpia | Implementado | `dotnet build --no-incremental`: 0 errores, 0 advertencias, sin `NoWarn`. |
| 180 pruebas unitarias | Parcial (la cifra es incorrecta) | Hay **191** en `main` (154 Fact + 37 InlineData), 191/191 verdes, 0 omitidas. TRX 2026-09-10 19:11:25→19:11:36 −04:00. |
| Cobertura de código | Parcial | 5.96 % líneas total (893/14 974); ≈21.8 % sin migraciones; Domain 79.8 %, Application 21.3 %, Infrastructure 0 %, Web no medido. |
| Páginas de negocio protegidas con política | Implementado | 29/29 con `[Authorize(Policy=…)]`; 0 sin protección; 16 públicas son flujos de Identity + Error/NotFound. |
| "Solo Finanzas aplica permisos" (jurado) | No aplica al commit auditado | Cierto antes del 2026-07-28 (`Docs/Seguridad-Permisos.md:12-18`); resuelto en `cdbf131`, `36dc703`, `a4e5a15`. |
| `PermisoPolicyProvider` / `PermisoAuthorizationHandler` | Implementado | `src/MiniERP.Infrastructure/Seguridad/`; registrados en `DependencyInjection.Seguridad.cs:23-24`. |
| Catálogo de permisos (17 constantes) | Parcial | 14 aplicados; `ventas.anular`, `inventario.ajustar`, `compras.devolver` definidos sin aplicar. |
| Permiso `ventas.anular` efectivo | No existe | Anular factura solo exige `ventas.ver` (`FacturaDetalle.razor:2,324-351`); Cajero y Supervisor pueden anular. |
| Roles Administrador/Cajero/Almacén/Supervisor | Implementado | `Roles.cs:10-19`; sembrados en `DatabaseInitializer.cs:124-142`; plantillas en `Permisos.cs:111-118`. |
| Pantalla de administración de usuarios y permisos | Implementado | `/usuarios`, `/usuarios/{Id}`, `/usuarios/{Id}/permisos`; `GestionUsuariosService` (crear, editar, activar, resetear, matriz de permisos). |
| D-01: venta de producto servicio | Confirmado (defecto real) | FK violada: `Producto.cs:84-85` retorna antes de `:97`; `Factura.cs:116` añade el movimiento; `VentaService.cs:158` lo persiste; `MovimientoInventarioConfiguration.cs:29-32`. Sin `catch` en `Ventas.razor:375-401`. Mismo patrón en `Compra.cs:97-109` y `Factura.cs:174-187`. |
| Devolución a proveedor | Parcial | Entidad + servicio + 13 pruebas de dominio. Sin repositorio EF, DbSet, migración, DI ni pantalla. Balance del proveedor no se afecta en runtime. |
| Multiempresa | No existe | 0 coincidencias de `TenantId`/`ComercioId`/`EmpresaId`/`HasQueryFilter`; un solo bloque `Comercio` en `appsettings.json`. Mono-comercio. |
| Asignación atómica de NCF | Implementado | `UPDATE … OUTPUT INSERTED.Actual` en `SecuenciaNcfRepositorio.cs:57-66`, dentro de transacción con rollback. |
| Índice único sobre NCF | Implementado | `IX_Facturas_Ncf` único filtrado (`VentasConfiguration.cs:40-42`; migración `20260716201411_Facturas.cs:98-103`). |
| Número interno de factura atómico | Parcial | `SugerirNumeroAsync` (`VentaRepositorio.cs:151-163`) calcula en memoria; protegido solo por `IX_Facturas_Numero`. |
| Política de contraseñas | Parcial | Solo defaults de Identity (6 chars + dígito + mayúscula + minúscula + símbolo); no configurada por el proyecto. |
| Bloqueo por intentos fallidos | No existe | `Login.razor:114` `lockoutOnFailure: false`; sin `options.Lockout`. Solo bloqueo administrativo. |
| Expiración de sesión | Parcial | Default cookie Identity (14 días deslizante); revalidación de circuito cada 30 min (`IdentityRevalidatingAuthenticationStateProvider.cs:18`). Sin política propia. |
| Auditoría de accesos | No existe | Solo `Logger.LogInformation("User logged in.")` del scaffold (`Login.razor:119`). Hay trazabilidad de operaciones, no de accesos. |
| Respaldo y restauración de BD | No existe | 0 coincidencias en código, docs y scripts. |
| Arnés / generador del escenario "Colmado La Esperanza" | No existe en el repositorio | 0 coincidencias en todas las ramas (19 nombres); `sim/reloj-virtual` solo aporta el reloj. Los datos están en una BD externa. |
| Valores a), b) del escenario (efectivo inicial, capital) | No verificable | El modelo no los contempla (0 coincidencias de `Aporte`/`Capital`/`CajaInicial`). |
| Valores c)–f) del escenario | No verificable | Datos no disponibles en repo ni en esta máquina; consultas SQL provistas en §10.5. |
| Hipótesis "inventario sembrado sin compra" | Confirmado (mecanismo) | `ProductoService.cs:84-100` (`APERTURA`); además compras de contado fuera de pagos y CxP (`CompraService.cs:133-141`, `FinanzasRepositorios.cs:183-191`). Verificado empíricamente en BD local: APERTURA = RD$ 427 345.00; `Pagos + CxP` = exactamente compras a crédito. |
| Repositorio / commit citable | Implementado | `https://github.com/jeisonsantoslogictool/MiniERP.git`, `main`, `4f3a1f416d6f0dd5f9f29d52ea1ac67a5bd3c236`. |

---

## Cambios aplicados el 2026-09-10

Tres commits sobre `main`, en este orden, cada uno con la suite completa en verde antes de commitear. Identidad de commit: la configurada en el repositorio (`Jeison1723 <jeison.santos@logictool.com.do>`). Ninguno ha sido subido a `origin` (`main` está 3 commits por delante de `origin/main`).

### C-1 · D-01: venta y recepción de servicios — commit `512dc381046bf5c98539bb5d0e1731f08d710fd2` (`512dc38`, 19:48:55 −04:00)

| Archivo | Cambio |
|---|---|
| `src/MiniERP.Domain/Ventas/Factura.cs` | `Emitir` (+6 líneas) y `Anular` (+4): `continue` cuando `!producto.ManejaInventario`, después de congelar `linea.CostoUnitario`. No se construye ni se devuelve movimiento para el servicio. |
| `src/MiniERP.Domain/Compras/Compra.cs` | `Recibir` (+5): mismo criterio, después de actualizar `producto.Costo`. |
| `tests/MiniERP.Tests/Ventas/FacturaTests.cs` | +3 pruebas: `Un_servicio_se_factura_sin_generar_movimiento_de_inventario`, `Ningun_movimiento_sale_sin_producto_al_mezclar_servicio_y_mercancia`, `Anular_una_venta_de_servicio_no_repone_nada`. |
| `tests/MiniERP.Tests/Compras/CompraTests.cs` | +2 pruebas: `Recibir_un_servicio_no_genera_movimiento_de_inventario`, `Al_recibir_mercancia_y_servicio_juntos_solo_la_mercancia_mueve_kardex`. |

Verificación de que las pruebas atrapan el defecto: con los dos archivos de `src/` revertidos temporalmente al estado de `4f3a1f4` (`git stash`), las 5 fallan (`Assert.Empty() Failure: Collection was not empty` ×3, `Assert.Single() Failure: The collection contained 2 items` ×2); con la corrección, pasan. `Producto.AplicarMovimiento` no se tocó.

### C-2 · Permiso `ventas.anular` — commit `e1f9b6241257b5b7ec80f0f68711204cf967547b` (`e1f9b62`, 19:51:38 −04:00)

Archivo único: `src/MiniERP.Web/Components/Pages/Ventas/FacturaDetalle.razor`.

| Barrera | Dónde |
|---|---|
| Botón "Anular" dentro de `<AuthorizeView Policy="@Permisos.VentasAnular">` | cabecera de la página |
| Tarjeta de confirmación (motivo + "Confirmar anulación") dentro del mismo `AuthorizeView` | cuerpo de la página |
| Verificación del lado del servidor: `MostrarAnular` y `ConfirmarAnular` llaman a `IAuthorizationService.AuthorizeAsync(user, Permisos.VentasAnular)` antes de invocar `IVentaService.AnularAsync`; si falla, mensaje "No tienes permiso para anular facturas." y no se llama al servicio | `@code`, método `PuedeAnularAsync()` |

Sobre "validar en el servidor": en Blazor Server con render interactivo, los manejadores de eventos **ya corren en el servidor** dentro del circuito; no existe una llamada HTTP separada que interceptar. La verificación en el handler es, por tanto, la validación de servidor que aplica. No se añadió autorización dentro de `VentaService` (capa Application) porque esa capa no recibe el principal, solo un `usuarioId`; hacerlo requeriría una abstracción nueva y quedó fuera del alcance. El acceso a la página sigue exigiendo `ventas.ver`.

**Roles con capacidad de anular tras el cambio** (según plantillas de `Permisos.PorDefectoDeRol`, sin modificar):

| Rol | ¿Puede anular? | Por qué |
|---|---|---|
| Administrador | **Sí** | Bypass por rol en `PermisoAuthorizationHandler`. |
| Cajero | **No** | Plantilla: `ventas.facturar`, `ventas.ver`, `inventario.ver`, `clientes.ver`. |
| Almacen | **No** | Sin permisos de ventas. |
| Supervisor | **No** | Plantilla: `ventas.ver`, `inventario.ver`, `compras.ver`, `clientes.ver`, `finanzas.ver`. |
| Cualquier usuario al que el administrador marque `ventas.anular` en `/usuarios/{id}/permisos` | Sí | El permiso es por usuario, no por rol. |

Antes del cambio: Administrador, Cajero y Supervisor podían anular. Los usuarios ya creados no requieren migración de claims (ninguna plantilla incluía `ventas.anular`, así que nadie lo tenía concedido); simplemente dejan de poder hacerlo.

### C-3 · Bloqueo por intentos fallidos — commit `7ff51f2c1ae5cbfde20af6a8acc4545e1ea90489` (`7ff51f2`, 19:53:54 −04:00)

| Archivo | Cambio |
|---|---|
| `src/MiniERP.Web/Components/Account/Pages/Login.razor` | `PasswordSignInAsync(..., lockoutOnFailure: true)` (antes `false`). El aviso de bloqueo en el log ahora incluye el correo. |
| `src/MiniERP.Infrastructure/DependencyInjection.cs` | En `AddIdentityCore`: `Lockout.AllowedForNewUsers = true`, `Lockout.MaxFailedAccessAttempts = 5`, `Lockout.DefaultLockoutTimeSpan = 15 min`. |

**Valores elegidos y por qué:**

- **5 intentos.** Es el valor por defecto de Identity y el umbral habitual: tolera al cajero que se equivoca de tecla dos o tres veces sin abrir la puerta a adivinar. Al bloquear, Identity reinicia `AccessFailedCount` a 0, así que tras cada desbloqueo vuelven a ser cinco.
- **15 minutos.** Tres veces el default (5 min). Con 5 intentos cada 15 min, un atacante obtiene como máximo 480 intentos por día por cuenta; frente a la política vigente (mínimo 6 caracteres con mayúscula, minúscula, dígito y símbolo) es inútil. No se subió más porque en un minimarket el bloqueado es casi siempre el propio empleado con la fila esperando: 15 minutos es un castigo tolerable, y si no puede esperar, el administrador lo desbloquea desde `/usuarios` con "Activar" (`GestionUsuariosService.CambiarEstadoAsync` ya hace `SetLockoutEndDateAsync(null)`), flujo que `Lockout.razor` ya anuncia ("pídele al administrador que la desbloquee").
- **`AllowedForNewUsers = true`.** Es el default, pero se deja explícito. Se comprobó en la BD local que los dos usuarios existentes ya tienen `LockoutEnabled = 1`, así que la política les aplica sin migrar datos.

Efectos secundarios a conocer: (1) un usuario bloqueado temporalmente aparece como "Inactivo" en `/usuarios` durante esos 15 minutos, porque la columna usa `IsLockedOutAsync`; el botón "Activar" lo desbloquea. (2) Cualquiera que conozca el correo del administrador puede bloquearlo 15 minutos con cinco intentos fallidos; es el compromiso estándar de todo lockout y la alternativa (excluir al admin) es peor. (3) Como `EsUltimoAdminActivoAsync` cuenta al bloqueado como inactivo, mientras el único administrador esté bloqueado no se puede desactivar a otro administrador; se resuelve solo a los 15 minutos.

### Estado de las pruebas después de los tres commits

Comando: `dotnet test MiniERP.slnx --collect:"XPlat Code Coverage" --logger trx` sobre `7ff51f2`.

| Métrica | Valor |
|---|---|
| Inicio (TRX `start`) | 2026-09-10 19:54:55.23 −04:00 |
| Fin (TRX `finish`) | 2026-09-10 19:55:10.18 −04:00 |
| **Total** | **196** |
| Aprobadas | **196** |
| Fallidas | **0** |
| Omitidas | 0 |
| Duración de ejecución (runner) | ≈ 1 s |
| **Cobertura de líneas (total)** | **5.99 %** (899 / 14 994) |
| Cobertura de líneas sin migraciones generadas | ≈ 21.8 % |
| `MiniERP.Domain` | 79.96 % |
| `MiniERP.Application` | 21.33 % |
| `MiniERP.Infrastructure` | 0 % |
| `MiniERP.Web` | no medido |
| Cobertura de ramas | 23.32 % (209 / 896) |
| Compilación | 0 errores, 0 advertencias |

Comparado con el commit auditado: +5 pruebas (191 → 196), +6 líneas cubiertas (893 → 899), +20 líneas válidas (las nuevas guardas). Los cambios C-2 y C-3 viven en `MiniERP.Web` e `Infrastructure`, que no tienen pruebas automatizadas: se verificaron por compilación y lectura, no por prueba.

### Referencia rápida

```
git rev-parse HEAD
7ff51f2c1ae5cbfde20af6a8acc4545e1ea90489

git log --oneline -5
7ff51f2 Seguridad: cinco claves equivocadas bloquean la cuenta quince minutos
e1f9b62 Ventas: anular una factura exige ventas.anular, no solo ventas.ver
512dc38 Ventas y compras: un servicio ya no tumba la venta ni la recepcion (D-01)
4f3a1f4 AJuste de formato impreso
a0ac282 Ajuste de diseño
```

Los ítems de la tabla resumen que cambian de estado con estos commits: **D-01** → corregido; **Permiso `ventas.anular` efectivo** → Implementado; **Bloqueo por intentos fallidos** → Implementado (5 intentos / 15 min). Todo lo demás sigue como se auditó.
