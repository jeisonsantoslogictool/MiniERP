# Andamio de permisos — provisional, se borra

**Esto no es trabajo de Samuel ni pretende serlo.** Es el mínimo del núcleo de permisos
—que le toca a **Jeison** en `jeison/seguridad`— escrito temporalmente para poder construir
y **probar** el gateo de Finanzas antes de que ese núcleo exista.

Ver el plan completo en [Seguridad-Permisos.md](Seguridad-Permisos.md).

---

## Por qué hizo falta

El addendum reparte el trabajo en dos fases: Jeison construye el núcleo (Fase A) y después
Dionis y Samuel gatean sus módulos (Fase B). La tarea de Samuel es escribir esto:

```razor
@attribute [Authorize(Policy = Permisos.FinanzasVer)]
```

Ahí hay dos dependencias del núcleo, y **ninguna de las dos existía**:

1. **La constante `Permisos.FinanzasVer`.** Sin el archivo que la define, el proyecto no
   compila: el compilador no sabe qué es `Permisos`.
2. **El proveedor de políticas.** Aunque se escribiera el texto `"finanzas.ver"` a mano para
   esquivar lo anterior, ASP.NET Core buscaría una política registrada con ese nombre, no la
   encontraría y **tumbaría la aplicación** al abrir la pantalla.

Esperar era la opción limpia, pero deja la rama parada. La alternativa —escribir el gateo
"a ciegas", sin compilar ni probar— es peor: nadie puede defender en la sustentación un
control de acceso que nunca vio funcionar.

---

## Qué trae, y de quién es cada cosa

| Archivo | Qué hace | Dueño real |
|---|---|---|
| `src/MiniERP.Domain/Core/Permisos.cs` | Catálogo de los 17 permisos, agrupados por módulo | **Jeison** |
| `src/MiniERP.Web/Seguridad/PermisoRequirement.cs` | La exigencia: "trae este permiso" | **Jeison** |
| `src/MiniERP.Web/Seguridad/PermisoAuthorizationHandler.cs` | Decide: pasa si es Administrador o si trae el claim | **Jeison** |
| `src/MiniERP.Web/Seguridad/PermisoPolicyProvider.cs` | Convierte cada permiso en política, al vuelo | **Jeison** |
| `src/MiniERP.Web/Seguridad/AndamioDePermisos.cs` | El registro en el contenedor | **Jeison** |
| `src/MiniERP.Web/Program.cs` (1 línea) | Llama al registro | **Jeison** |

**El único archivo compartido que se toca es `Program.cs`, y es una sola línea.** No se tocó
`NavMenu.razor` ni `Routes.razor`.

**Tampoco hizo falta una pantalla de rebote.** Se escribió una y se descartó al probar: la
plataforma ya trae `/Account/AccessDenied`, y la autorización de endpoint de ASP.NET Core
redirige sola hacia allá antes de que el router de Blazor llegue a intervenir. Escribir otra
habría sido código muerto.

**Lo que el andamio NO trae**, y sigue siendo tarea completa de Jeison:

- Las dos pantallas: **Usuarios** y **Permisos del usuario**.
- El servicio de gestión de usuarios sobre `UserManager`.
- `ApplicationUser.Nombre` y su migración.
- La **siembra de plantillas de rol** en `DatabaseInitializer`.
- El **menú gateado** (`NavMenu.razor`) — no se tocó, es archivo compartido.
- Los guardrails (no borrar al último Administrador, etc.).

Es decir: el andamio da el **motor**, no el **panel de control**. Mientras esté, los permisos
se conceden insertando el claim a mano en `AspNetUserClaims`; con las pantallas de Jeison se
harán con interruptores.

---

## Cómo se borra

El andamio vive en **un commit propio y separado**, anterior al de la entrega de Samuel:

```
commit 2   Finanzas: gatea las pantallas y las acciones por permiso   <- se queda
commit 1   ANDAMIO PROVISIONAL: nucleo minimo de permisos             <- se descarta
```

Cuando el núcleo de Jeison entre a `main`:

```bash
git fetch origin && git rebase --onto origin/main <commit-1> samuel/permisos
```

Eso deja solo el commit 2 encima del núcleo real. El gateo de Finanzas **no cambia ni una
línea**: sigue pidiendo `Permisos.FinanzasVer` y `Permisos.FinanzasEgreso`, que son los
mismos nombres que define el plan.

Si Jeison prefiere aprovechar algo de aquí, que lo copie a su rama y lo defienda él: **es
suyo**. Lo que no debe pasar es que este andamio llegue a `main` como si fuera la solución
final.

---

## El contrato que hay que respetar

Para que el commit 2 siga funcionando al descartar el andamio, el núcleo de Jeison tiene que
mantener tres cosas, que salen del propio plan:

1. Las constantes se llaman `Permisos.FinanzasVer` y `Permisos.FinanzasEgreso`, con los
   textos `"finanzas.ver"` y `"finanzas.egreso"`.
2. `[Authorize(Policy = ...)]` con esos nombres bloquea a quien no los tiene.
3. El **Administrador pasa siempre**, sin necesidad de tener el claim.

Si Jeison cambia alguno de esos tres puntos, hay que avisar: cambia el gateo de los tres
módulos, no solo el de Finanzas.
