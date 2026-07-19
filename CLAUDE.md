# CLAUDE.md

Contexto del proyecto para Claude Code. Se carga automáticamente al abrir el repositorio.

---

## Qué es esto

**Trabajo Final de Grado del Grupo 6** — Universidad Dominicana O&M, Moca. Ingeniería en
Sistemas y Computación, 2026.

Mini ERP modular en ASP.NET Core y C# para microcomercios minoristas de la República
Dominicana. Cinco módulos: inventario, ventas (POS), compras, clientes y finanzas.
Se valida con un caso piloto en un **minimarket real, propiedad de Jeison**, que vende
**a crédito y al contado**, y **por peso y por unidad**. Esas cuatro características
explican buena parte del modelo de datos.

**Este NO es un repositorio de Logictool.** No aplica el CLAUDE.md de Onix.Apps. Es un
sistema propio, mucho más pequeño, y debe ser defendible como trabajo del grupo: se
aprovecha el conocimiento de otros sistemas, no se copia su código.

| | |
|---|---|
| **Sustentantes** | Jeison Luis Santos · Samuel Sánchez Rosario · Dionis José Castro Gómez · Rangelis Toribio |
| **Asesor** | Lic. Elvin Germán |
| **Documentos** | [Plan maestro](Docs/Plan-Maestro.md) · [Asignaciones por rama](Docs/Asignaciones.md) |

---

## Empieza aquí — ¿en qué rama estás?

**Lo primero al abrir el repo:** detecta la rama actual (`git branch --show-current`) y orienta
al desarrollador con lo suyo antes de nada. Cada quien trabaja en **su** rama, nunca en `main`.

| Si estás en… | Eres | Eres dueño de | Tus tareas |
|---|---|---|---|
| `jeison/pos` | **Jeison** | `Inventario/` · `Ventas/` | [Asignaciones → Jeison](Docs/Asignaciones.md) |
| `samuel/finanzas` | **Samuel** | `Finanzas/` | [Asignaciones → Samuel](Docs/Asignaciones.md) |
| `dionis/cobros-pagos` | **Dionis** | `Clientes/` · `Compras/` | [Asignaciones → Dionis](Docs/Asignaciones.md) |
| `main` | — | No se trabaja aquí | Cámbiate a tu rama: `git checkout <tu-rama>` |

### La frontera: cada quien es dueño de carpetas completas

**No edites el módulo de otro.** Son tres desarrolladores trabajando en paralelo, cada uno
con su propia sesión de Claude. Si dos tocan el mismo archivo, el merge lo paga el que
llegue segundo, y peor: nadie puede defender en la sustentación un módulo que otro escribió.

La frontera se trazó por **módulo del anteproyecto**, no por concepto:

- Jeison: inventario y ventas — la mercancía y su salida.
- Dionis: clientes y compras — los terceros y su crédito, de ambos lados.
- Samuel: finanzas — la capa analítica que lee todo lo anterior sin modificarlo.

**¿Necesitas algo del módulo de otro?** Pídelo, no lo escribas. Samuel consume
`LineaFactura` y `Egreso`; no toca `Factura.cs`.

### Los tres archivos que sí van a chocar siempre

`Infrastructure/DependencyInjection.cs` y `Persistence/MiniErpDbContext.cs` los tocan los
tres: cada quien registra sus servicios y agrega sus DbSets. Igual con las migraciones,
que comparten una sola línea de tiempo.

Eso **no tiene solución de diseño**. Son conflictos triviales —líneas que se agregan— y la
mitigación es traer `main` seguido, no evitar el choque:

```bash
git fetch origin && git merge origin/main
```

Hazlo antes de empezar el día y antes de pedir el pull request. Nunca al final.

- **Estado global del proyecto:** la tabla **Dónde vamos** (aquí abajo) es el tablero. Al cerrar
  una tarea, actualízala en el mismo commit.
- **Tu lista concreta**, con el criterio de "terminado", vive en [Docs/Asignaciones.md](Docs/Asignaciones.md).
- **El plan y el calendario**, en [Docs/Plan-Maestro.md](Docs/Plan-Maestro.md).

---

## Dónde vamos

**F0, F1 y F2 completos y verificados en navegador.** Sigue F3.

| Fase | Estado | Qué incluye |
|------|--------|-------------|
| F0 | ✅ | Solución en capas, Identity, arranque autoconfigurable |
| F1 | ✅ | Inventario (productos, existencias, movimientos, alertas) y clientes |
| F2 | ✅ | Compras con recepción, secuencias de NCF, POS con cobro y factura |
| F3 | ⬜ | Finanzas · cobros y pagos · XML e-CF con QR |
| F4 | ⬜ | Integración, pruebas y caso piloto |
| F5 | ⬜ | Capítulo V y cierre del informe |

**El gate de F2 está demostrado de punta a punta:** se compraron 20 LB y el stock subió
de 9.5 a 29.5 con el costo promediado a 30.8729; se vendieron 3 LB y bajó a 26.5, con
NCF `B0200000001`, total 114.00 y cambio 86.00.

### Huecos conocidos

- **No existe la pantalla de facturas emitidas.** El POS enlaza a `/ventas/facturas/{id}`
  y esa ruta da 404. La venta guarda bien; solo falta dónde verla.
- **Los balances suben y nunca bajan.** Una compra a crédito deja deuda con el proveedor
  y no hay forma de pagarla. Le toca a Dionis.
- **Las pruebas son todas del dominio.** Servicios, repositorios y pantallas se verifican
  a mano. Falta prueba de integración de la transacción de emisión: nada demuestra
  automáticamente que un rollback libera el NCF reservado.

---

## Cómo se trabaja

**Cada quien en su rama**, con su nombre. Nadie commitea directo a `main`: se integra por
pull request revisado por otro del grupo.

| Rama | Quién | Dueño de |
|------|-------|----------|
| `jeison/pos` | Jeison | `Inventario/` · `Ventas/` — POS y comprobantes |
| `dionis/cobros-pagos` | Dionis | `Clientes/` · `Compras/` — terceros y su crédito |
| `samuel/finanzas` | Samuel | `Finanzas/` — lee todo lo demás, no lo modifica |

**Un módulo no está terminado hasta que su sección del Capítulo IV existe**, escrita en la
misma semana y con las capturas del momento.

### Trabajo en paralelo — no chocar

La frontera va por **carpeta de módulo** (detalle en [Asignaciones](Docs/Asignaciones.md)),
pero los conflictos salen en los archivos compartidos. Reglas:

- **`@using` de un módulo** → `Components/Pages/<Módulo>/_Imports.razor`, no el `_Imports.razor`
  raíz (que se queda con framework, `Domain.*` y `Application.Common`).
- **Registrar un servicio** → `DependencyInjection.<Módulo>.cs` (método `Add<Módulo>`), no el
  `AddInfrastructure` raíz, que solo los encadena.
- **Migraciones en serie:** una a la vez, integrada a `main` el mismo día. Dos migraciones sin
  integrar chocan en el `ModelSnapshot`.
- **Cadena de conexión:** `appsettings.json` con `Server=localhost`; tu instancia con nombre
  (p. ej. `SQLEXPRESS`) va en user-secrets, **nunca** en `appsettings.json` (es compartido).
- **Integra a `main` a diario y re-ramifica** al cerrar cada tarea; no vivas en una rama vieja.

---

## Levantarlo

```bash
dotnet run --project src/MiniERP.Web
```

Eso es todo. **Arrancar la app es el único paso de instalación**: crea la base si no
existe, aplica migraciones y siembra roles, administrador, unidades de medida, categorías
y secuencias de NCF. Todo idempotente.

La contraseña del administrador (`admin@minierp.local`) **se genera al azar y se escribe
una sola vez en la consola**, dentro de un recuadro. No hay contraseñas en el repositorio.
Para fijar una: `dotnet user-secrets set "Seed:AdminPassword" "..." --project src/MiniERP.Web`.

**Requisitos:** .NET 10 · SQL Server en `localhost` con autenticación de Windows ·
Visual Studio 2026 (por el formato `.slnx`).

```bash
dotnet build MiniERP.slnx
dotnet test                    # 100 pruebas del dominio
```

---

## Arquitectura

Modular en capas. **Modular** por módulo de negocio, **en capas** por responsabilidad.
Las capas internas nunca dependen de las externas.

```
MiniERP.Domain          Entidades y reglas puras. Sin dependencias.
      ^
MiniERP.Application     Casos de uso, contratos de repositorio, DTOs. No conoce EF Core.
      ^
MiniERP.Infrastructure  EF Core, SQL Server, Identity. Lo único que habla con la base.
      ^
MiniERP.Web             Blazor Server. Solo presentación.
```

Dentro de cada capa hay una carpeta por módulo. **Esa estructura es la evidencia del
Capítulo III**: no la reorganices sin motivo.

Nombres de dominio en español (`Producto`, `Factura`, `Compra`); vocabulario de framework
en inglés (`ApplicationUser`, `MiniErpDbContext`, `DependencyInjection`).

---

## Reglas del proyecto

Estas no son preferencias de estilo: cada una responde a algo que ya pasó o que el
anteproyecto exige.

- **Ningún saldo se edita.** Existencia, balance de cliente y balance de proveedor solo
  cambian por una operación que deja rastro. Incluso la existencia inicial de un producto
  entra como movimiento de apertura.
- **Todo lo que el sistema necesite para arrancar se siembra** en `DatabaseInitializer`.
  Nunca se documenta como paso manual.
- **Sin contraseñas en el repositorio.** Autenticación de Windows para SQL Server.
- **Versiones de paquetes solo en `Directory.Packages.props`**, jamás en un `.csproj`.
- **Las migraciones salen de Infrastructure:**
  ```bash
  dotnet ef migrations add <Nombre> \
    --project src/MiniERP.Infrastructure \
    --startup-project src/MiniERP.Web \
    --output-dir Persistence/Migrations
  ```
- **Un solo `MiniErpDbContext`** para identidad y los cinco módulos: una sola línea de
  migraciones para tres desarrolladores.
- **`HasData` solo para catálogo inmutable** (unidades de medida). Lo que exige lógica
  (hash de contraseña) o lo que el usuario editará va en el sembrador de arranque.
  Si usas `HasData`, la fecha va **fija**: un `DateTime.UtcNow` ahí hace que el modelo
  cambie en cada compilación y las migraciones nunca cierren.

---

## Un pago a proveedor NO es un egreso

Esta es la trampa más cara del proyecto, y no da error: produce números creíbles y falsos.

Tres cosas distintas que parecen la misma:

| Concepto | Dueño | Qué es | ¿Afecta la utilidad? |
|---|---|---|---|
| **`Egreso`** | Samuel · `Finanzas/` | Gasto operativo que no pasa por inventario: alquiler, luz, agua, sueldos, transporte | **Sí** |
| **`Pago`** | Dionis · `Compras/` | Saldar lo que se le debe a un proveedor | **No** — liquida una deuda |
| **`Cobro`** | Dionis · `Clientes/` | Saldar lo que un cliente debe | **No** — el ingreso ya se reconoció al facturar |

Registrar los pagos a proveedor como egresos cuenta el costo **dos veces**:

```
Compras arroz por 640, lo vendes en 1,000, y le pagas al proveedor.

  Ingresos                          1,000
− Costo de lo vendido                −640    ← el costo ya está aquí
− Egresos (pago al proveedor)        −640    ← contado otra vez
= Utilidad                            −280   ← FALSO. Ganaste 360.
```

El gasto ocurrió cuando **vendiste** la mercancía, no cuando pagaste la factura.

### Son dos reportes distintos, y el anteproyecto pide los dos

**Estado de resultados** — cuánto gana el negocio:

```
  Ingresos              Factura.Subtotal del período (sin ITBIS)
− Costo de lo vendido   Factura.CostoTotal (congelado al vender)
− Egresos operativos    Egreso
= Utilidad
```

No entran cobros ni pagos. El ITBIS tampoco: ese dinero es de la DGII, no del comercio.

**Flujo de caja** — cuánto efectivo entra y sale de la gaveta:

```
+ Ventas de contado
+ Cobros a clientes
− Egresos pagados
− Pagos a proveedores
= Efectivo neto
```

No entran las ventas a crédito que aún no se han cobrado.

Un negocio puede tener utilidad y no tener efectivo — vendió todo fiado. O tener efectivo
y estar perdiendo — cobró viejo y vende bajo costo. El planteamiento del problema pide las
dos cosas por separado: *"calcular sus márgenes"* y *"cuidar su flujo de efectivo"*.
Mezclarlas es el error que esta sección existe para evitar.

---

## Decisiones de dominio que no se cambian sin discutirlas

**Costo promedio ponderado, no último costo.** Comprar 10 a 30 y luego 10 a 40 deja el
costo en 35. Con último costo quedaría en 40, inflando el costo y escondiendo el margen
real. El anteproyecto habla de no conocer *"los costos reales de adquisición"*: esto es lo
que responde esa pregunta. Vive en `Compra.CalcularCostoPromedio`.

**El costo se congela en la línea de factura.** `LineaFactura.CostoUnitario` guarda el
costo al momento de vender. Sin eso, una compra posterior cambiaría retroactivamente el
margen de ventas ya hechas, y los resultados del Capítulo V se moverían solos. **Los
reportes de rentabilidad deben usar `LineaFactura.CostoUnitario`, nunca `Producto.Costo`.**

**El NCF se asigna con un `UPDATE ... OUTPUT` atómico.** Leer el contador, sumarle uno y
guardar deja una ventana en la que dos cajas obtienen el mismo número: dos comprobantes
con el mismo NCF es una infracción ante la DGII, no un bug cosmético. Verificado con 20
procesos concurrentes. Vive en `SecuenciaNcfRepositorio.AsignarSiguienteAsync`.

**La emisión corre en una transacción explícita** para que un fallo libere el NCF
reservado. El precio: la fila de la secuencia se bloquea durante la venta, así que dos
cajas se serializan un instante. Aceptable para un local con una o dos cajas.

**La merma es un tipo de movimiento propio**, no un ajuste negativo. El anteproyecto
nombra las *"mermas no detectadas"* como uno de los problemas a resolver; mezclarlas con
los ajustes las volvería invisibles otra vez. Exigen motivo por escrito.

**El redondeo es aritmético, no bancario.** `RetailConstants.RedondearImporte` usa
`AwayFromZero`. .NET redondea a par por defecto, y que 2.505 dé 2.50 le hace perder la
confianza al comerciante en su propia caja.

**Los códigos de `TipoComprobante` son los de la DGII** (01, 02, 14, 15) porque forman
parte del NCF. Hay una prueba que los fija.

**Documentos congelados.** La descripción, la tasa de ITBIS y el nombre del cliente se
copian al documento al capturarlo. Si mañana el producto cambia de gravado a exento, la
factura de ayer debe seguir mostrando lo que se imprimió.

---

## Alcance — lo que NO se hace

Definido así en el anteproyecto y sostenido a lo largo del proyecto. Si alguien lo pide,
citar esto y decir que no:

- **Certificación fiscal ante la DGII.** Se genera el XML e-CF y el QR, y ahí se para.
- Analítica predictiva.
- Integración con comercio electrónico.
- Nómina, producción industrial y logística de distribución.
- Múltiples almacenes: el comercio opera en una sola ubicación, y la existencia vive en
  `Producto`.

Las secuencias de NCF que siembra el sistema **no son rangos autorizados por la DGII**.
Son de arranque, para que el sistema funcione desde el primer día. Antes del piloto real
hay que cargar los que la DGII haya autorizado al comercio.
